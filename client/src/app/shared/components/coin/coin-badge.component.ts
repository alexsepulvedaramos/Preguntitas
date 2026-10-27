import { Component, computed, inject, input } from '@angular/core';

import { I18nService } from '../../../core/i18n/i18n.service';
import { CoinTextComponent } from './coin-text.component';

// «+N 🪙» pinned to the top-right corner of a button that pays coins (§17.16, §17.17): the
// button needs `relative`. Decorative: the button's own label says what it does.
@Component({
  selector: 'app-coin-badge',
  imports: [CoinTextComponent],
  template: `<span
    class="pointer-events-none absolute -top-2.5 -right-1.5 rounded-full bg-accent px-1.5 py-0.5 text-xs font-semibold leading-none tabular-nums text-accent-foreground shadow-sm ring-2 ring-background"
    ><app-coin-text [text]="text()"
  /></span>`,
})
export class CoinBadgeComponent {
  private readonly t = inject(I18nService).t;
  readonly coins = input.required<number>();

  protected readonly text = computed(() => this.t('coins.reward', { coins: this.coins() }));
}
