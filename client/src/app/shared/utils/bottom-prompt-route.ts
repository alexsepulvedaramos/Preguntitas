import { Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';

// The floating bottom prompts (install the app, turn on notifications) stay off the group
// screens: there the vote button and the chat composer live at the bottom and the prompt
// would cover them (UX audit T9). They show on the group list, profile, shop and info pages.
export function injectHideBottomPrompts(): Signal<boolean> {
  const router = inject(Router);
  const url = toSignal(
    router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects),
    ),
    { initialValue: router.url },
  );
  return computed(() => /^\/(groups\/[^/?#]+|join|demo)/.test(url()));
}
