import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, signal } from '@angular/core';

import { WordStaggerPipe } from '../../../../shared/pipes/word-stagger.pipe';

/** Real seeded pack questions, shown as a teaser on the login page. */
const SHOWCASE_QUESTIONS = [
  '¿Quién fundaría una secta totalmente por accidente?',
  'Del 1 al 10, ¿qué tan buen conductor te consideras?',
  '¿Quién será la primera persona en divorciarse?',
  'Si cambias una a una todas las piezas de un barco, ¿sigue siendo el mismo barco?',
  'Top 3 frutos secos',
];

@Component({
  selector: 'app-rotating-question',
  imports: [WordStaggerPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-2 text-center">
      <p class="text-xs font-medium uppercase tracking-widest text-muted-foreground">
        Hoy podría tocaros…
      </p>
      <div class="flex min-h-[6rem] items-center justify-center">
        <h1
          class="vp-question-text text-2xl md:text-3xl"
          [innerHTML]="question() | wordStagger"
        ></h1>
      </div>
      <p class="text-sm text-muted-foreground">
        Una pregunta al día sobre tu grupo. Vota y descubre quién ha votado a quién.
      </p>
    </div>
  `,
})
export class RotatingQuestionComponent implements OnInit, OnDestroy {
  private static readonly ROTATION_MS = 4500;

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
