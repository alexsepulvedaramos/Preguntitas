import { Component, computed, input } from '@angular/core';

import { Look, nameColorClass } from '../../../core/constants/shop-catalog';

// A member's name in their shop name colour (rama 42, §17.10). Shown in the chat, the member
// list, results/voters and the member card.
@Component({
  selector: 'app-user-name',
  host: { class: 'inline-flex min-w-0 max-w-full items-baseline gap-1' },
  template: `<span class="truncate" [class]="colorClass()">{{ name() }}</span>`,
})
export class UserNameComponent {
  readonly name = input<string | null | undefined>('');
  readonly look = input<Look | null | undefined>(null);

  protected readonly colorClass = computed(() => nameColorClass(this.look()?.nameColor));
}
