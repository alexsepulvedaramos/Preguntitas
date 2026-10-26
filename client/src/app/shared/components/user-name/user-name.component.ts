import { Component, input } from '@angular/core';

// A member's name (rama 42). It carried the shop name colour until rama 57 retired it; kept as
// the one place names are drawn, truncating to the space they get.
@Component({
  selector: 'app-user-name',
  host: { class: 'inline-flex min-w-0 max-w-full items-baseline gap-1' },
  template: `<span class="truncate">{{ name() }}</span>`,
})
export class UserNameComponent {
  readonly name = input<string | null | undefined>('');
}
