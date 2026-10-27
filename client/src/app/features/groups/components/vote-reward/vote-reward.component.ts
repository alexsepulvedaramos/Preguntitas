import { Component, Signal, computed, inject } from '@angular/core';

import { CoinBadgeComponent } from '../../../../shared/components/coin/coin-badge.component';

// What the vote being cast pays, provided by `app-vote` to the per-type pickers inside it
// (§17.17: every action that pays coins shows it). null when it pays nothing (a flash).
export abstract class VoteRewardSource {
  abstract readonly reward: Signal<number | null>;
}

// «+N 🪙» on a vote button; the button needs `relative`. Draws nothing outside `app-vote`.
@Component({
  selector: 'app-vote-reward',
  imports: [CoinBadgeComponent],
  template: `@if (coins(); as coins) {<app-coin-badge [coins]="coins" />}`,
})
export class VoteRewardComponent {
  private readonly source = inject(VoteRewardSource, { optional: true });
  protected readonly coins = computed(() => this.source?.reward() ?? null);
}
