import { Component, computed, input } from '@angular/core';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import { OptionResult, Voter } from '../../../../core/models/result.model';

// Cycled by row index so categorical results (Custom Poll, Superlative, Secret Pairing,
// Deathmatch) get a distinct tone per option. Scale leaves `index` unset (always chart-1) —
// it's a single ordered distribution, not separate options, so varying the hue per bin
// would be misleading.
const CHART_COLORS = [
  'bg-chart-1',
  'bg-chart-2',
  'bg-chart-3',
  'bg-chart-4',
  'bg-chart-5',
  'bg-chart-6',
];

// Shared result row (rama 7, spec §13 row 7): a compact bar whose width is proportional to
// its percentage, used by every per-type results component. Tapping a row with at least one
// voter opens a dialog listing who voted for it ("who voted for what" — spec §4.6). Rows
// with no voters (e.g. an empty Scale bin) render as a static, non-interactive bar.
@Component({
  selector: 'app-result-option-bar',
  imports: [HlmDialogImports, HlmAvatarImports],
  templateUrl: './result-option-bar.component.html',
})
export class ResultOptionBarComponent {
  public readonly label = input.required<string>();
  public readonly subtitle = input<string | null>(null);
  public readonly voteCount = input.required<number>();
  public readonly percentage = input.required<number>();
  public readonly barWidth = input<number | null>(null);
  public readonly voters = input.required<Voter[]>();
  public readonly allResults = input<OptionResult[]>([]);
  public readonly index = input<number>(0);

  protected readonly hasAnyVoters = computed(() =>
    this.allResults().some(r => r.voters.length > 0)
  );
  protected readonly colorClass = computed(
    () => CHART_COLORS[this.index() % CHART_COLORS.length],
  );
  protected readonly displayWidth = computed(() => this.barWidth() ?? this.percentage());

  protected initials(username: string): string {
    return username.slice(0, 2).toUpperCase();
  }
}
