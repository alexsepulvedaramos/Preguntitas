import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { StreakTierKey } from '../../../core/constants/streak-tiers';
import { FinishKey } from './ring-finishes';
import { renderStreakFrame } from './streak-frame-renderer';

let nextId = 0;

// Streak frame around an avatar (rama 19): the tier's material, its shine and ornaments, a
// shop finish on top (rama 57), plus the crown for the group's best streak. Absolutely
// positioned over a box twice the avatar's size; it sets no z-index so its layers interleave
// with the avatar's face (back layers below, the finish and the ribbon above).
@Component({
  selector: 'app-streak-frame',
  template: '',
  host: {
    class: 'vp-frame',
    'aria-hidden': 'true',
    '[innerHTML]': 'markup()',
  },
})
export class StreakFrameComponent {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly uid = `vpf${nextId++}`;

  readonly tier = input<StreakTierKey | null>(null);
  // A shop finish drawn over the tier's material (rama 57).
  readonly finish = input<FinishKey | null>(null);
  readonly crown = input(false);
  readonly lite = input(true);
  readonly ribbon = input<number | null>(null);

  // The markup is built only from our own constants (see renderStreakFrame), never from
  // user input, so bypassing sanitization is safe and keeps the SVG gradients and styles.
  protected readonly markup = computed<SafeHtml>(() =>
    this.sanitizer.bypassSecurityTrustHtml(
      renderStreakFrame({
        tier: this.tier(),
        finish: this.finish(),
        crown: this.crown(),
        lite: this.lite(),
        ribbon: this.ribbon(),
        uid: this.uid,
      })
    )
  );
}
