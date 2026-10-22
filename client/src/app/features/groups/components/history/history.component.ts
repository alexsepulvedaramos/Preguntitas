import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { switchMap } from 'rxjs/operators';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCalendar, lucideCircleCheck, lucideCircleX, lucideLock, lucideZap } from '@ng-icons/lucide';

import { QuestionsService } from '../../../../core/services/questions.service';
import { DailyService } from '../../../../core/services/daily.service';
import { HistoryEntry } from '../../../../core/models/question.model';
import { QuestionResult } from '../../../../core/models/result.model';
import { QuestionToVote } from '../../../../core/models/question.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { ResultsComponent } from '../results/results.component';
import { VoteFormComponent } from '../vote-form/vote-form.component';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { GroupsService } from '../../services/groups.service';
import { apiErrorMessage } from '../../../../shared/utils/api-error';
import { WordStaggerPipe } from '../../../../shared/pipes/word-stagger.pipe';

import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { provideCalendarI18n } from '../../../../core/i18n/calendar-i18n';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { QuestionTypeChipComponent } from '../../../../shared/components/question-type-chip/question-type-chip.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';

// A day's question is keyed by its date; a flash question (rama 44) by its id, since it can
// share the day with the daily one.
export function historyKey(entry: HistoryEntry): string {
  return entry.flashId != null ? `flash-${entry.flashId}` : entry.date;
}

@Component({
  selector: 'app-history',
  imports: [PageHeaderComponent, QuestionTypeChipComponent, 
    HlmButtonImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    HlmDatePickerImports,
    NgIcon,
    ResultsComponent,
    VoteFormComponent,
    WordStaggerPipe,
  ],
  // The date picker is the app's only calendar: its i18n is provided here so the calendar
  // code stays in this lazy chunk.
  providers: [provideIcons({ lucideArrowLeft, lucideCalendar, lucideCircleCheck, lucideCircleX, lucideLock, lucideZap }), provideCalendarI18n()],
  templateUrl: './history.component.html',
})
export class HistoryComponent implements OnInit {
  private readonly questionsService = inject(QuestionsService);
  private readonly dailyService = inject(DailyService);
  private readonly groupStreaks = inject(GroupStreaksService);
  private readonly groupsService = inject(GroupsService);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));
  // ?flash={id}: opened from the "time capsule opened" push (rama 50), straight on that flash.
  public readonly flash = input<string | undefined>();

  // History list state
  public readonly entries = signal<HistoryEntry[]>([]);
  public readonly hasMore = signal(false);
  public readonly loading = signal(true);
  public readonly loadingMore = signal(false);
  public readonly error = signal<TranslationKey | null>(null);

  // Detail state — the currently-expanded entry (see historyKey) and its full results.
  public readonly selectedKey = signal<string | null>(null);
  private readonly selectedEntry = signal<HistoryEntry | null>(null);
  public readonly selectedResult = signal<QuestionResult | null>(null);
  public readonly loadingDetail = signal(false);
  public readonly detailError = signal<TranslationKey | null>(null);

  // Late vote (rama 39): a locked past question opens the voting form in place. Members are
  // only needed by the people pickers, so they load the first time the form opens.
  public readonly members = signal<GroupMember[]>([]);
  public readonly showLateVote = signal(false);
  public readonly lateVoting = signal(false);
  public readonly lateVoteError = signal<string | null>(null);

  // Date picker binding (JS Date, reset after each use so the trigger always shows the placeholder).
  public readonly pickerDate = signal<Date | undefined>(undefined);

  // Limit the date picker range: no future dates; lower bound = oldest loaded entry
  // (only applied when there are no more pages so we know the true minimum).
  public readonly pickerMax = new Date();
  public readonly pickerMin = computed<Date | undefined>(() => {
    if (this.hasMore()) return undefined;
    const last = this.entries().at(-1);
    if (!last) return undefined;
    const [y, m, d] = last.date.split('-').map(Number);
    return new Date(y, m - 1, d, 12);
  });

  protected readonly QuestionType = QuestionType;
  protected readonly historyKey = historyKey;

  ngOnInit() {
    // Results avatars read members' streak rings from here (rama 19).
    this.groupStreaks.ensure(this.numericGroupId());
    this.loadHistory();
  }

  private loadHistory() {
    this.loading.set(true);
    this.error.set(null);

    this.questionsService.getHistory(this.numericGroupId()).subscribe({
      next: (page) => {
        this.entries.set(page.items);
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
        this.openLinkedFlash();
      },
      error: () => {
        this.error.set('history.loadError');
        this.loading.set(false);
      },
    });
  }

  // The flash named in ?flash= is opened; if it's older than the first page, its row is
  // added on top so the detail panel has somewhere to render.
  private openLinkedFlash() {
    const flashId = Number(this.flash());
    if (!flashId) return;
    const loaded = this.entries().find((e) => e.flashId === flashId);
    if (loaded) {
      this.selectEntry(loaded);
      return;
    }
    this.dailyService.getFlash(this.numericGroupId(), flashId).subscribe({
      next: (result) => {
        const entry: HistoryEntry = {
          date: new Date(result.dateCreated).toISOString().slice(0, 10),
          questionText: result.text,
          type: result.type,
          totalVotes: result.totalVotes,
          authorUsername: result.author?.username ?? null,
          myGuessCorrect: null,
          flashId,
          anonymousAuthor: result.author == null,
          capsuleOpensAt: result.capsuleOpensAt ?? null,
        };
        this.entries.update((prev) => [entry, ...prev]);
        this.selectEntry(entry);
      },
    });
  }

  isSealed(iso: string): boolean {
    return new Date(iso).getTime() > Date.now();
  }

  // Time capsule (rama 50): the day its results open, in the app's language.
  formatCapsuleDate(iso: string): string {
    return this.i18n.formatDate(iso, { day: 'numeric', month: 'long', year: 'numeric' });
  }

  loadMore() {
    const last = this.entries().at(-1);
    if (!last) return;

    this.loadingMore.set(true);
    this.questionsService.getHistory(this.numericGroupId(), last.date).subscribe({
      next: (page) => {
        this.entries.update((prev) => [...prev, ...page.items]);
        this.hasMore.set(page.hasMore);
        this.loadingMore.set(false);
      },
      error: () => {
        this.loadingMore.set(false);
      },
    });
  }

  // Toggle: clicking the same entry collapses it; clicking another fetches its detail.
  selectEntry(entry: HistoryEntry) {
    this.resetLateVote();
    const key = historyKey(entry);
    if (this.selectedKey() === key) {
      this.selectedKey.set(null);
      this.selectedEntry.set(null);
      this.selectedResult.set(null);
      return;
    }

    this.selectedKey.set(key);
    this.selectedEntry.set(entry);
    this.selectedResult.set(null);
    this.loadingDetail.set(true);
    this.detailError.set(null);

    const detail$ =
      entry.flashId != null
        ? this.dailyService.getFlash(this.numericGroupId(), entry.flashId)
        : this.questionsService.getByDate(this.numericGroupId(), entry.date);
    detail$.subscribe({
      next: (result) => {
        this.selectedResult.set(result);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.detailError.set('history.noResults');
        this.loadingDetail.set(false);
      },
    });
  }

  // Called by the date picker. Resets the picker immediately so the trigger always
  // shows the placeholder ("Ir a una fecha..."), then selects the entry.
  // If the chosen date isn't in the currently-loaded page, a synthetic row is
  // inserted so the inline detail panel has somewhere to render.
  onDatePickerChange(date: Date | undefined) {
    this.pickerDate.set(undefined);
    if (!date) return;
    this.resetLateVote();

    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate(), 12);
    const dateStr = d.toISOString().slice(0, 10);

    const loaded = this.entries().find(e => e.date === dateStr && e.flashId == null);
    if (loaded) {
      this.selectEntry(loaded);
      return;
    }

    // Date not yet in the list — fetch its result and inject a synthetic entry.
    this.selectedKey.set(dateStr);
    this.selectedResult.set(null);
    this.loadingDetail.set(true);
    this.detailError.set(null);

    this.questionsService.getByDate(this.numericGroupId(), dateStr).subscribe({
      next: (result) => {
        const synthetic: HistoryEntry = {
          date: dateStr,
          questionText: result.text,
          type: result.type,
          totalVotes: result.totalVotes,
          authorUsername: result.author?.username ?? null,
          myGuessCorrect: null, // the detail doesn't carry your guess; the list row does
        };
        this.selectedEntry.set(synthetic);
        this.entries.update(prev => {
          const insertAt = prev.findIndex(e => e.date < dateStr);
          if (insertAt === -1) return [...prev, synthetic];
          return [...prev.slice(0, insertAt), synthetic, ...prev.slice(insertAt)];
        });
        this.selectedResult.set(result);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.detailError.set('history.noResults');
        this.loadingDetail.set(false);
        this.selectedKey.set(null);
      },
    });
  }

  openLateVote() {
    this.showLateVote.set(true);
    if (this.members().length > 0) return;
    this.groupsService.getGroupMembers(this.numericGroupId()).subscribe({
      next: (members) => this.members.set(members),
    });
  }

  submitLateVote(dto: CreateVote) {
    const entry = this.selectedEntry();
    if (!entry) return;
    const key = historyKey(entry);
    const groupId = this.numericGroupId();

    // A flash question's late vote returns nothing: re-read its (now unlocked) detail.
    const vote$ =
      entry.flashId != null
        ? this.dailyService
            .voteFlash(groupId, entry.flashId, dto)
            .pipe(switchMap(() => this.dailyService.getFlash(groupId, entry.flashId!)))
        : this.questionsService.lateVote(groupId, entry.date, dto);

    this.lateVoteError.set(null);
    this.lateVoting.set(true);
    vote$.subscribe({
      next: (result) => {
        this.lateVoting.set(false);
        this.showLateVote.set(false);
        this.selectedResult.set(result);
        this.entries.update((prev) =>
          prev.map((e) => (historyKey(e) === key ? { ...e, totalVotes: result.totalVotes } : e)),
        );
      },
      error: (err) => {
        this.lateVoting.set(false);
        this.lateVoteError.set(apiErrorMessage(err, this.t('history.lateVoteFailed')));
      },
    });
  }

  private resetLateVote() {
    this.showLateVote.set(false);
    this.lateVoting.set(false);
    this.lateVoteError.set(null);
  }

  // Builds a minimal QuestionToVote from a QuestionResult so that app-results can
  // dispatch on type and render the Scale distribution correctly (needs rangeMin/rangeMax).
  syntheticQuestion(result: QuestionResult): QuestionToVote {
    return {
      id: result.id,
      text: result.text,
      type: result.type,
      rangeMin: result.rangeMin ?? 1,
      rangeMax: result.rangeMax ?? 10,
      allowNobody: false,
      blacklistedUserIds: [],
      minSelections: null,
      maxSelections: null,
      targetUserId: null,
      allowOther: false,
      teams: [],
      options: [],
    };
  }

  // Date formatting helpers — all take a YYYY-MM-DD string.
  // Append T12:00:00 to avoid UTC midnight shifting the date in negative-offset locales.

  formatFullDate(dateStr: string): string {
    const d = new Date(`${dateStr}T12:00:00`);
    return this.i18n.formatDate(d, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }

  getDayName(dateStr: string): string {
    const d = new Date(`${dateStr}T12:00:00`);
    return this.i18n.formatDate(d, { weekday: 'short' });
  }

  getDayNumber(dateStr: string): string {
    return dateStr.split('-')[2];
  }

  getMonthName(dateStr: string): string {
    const d = new Date(`${dateStr}T12:00:00`);
    return this.i18n.formatDate(d, { month: 'short' });
  }

  questionTypeLabel(type: QuestionType): string {
    return this.t(QUESTION_TYPE_LABELS[type]);
  }

}
