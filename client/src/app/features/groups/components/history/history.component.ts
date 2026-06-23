import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCalendar } from '@ng-icons/lucide';

import { QuestionsService } from '../../../../core/services/questions.service';
import { HistoryEntry } from '../../../../core/models/question.model';
import { QuestionResult } from '../../../../core/models/result.model';
import { QuestionToVote } from '../../../../core/models/question.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { ResultsComponent } from '../results/results.component';

@Component({
  selector: 'app-history',
  imports: [
    RouterLink,
    HlmButtonImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    HlmDatePickerImports,
    NgIcon,
    ResultsComponent,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCalendar })],
  templateUrl: './history.component.html',
})
export class HistoryComponent implements OnInit {
  private readonly questionsService = inject(QuestionsService);

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  // History list state
  public readonly entries = signal<HistoryEntry[]>([]);
  public readonly hasMore = signal(false);
  public readonly loading = signal(true);
  public readonly loadingMore = signal(false);
  public readonly error = signal<string | null>(null);

  // Detail state — the currently-expanded date and its full results.
  public readonly selectedDate = signal<string | null>(null);
  public readonly selectedResult = signal<QuestionResult | null>(null);
  public readonly loadingDetail = signal(false);
  public readonly detailError = signal<string | null>(null);

  // Date picker binding (JS Date, reset after each use so the trigger always shows the placeholder).
  public readonly pickerDate = signal<Date | undefined>(undefined);

  protected readonly QuestionType = QuestionType;

  ngOnInit() {
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
      },
      error: () => {
        this.error.set('No se ha podido cargar el historial.');
        this.loading.set(false);
      },
    });
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
  selectEntry(date: string) {
    if (this.selectedDate() === date) {
      this.selectedDate.set(null);
      this.selectedResult.set(null);
      return;
    }

    this.selectedDate.set(date);
    this.selectedResult.set(null);
    this.loadingDetail.set(true);
    this.detailError.set(null);

    this.questionsService.getByDate(this.numericGroupId(), date).subscribe({
      next: (result) => {
        this.selectedResult.set(result);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.detailError.set('No hay resultados para esa fecha.');
        this.loadingDetail.set(false);
      },
    });
  }

  // Called by the date picker. Resets the picker immediately so the trigger always
  // shows the placeholder ("Ir a una fecha..."), and delegates to selectEntry.
  onDatePickerChange(date: Date | undefined) {
    // Reset the picker signal so the trigger placeholder is restored next render.
    this.pickerDate.set(undefined);

    if (!date) return;
    // YYYY-MM-DD in local time (using noon to avoid timezone-offset edge cases).
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate(), 12);
    const dateStr = d.toISOString().slice(0, 10);
    this.selectEntry(dateStr);
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
    return d.toLocaleDateString('es-ES', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }

  getDayName(dateStr: string): string {
    const d = new Date(`${dateStr}T12:00:00`);
    return d.toLocaleDateString('es-ES', { weekday: 'short' });
  }

  getDayNumber(dateStr: string): string {
    return dateStr.split('-')[2];
  }

  getMonthName(dateStr: string): string {
    const d = new Date(`${dateStr}T12:00:00`);
    return d.toLocaleDateString('es-ES', { month: 'short' });
  }

  questionTypeLabel(type: QuestionType): string {
    return QUESTION_TYPE_LABELS[type];
  }

  questionTypeBadgeClass(type: QuestionType): string {
    return QUESTION_TYPE_BADGE_CLASS[type];
  }
}
