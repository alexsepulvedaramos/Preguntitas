import { Component, ElementRef, ViewChild, computed, inject, input, output, signal } from '@angular/core';
import { forkJoin } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSparkles, lucideTrash2 } from '@ng-icons/lucide';

import { AuthService } from '../../../../core/auth/auth.service';
import { DailyService } from '../../../../core/services/daily.service';
import { QuestionsService } from '../../../../core/services/questions.service';
import { PacksService } from '../../../../core/services/packs.service';
import { Option, Question, QuestionToVote } from '../../../../core/models/question.model';
import { SelectionSourceItem } from '../../../../core/models/daily.model';
import { Pack, PackTemplate } from '../../../../core/models/pack.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { GroupMember } from '../../models/group.models';
import { CreateQuestionComponent } from '../create-question/create-question.component';
import { DeathmatchCreateComponent } from '../deathmatch-create/deathmatch-create.component';

type Step = 'sources' | 'create' | 'assign-teams';

const PAGE_SIZE = 8;

// Selector picker (rama 8/15, spec §13 row 8+15 / §6.4): lets the day's selector choose the
// next-day question from the group pool, any enabled pack, or create one inline. Anyone —
// not just the selector — can open it to suggest/save a question to the pool, but only
// the selector sees the "elegir para mañana" actions (the backend rejects daily/select
// from anyone else). Pool items owned by the current user (or any item, if the user is
// the group admin) can be deleted directly from the list.
//
// The pool and pack-templates lists are independently paginated ("Cargar más"), share a
// type filter, and the pack list additionally takes a pack filter. Both QuestionDto and
// PackTemplateDto pages get mapped into the same local SelectionSourceItem shape so the
// pick/team-assignment/edit logic below doesn't care which paginated source an item came from.
@Component({
  selector: 'app-select-question-dialog',
  imports: [
    HlmButtonImports,
    HlmDialogImports,
    HlmSpinnerImports,
    HlmToggleGroupImports,
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

  // Filters: type is shared by both lists; pack only narrows the pack-templates list.
  protected readonly typeFilter = signal<QuestionType | null>(null);
  protected readonly packFilter = signal<number | null>(null);
  protected readonly availablePacks = signal<Pack[]>([]);

  // Pool list — independently paginated.
  protected readonly poolItems = signal<SelectionSourceItem[]>([]);
  protected readonly poolHasMore = signal(false);
  protected readonly poolLoadingMore = signal(false);

  // Pack-templates list — independently paginated.
  protected readonly packItems = signal<SelectionSourceItem[]>([]);
  protected readonly packHasMore = signal(false);
  protected readonly packLoadingMore = signal(false);

  // Passed to app-create-question when editing an already-selected Scale/CustomPoll question.
  protected readonly pendingEditValue = signal<QuestionToVote | null>(null);

  // Deathmatch-from-pack team assignment (instead of the random split in §6.3, the
  // selector arranges the teams themselves before confirming).
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
    this.packFilter.set(null);
    if (this.isSelector()) {
      this.loadSources();
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

    // All Deathmatch items (pack and pool) go through the team assignment step so the
    // selector can arrange/review teams before confirming. Pack items start fresh;
    // pool items pre-fill from the question's stored teams.
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

    this.dailyService
      .select(this.groupId(), dto)
      .subscribe({
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
      this.loadSources();
    } else {
      ctx.close();
    }
  }

  formatOptions(options: Option[]): string {
    return options.map((o) => o.text).join(' · ');
  }

  onTypeFilterChange(value: string | string[] | null | undefined) {
    const v = Array.isArray(value) ? value[0] : value;
    this.typeFilter.set(v && v !== 'all' ? (Number(v) as QuestionType) : null);
    this.loadPool(true);
    this.loadPack(true);
  }

  onPackFilterChange(value: string | string[] | null | undefined) {
    const v = Array.isArray(value) ? value[0] : value;
    this.packFilter.set(v && v !== 'all' ? Number(v) : null);
    this.loadPack(true);
  }

  loadMorePool() {
    this.loadPool(false);
  }

  loadMorePack() {
    this.loadPack(false);
  }

  private loadSources() {
    this.loading.set(true);
    this.error.set(null);

    this.packsService.getPacks(this.groupId()).subscribe({
      next: (packs) => this.availablePacks.set(packs.filter((p) => p.enabled)),
      error: () => {
        // Pack filter is a non-essential refinement — silently leave it empty on failure.
      },
    });

    forkJoin({
      pool: this.questionsService.getPool(this.groupId(), {
        pageSize: PAGE_SIZE,
        type: this.typeFilter() ?? undefined,
      }),
      pack: this.packsService.getTemplates(this.groupId(), {
        pageSize: PAGE_SIZE,
        type: this.typeFilter() ?? undefined,
      }),
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
        error: () => {
          this.poolLoadingMore.set(false);
        },
      });
  }

  private loadPack(reset: boolean) {
    if (!reset && (this.packLoadingMore() || !this.packHasMore())) return;

    const before = reset ? undefined : this.packItems().at(-1)?.id;
    this.packLoadingMore.set(true);

    this.packsService
      .getTemplates(this.groupId(), {
        pageSize: PAGE_SIZE,
        type: this.typeFilter() ?? undefined,
        packId: this.packFilter() ?? undefined,
        before,
      })
      .subscribe({
        next: (page) => {
          const mapped = page.items.map((t) => this.toPackItem(t));
          this.packItems.update((items) => (reset ? mapped : [...items, ...mapped]));
          this.packHasMore.set(page.hasMore);
          this.packLoadingMore.set(false);
        },
        error: () => {
          this.packLoadingMore.set(false);
        },
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
