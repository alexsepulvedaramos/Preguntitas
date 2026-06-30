import { Component, ElementRef, ViewChild, computed, inject, input, output, signal } from '@angular/core';
import { forkJoin, of } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmNativeSelectImports } from '@spartan-ng/helm/native-select';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSparkles, lucideTrash2 } from '@ng-icons/lucide';

import { AuthService } from '../../../../core/auth/auth.service';
import { DailyService } from '../../../../core/services/daily.service';
import { QuestionsService } from '../../../../core/services/questions.service';
import { PacksService } from '../../../../core/services/packs.service';
import { Option, Question, QuestionPage, QuestionToVote } from '../../../../core/models/question.model';
import { SelectionSourceItem } from '../../../../core/models/daily.model';
import { Pack, PackTemplate, PackTemplatePage } from '../../../../core/models/pack.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { GroupMember } from '../../models/group.models';
import { CreateQuestionComponent } from '../create-question/create-question.component';
import { DeathmatchCreateComponent } from '../deathmatch-create/deathmatch-create.component';

type Step = 'sources' | 'create' | 'assign-teams';

// 'all' = group pool + every enabled pack · 'pool' = only group-created · number = one pack id.
type SourceFilter = 'all' | 'pool' | number;

const PAGE_SIZE = 10;

// Selector picker (rama 8/15, spec §6.4): lets the day's selector choose the next-day question
// from the group pool, any enabled pack, or create one inline. Anyone — not just the selector —
// can open it to suggest/save a question to the pool, but only the selector sees the "elegir
// para mañana" actions (the backend rejects daily/select from anyone else). Pool items owned by
// the current user (or any item, if the user is the group admin) can be deleted from the list.
//
// UI: one combined, scannable list of question cards with two compact dropdowns — type and
// source (Todas / Del grupo / a specific pack). The pool and pack template pages are fetched
// independently and merged client-side (pool first, preserving user-pool priority — §6.6) so a
// single "Cargar más" walks the pool pages first and then the pack pages. Both QuestionDto and
// PackTemplateDto map into the same local SelectionSourceItem shape, so the pick/edit/team
// logic below doesn't care which source an item came from.
@Component({
  selector: 'app-select-question-dialog',
  imports: [
    HlmButtonImports,
    HlmDialogImports,
    HlmSpinnerImports,
    HlmNativeSelectImports,
    NgIcon,
    CreateQuestionComponent,
    DeathmatchCreateComponent,
  ],
  providers: [provideIcons({ lucideSparkles, lucideTrash2 })],
  templateUrl: './select-question-dialog.component.html',
})
export class SelectQuestionDialogComponent {
  private readonly authService = inject(AuthService);
  private readonly dailyService = inject(DailyService);
  private readonly questionsService = inject(QuestionsService);
  private readonly packsService = inject(PacksService);

  public readonly groupId = input.required<number>();
  public readonly members = input.required<GroupMember[]>();
  public readonly isSelector = input.required<boolean>();

  // The question already picked for tomorrow, if any — only ever set when isSelector().
  public readonly pendingQuestion = input<QuestionToVote | null>(null);
  public readonly compact = input(false);

  public readonly selected = output<void>();

  protected readonly QuestionType = QuestionType;
  protected readonly QUESTION_TYPE_LABELS = QUESTION_TYPE_LABELS;
  protected readonly QUESTION_TYPE_BADGE_CLASS = QUESTION_TYPE_BADGE_CLASS;
  protected readonly questionTypes = Object.values(QuestionType).filter(
    (v): v is QuestionType => typeof v === 'number',
  );

  protected readonly step = signal<Step>('sources');
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly pickingKey = signal<string | null>(null);
  protected readonly deletingId = signal<number | null>(null);

  // Filters. The two string computeds back the native-select [value] bindings.
  protected readonly typeFilter = signal<QuestionType | null>(null);
  protected readonly typeFilterValue = computed(() => this.typeFilter()?.toString() ?? 'all');
  protected readonly sourceFilter = signal<SourceFilter>('all');
  protected readonly sourceFilterValue = computed(() => this.sourceFilter().toString());
  protected readonly availablePacks = signal<Pack[]>([]);

  // Pool + pack pages (fetched independently, merged for display).
  protected readonly poolItems = signal<SelectionSourceItem[]>([]);
  protected readonly poolHasMore = signal(false);
  protected readonly poolLoadingMore = signal(false);
  protected readonly packItems = signal<SelectionSourceItem[]>([]);
  protected readonly packHasMore = signal(false);
  protected readonly packLoadingMore = signal(false);

  // The single list the template renders, plus its combined paging state.
  protected readonly items = computed<SelectionSourceItem[]>(() => {
    const s = this.sourceFilter();
    if (s === 'pool') return this.poolItems();
    if (typeof s === 'number') return this.packItems();
    return [...this.poolItems(), ...this.packItems()];
  });
  protected readonly hasMore = computed(() => {
    const s = this.sourceFilter();
    if (s === 'pool') return this.poolHasMore();
    if (typeof s === 'number') return this.packHasMore();
    return this.poolHasMore() || this.packHasMore();
  });
  protected readonly loadingMore = computed(
    () => this.poolLoadingMore() || this.packLoadingMore(),
  );

  // Passed to app-create-question when editing an already-selected Scale/CustomPoll question.
  protected readonly pendingEditValue = signal<QuestionToVote | null>(null);

  // Deathmatch team assignment (instead of the random split in §6.3, the selector arranges
  // the teams themselves before confirming).
  protected readonly teamAssignmentItem = signal<SelectionSourceItem | null>(null);
  protected readonly teamAssignmentTeams = signal<number[][]>([[], []]);
  protected readonly assigningTeams = signal(false);

  protected readonly isAdmin = computed(() => {
    const currentUserId = Number(this.authService.currentUser()?.id);
    return this.members().find((m) => m.id === currentUserId)?.isAdmin ?? false;
  });

  @ViewChild('triggerBtn') private triggerBtn!: ElementRef<HTMLButtonElement>;

  triggerOpen() {
    this.resetDialog();
    // Delegates to the native trigger so Spartan's dialog state machine stays in sync.
    this.triggerBtn?.nativeElement.click();
  }

  resetDialog() {
    this.step.set(this.isSelector() ? 'sources' : 'create');
    this.error.set(null);
    this.pickingKey.set(null);
    this.deletingId.set(null);
    this.teamAssignmentItem.set(null);
    this.teamAssignmentTeams.set([[], []]);
    this.assigningTeams.set(false);
    this.pendingEditValue.set(null);
    this.typeFilter.set(null);
    if (this.isSelector()) {
      this.bootstrapSources();
    }
  }

  itemKey(item: SelectionSourceItem): string {
    return `${item.sourceType}-${item.id}`;
  }

  canDelete(item: SelectionSourceItem): boolean {
    if (item.sourceType !== 'pool') return false;
    if (this.isAdmin()) return true;

    const currentUserId = Number(this.authService.currentUser()?.id);
    return item.creatorId === currentUserId;
  }

  pick(item: SelectionSourceItem, ctx: { close: () => void }) {
    if (this.pickingKey()) return;

    // All Deathmatch items go through the team assignment step so the selector can arrange/review
    // teams before confirming. Pack items start fresh; pool items pre-fill from stored teams.
    if (item.type === QuestionType.Deathmatch) {
      this.teamAssignmentItem.set(item);
      this.teamAssignmentTeams.set(
        item.teams?.length ? item.teams.map((t) => [...t]) : [[], []],
      );
      this.step.set('assign-teams');
      return;
    }

    this.error.set(null);
    this.pickingKey.set(this.itemKey(item));

    const dto =
      item.sourceType === 'pool'
        ? { existingQuestionId: item.id }
        : { templateId: item.id };

    this.dailyService.select(this.groupId(), dto).subscribe({
      next: () => {
        this.pickingKey.set(null);
        this.selected.emit();
        ctx.close();
      },
      error: (err) => {
        this.pickingKey.set(null);
        this.error.set(err.error ?? 'No se ha podido seleccionar la pregunta.');
      },
    });
  }

  confirmTeamAssignment(ctx: { close: () => void }) {
    const item = this.teamAssignmentItem();
    if (!item || this.assigningTeams()) return;

    this.error.set(null);
    this.assigningTeams.set(true);

    // Pool questions: update teams on the existing entity (avoids creating a duplicate).
    // Pack templates: must be cloned as a new question (no pool entity exists yet).
    const dto =
      item.sourceType === 'pool'
        ? { existingQuestionId: item.id, teamsOverride: this.teamAssignmentTeams() }
        : {
            newQuestion: {
              text: item.text,
              type: QuestionType.Deathmatch,
              allowNobody: false,
              blacklistedUserIds: [],
              rangeMin: null,
              rangeMax: null,
              targetUserId: null,
              minSelections: null,
              maxSelections: null,
              allowOther: false,
              teams: this.teamAssignmentTeams(),
              options: [],
            },
          };

    this.dailyService.select(this.groupId(), dto).subscribe({
      next: () => {
        this.assigningTeams.set(false);
        this.selected.emit();
        ctx.close();
      },
      error: (err) => {
        this.assigningTeams.set(false);
        this.error.set(err.error ?? 'No se ha podido confirmar los equipos.');
      },
    });
  }

  backFromTeamAssignment() {
    this.teamAssignmentItem.set(null);
    this.step.set('sources');
  }

  delete(item: SelectionSourceItem) {
    if (this.deletingId()) return;

    this.error.set(null);
    this.deletingId.set(item.id);

    this.questionsService.delete(this.groupId(), item.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.poolItems.update((items) => items.filter((q) => q.id !== item.id));
      },
      error: (err) => {
        this.deletingId.set(null);
        this.error.set(err.error ?? 'No se ha podido eliminar la pregunta.');
      },
    });
  }

  openCreate() {
    this.pendingEditValue.set(null);
    this.step.set('create');
  }

  backToSources() {
    this.pendingEditValue.set(null);
    this.step.set('sources');
  }

  // Opens the appropriate edit step for the already-chosen pending question.
  // Only called for types that have configurable metadata (Deathmatch, Scale, CustomPoll).
  editPendingQuestion() {
    const pending = this.pendingQuestion();
    if (!pending) return;

    if (pending.type === QuestionType.Deathmatch) {
      const item: SelectionSourceItem = {
        sourceType: 'pool',
        id: pending.id,
        text: pending.text,
        type: pending.type,
        options: [],
        teams: pending.teams ?? [],
      };
      this.teamAssignmentItem.set(item);
      this.teamAssignmentTeams.set(
        pending.teams?.length ? pending.teams.map((t) => [...t]) : [[], []],
      );
      this.step.set('assign-teams');
    } else {
      // Scale / CustomPoll: open the create form pre-filled with the existing values.
      this.pendingEditValue.set(pending);
      this.step.set('create');
    }
  }

  isEditableType(type: QuestionType): boolean {
    return (
      type === QuestionType.Deathmatch ||
      type === QuestionType.Scale ||
      type === QuestionType.CustomPoll
    );
  }

  onSelectedForTomorrow(ctx: { close: () => void }) {
    this.selected.emit();
    ctx.close();
  }

  onSavedToPool(ctx: { close: () => void }) {
    if (this.isSelector()) {
      this.step.set('sources');
      this.reload();
    } else {
      ctx.close();
    }
  }

  formatOptions(options: Option[]): string {
    return options.map((o) => o.text).join(' · ');
  }

  onTypeChange(value: string | undefined | null) {
    this.typeFilter.set(!value || value === 'all' ? null : (Number(value) as QuestionType));
    this.reload();
  }

  onSourceChange(value: string | undefined | null) {
    if (!value || value === 'all') this.sourceFilter.set('all');
    else if (value === 'pool') this.sourceFilter.set('pool');
    else this.sourceFilter.set(Number(value));
    this.reload();
  }

  loadMore() {
    const s = this.sourceFilter();
    if (s === 'pool') return this.loadPool(false);
    if (typeof s === 'number') return this.loadPack(false);
    // Combined view: exhaust the pool pages first, then the pack pages.
    return this.poolHasMore() ? this.loadPool(false) : this.loadPack(false);
  }

  // Default the source filter to the group's own pool — questions the group wrote themselves
  // take priority over pack content. Only falls back to the Base pack (not "Todo") when the
  // pool is empty, so a brand-new group still lands on a sensible, non-empty starting list.
  private bootstrapSources() {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      pool: this.questionsService.getPool(this.groupId(), { pageSize: PAGE_SIZE }),
      packs: this.packsService.getPacks(this.groupId()),
    }).subscribe({
      next: ({ pool, packs }) => {
        const enabledPacks = packs.filter((p) => p.enabled);
        this.availablePacks.set(enabledPacks);
        this.poolItems.set(pool.items.map((q) => this.toPoolItem(q)));
        this.poolHasMore.set(pool.hasMore);

        if (pool.items.length > 0) {
          this.sourceFilter.set('pool');
          this.loading.set(false);
        } else {
          const basePack = enabledPacks.find((p) => p.name === 'Base');
          this.sourceFilter.set(basePack?.id ?? 'all');
          this.loadInitialPackPage();
        }
      },
      error: () => {
        this.error.set('No se han podido cargar las preguntas disponibles.');
        this.loading.set(false);
      },
    });
  }

  private loadInitialPackPage() {
    const s = this.sourceFilter();

    this.packsService
      .getTemplates(this.groupId(), {
        pageSize: PAGE_SIZE,
        packId: typeof s === 'number' ? s : undefined,
      })
      .subscribe({
        next: (page) => {
          this.packItems.set(page.items.map((t) => this.toPackItem(t)));
          this.packHasMore.set(page.hasMore);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('No se han podido cargar las preguntas disponibles.');
          this.loading.set(false);
        },
      });
  }

  private reload() {
    this.loading.set(true);
    this.error.set(null);

    const type = this.typeFilter() ?? undefined;
    const s = this.sourceFilter();
    const wantPool = s === 'all' || s === 'pool';
    const wantPack = s === 'all' || typeof s === 'number';

    const empty = { items: [], hasMore: false };

    forkJoin({
      pool: wantPool
        ? this.questionsService.getPool(this.groupId(), { pageSize: PAGE_SIZE, type })
        : of(empty as QuestionPage),
      pack: wantPack
        ? this.packsService.getTemplates(this.groupId(), {
            pageSize: PAGE_SIZE,
            type,
            packId: typeof s === 'number' ? s : undefined,
          })
        : of(empty as PackTemplatePage),
    }).subscribe({
      next: ({ pool, pack }) => {
        this.poolItems.set(pool.items.map((q) => this.toPoolItem(q)));
        this.poolHasMore.set(pool.hasMore);
        this.packItems.set(pack.items.map((t) => this.toPackItem(t)));
        this.packHasMore.set(pack.hasMore);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se han podido cargar las preguntas disponibles.');
        this.loading.set(false);
      },
    });
  }

  private loadPool(reset: boolean) {
    if (!reset && (this.poolLoadingMore() || !this.poolHasMore())) return;

    const before = reset ? undefined : this.poolItems().at(-1)?.id;
    this.poolLoadingMore.set(true);

    this.questionsService
      .getPool(this.groupId(), { pageSize: PAGE_SIZE, type: this.typeFilter() ?? undefined, before })
      .subscribe({
        next: (page) => {
          const mapped = page.items.map((q) => this.toPoolItem(q));
          this.poolItems.update((items) => (reset ? mapped : [...items, ...mapped]));
          this.poolHasMore.set(page.hasMore);
          this.poolLoadingMore.set(false);
        },
        error: () => this.poolLoadingMore.set(false),
      });
  }

  private loadPack(reset: boolean) {
    if (!reset && (this.packLoadingMore() || !this.packHasMore())) return;

    const s = this.sourceFilter();
    const packId = typeof s === 'number' ? s : undefined;
    const before = reset ? undefined : this.packItems().at(-1)?.id;
    this.packLoadingMore.set(true);

    this.packsService
      .getTemplates(this.groupId(), {
        pageSize: PAGE_SIZE,
        type: this.typeFilter() ?? undefined,
        packId,
        before,
      })
      .subscribe({
        next: (page) => {
          const mapped = page.items.map((t) => this.toPackItem(t));
          this.packItems.update((items) => (reset ? mapped : [...items, ...mapped]));
          this.packHasMore.set(page.hasMore);
          this.packLoadingMore.set(false);
        },
        error: () => this.packLoadingMore.set(false),
      });
  }

  private toPoolItem(q: Question): SelectionSourceItem {
    return {
      sourceType: 'pool',
      id: q.id,
      text: q.text,
      type: q.type,
      options: q.options.map((o) => o.text),
      teams: q.teams ?? [],
      creatorId: q.creatorId,
    };
  }

  private toPackItem(t: PackTemplate): SelectionSourceItem {
    return {
      sourceType: 'pack',
      id: t.id,
      text: t.text,
      type: t.type,
      options: t.options,
      teams: [],
    };
  }
}
