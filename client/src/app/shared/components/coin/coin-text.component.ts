import { Component, computed, inject, input } from '@angular/core';

import { I18nService } from '../../../core/i18n/i18n.service';
import { CoinComponent } from './coin.component';

// The coin marker in the dictionaries: translations write prices as "{price} 🪙" and this
// component draws the app's coin in its place. Anywhere a string is shown raw (a toast, an
// aria-label) the marker still reads as a coin.
export const COIN_MARKER = '🪙';

// A translated text with every COIN_MARKER drawn as the app's coin.
@Component({
  selector: 'app-coin-text',
  imports: [CoinComponent],
  template: `@for (part of parts(); track $index) {@if (part === marker) {<app-coin [label]="label()" />} @else {<span>{{ part }}</span>}}`,
})
export class CoinTextComponent {
  protected readonly t = inject(I18nService).t;
  readonly text = input.required<string>();

  protected readonly marker = COIN_MARKER;
  protected readonly label = computed(() => this.t('coins.unit'));
  protected readonly parts = computed(() =>
    this.text()
      .split(COIN_MARKER)
      .flatMap((part, i) => (i === 0 ? [part] : [COIN_MARKER, part]))
      .filter(part => part !== ''),
  );
}
