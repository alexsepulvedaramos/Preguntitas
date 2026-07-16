import { ChangeDetectionStrategy, Component } from '@angular/core';

interface ShowcaseRow {
  name: string;
  pct: number;
  color: string;
  voters: string[];
}

/** Fake looping results animation, shown as a product teaser on the register page. */
@Component({
  selector: 'app-results-showcase',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3 rounded-xl border border-border bg-card/60 p-4" aria-hidden="true">
      <p class="vp-question-text text-lg md:text-xl text-center">
        ¿Quién llegaría tarde a su propia boda?
      </p>

      <div class="space-y-2">
        @for (row of rows; track row.name) {
          <div class="flex items-center gap-2">
            <div
              class="flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-bold text-black/80"
              [style.background]="row.color"
            >
              {{ row.name[0] }}
            </div>
            <div class="relative h-8 flex-1 overflow-hidden rounded-lg bg-muted">
              <div
                class="vp-demo-fill absolute inset-y-0 left-0 rounded-lg"
                [style.background]="row.color"
                [style.max-width.%]="row.pct"
              ></div>
              <span
                class="absolute inset-y-0 left-2 flex items-center text-xs font-medium text-foreground"
                style="text-shadow: 0 1px 2px rgb(0 0 0 / 0.35)"
              >
                {{ row.name }}
              </span>
              <div class="vp-demo-voters absolute inset-y-0 right-2 flex items-center">
                @for (voter of row.voters; track voter) {
                  <div
                    class="-ml-1 flex size-5 items-center justify-center rounded-full border border-background bg-secondary text-[10px] font-semibold text-secondary-foreground"
                  >
                    {{ voter }}
                  </div>
                }
              </div>
            </div>
            <span class="w-10 shrink-0 text-right text-xs font-medium text-muted-foreground">
              {{ row.pct }}%
            </span>
          </div>
        }
      </div>

      <p class="text-center text-xs text-muted-foreground">
        Resultados en vivo: mira quién ha votado a quién
      </p>
    </div>
  `,
  styles: `
    .vp-demo-fill {
      width: 100%;
      animation: vp-demo-grow 6s cubic-bezier(0.16, 1, 0.3, 1) infinite;
    }
    .vp-demo-voters {
      animation: vp-demo-appear 6s ease infinite;
    }
    :host > div > div > div:nth-child(2) .vp-demo-fill {
      animation-delay: 0.2s;
    }
    :host > div > div > div:nth-child(3) .vp-demo-fill {
      animation-delay: 0.4s;
    }
    @keyframes vp-demo-grow {
      0% { width: 0; opacity: 1; }
      35%, 88% { width: 100%; opacity: 1; }
      96%, 100% { width: 100%; opacity: 0; }
    }
    @keyframes vp-demo-appear {
      0%, 40% { opacity: 0; transform: translateX(4px); }
      55%, 88% { opacity: 1; transform: translateX(0); }
      96%, 100% { opacity: 0; }
    }
    @media (prefers-reduced-motion: reduce) {
      .vp-demo-fill, .vp-demo-voters {
        animation: none;
      }
    }
  `,
})
export class ResultsShowcaseComponent {
  protected readonly rows: ShowcaseRow[] = [
    { name: 'Marta', pct: 54, color: 'var(--chart-1)', voters: ['D', 'L'] },
    { name: 'Dani', pct: 31, color: 'var(--chart-2)', voters: ['M'] },
    { name: 'Lucía', pct: 15, color: 'var(--chart-6)', voters: ['J'] },
  ];
}
