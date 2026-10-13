import { Component, computed, input } from '@angular/core';

import { BADGE_EMOJI, Look, nameColorClass } from '../../../core/constants/shop-catalog';

// A member's name with their shop name colour and badge emoji (rama 42, §17.10). Shown in
// the chat, the member list, results/voters and the member card.
@Component({
  selector: 'app-user-name',
  host: { class: 'inline-flex min-w-0 max-w-full items-baseline gap-1' },
  template: `
    <span class="truncate" [class]="colorClass()">{{ name() }}</span>
    @if (badge()) {
      <span class="shrink-0" aria-hidden="true">{{ badge() }}</span>
    }
  `,
})
export class UserNameComponent {
  readonly name = input<string | null | undefined>('');
  readonly look = input<Look | null | undefined>(null);

  protected readonly colorClass = computed(() => nameColorClass(this.look()?.nameColor));
  protected readonly badge = computed(() => BADGE_EMOJI[this.look()?.badge ?? ''] ?? null);
}
