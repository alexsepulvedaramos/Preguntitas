import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
import { forkJoin } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSparkles, lucideTrash2 } from '@ng-icons/lucide';

import { AuthService } from '../../../../core/auth/auth.service';
import { DailyService } from '../../../../core/services/daily.service';
import { QuestionsService } from '../../../../core/services/questions.service';
import { Option, QuestionToVote } from '../../../../core/models/question.model';
import { SelectionSourceItem, SelectionSources } from '../../../../core/models/daily.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { GroupMember } from '../../models/group.models';
import { CreateQuestionComponent } from '../create-question/create-question.component';
import { DeathmatchCreateComponent } from '../deathmatch-create/deathmatch-create.component';

type Step = 'sources' | 'create' | 'assign-teams';

// Selector picker (rama 8, spec §13 row 8 / §6.4): lets the day's selector choose the
// next-day question from the group pool, the base pack, or create one inline. Anyone —
// not just the selector — can open it to suggest/save a question to the pool, but only
// the selector sees the "elegir para mañana" actions (the backend rejects daily/select
// from anyone else). Pool items owned by the current user (or any item, if the user is
// the group admin) can be deleted directly from the list.
@Component({
  selector: 'app-select-question-dialog',
  imports: [
    HlmButtonImports,
    HlmDialogImports,
    HlmSpinnerImports,
    NgIcon,
    CreateQuestionComponent,
    DeathmatchCreateComponent,
  ],
  providers: [provideIcons({ lucideSparkles, lucideTrash2 })],
  templateUrl: './select-question-dialog.component.html',
})
export class SelectQuestionDialogComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dailyService = inject(DailyService);
  private readonly questionsService = inject(QuestionsService);

  public readonly groupId = input.required<number>();
  public readonly members = input.required<GroupMember[]>();
  public readonly isSelector = input.required<boolean>();

  // The question already picked for tomorrow, if any — only ever set when isSelector().
  public readonly pendingQuestion = input<QuestionToVote | null>(null);

  public readonly selected = output<void>();

  protected readonly QuestionType = QuestionType;
  protected readonly QUESTION_TYPE_LABELS = QUESTION_TYPE_LABELS;
  protected readonly QUESTION_TYPE_BADGE_CLASS = QUESTION_TYPE_BADGE_CLASS;

  protected readonly step = signal<Step>('sources');
  protected readonly sources = signal<SelectionSources>({ pool: [], pack: [] });
  protected readonly poolCreatorIds = signal<Record<number, number | null>>({});
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly pickingKey = signal<string | null>(null);
  protected readonly deletingId = signal<number | null>(null);

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

  ngOnInit() {
    this.resetDialog();
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
    this.loadSources();
  }

  itemKey(item: SelectionSourceItem): string {
    return `${item.sourceType}-${item.id}`;
  }

  canDelete(item: SelectionSourceItem): boolean {
    if (item.sourceType !== 'pool') return false;
    if (this.isAdmin()) return true;

    const currentUserId = Number(this.authService.currentUser()?.id);
    return this.poolCreatorIds()[item.id] === currentUserId;
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

    this.dailyService
      .select(this.groupId(), {
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
      })
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
        this.sources.update((s) => ({ ...s, pool: s.pool.filter((q) => q.id !== item.id) }));
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

  // True when a pool item is the currently-pending question (already picked but not
  // activated). We hide it from the pool list to avoid showing it twice.
  isPendingPoolItem(item: SelectionSourceItem): boolean {
    const pending = this.pendingQuestion();
    return item.sourceType === 'pool' && !!pending && item.id === pending.id;
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

  private loadSources() {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      sources: this.dailyService.getSelectionSources(this.groupId()),
      pool: this.questionsService.getPool(this.groupId()),
    }).subscribe({
      next: ({ sources, pool }) => {
        this.sources.set(sources);
        this.poolCreatorIds.set(
          Object.fromEntries(pool.map((q) => [q.id, q.creatorId]))
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se han podido cargar las preguntas disponibles.');
        this.loading.set(false);
      },
    });
  }
}
