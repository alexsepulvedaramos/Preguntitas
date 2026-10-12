import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';

import { WordStaggerPipe } from '../../../../shared/pipes/word-stagger.pipe';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';

/** Real seeded pack questions, shown as a teaser on the login page. */
const SHOWCASE_QUESTIONS: TranslationKey[] = [
  'auth.showcase.question1',
  'auth.showcase.question2',
  'auth.showcase.question3',
  'auth.showcase.question4',
  'auth.showcase.question5',
];

@Component({
  selector: 'app-rotating-question',
  imports: [WordStaggerPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-2 text-center">
      <p class="text-xs font-medium uppercase tracking-widest text-muted-foreground">
        {{ t('auth.showcase.eyebrow') }}
      </p>
      <div class="flex min-h-[6rem] items-center justify-center">
        <h1
          class="vp-question-text text-2xl md:text-3xl"
          [innerHTML]="t(question()) | wordStagger"
        ></h1>
      </div>
      <p class="text-sm text-muted-foreground">
        {{ t('auth.showcase.tagline') }}
      </p>
    </div>
  `,
})
export class RotatingQuestionComponent implements OnInit, OnDestroy {
  private static readonly ROTATION_MS = 4500;

  protected readonly t = inject(I18nService).t;
  private index = 0;
  private intervalId?: ReturnType<typeof setInterval>;

  protected readonly question = signal(SHOWCASE_QUESTIONS[0]);

  ngOnInit(): void {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    this.intervalId = setInterval(() => {
      this.index = (this.index + 1) % SHOWCASE_QUESTIONS.length;
      this.question.set(SHOWCASE_QUESTIONS[this.index]);
    }, RotatingQuestionComponent.ROTATION_MS);
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }
}
