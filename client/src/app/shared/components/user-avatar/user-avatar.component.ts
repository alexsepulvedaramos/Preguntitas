import { Component, input, computed } from '@angular/core';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import {
  StreakTierKey,
  frameFollowsStreak,
  isHexFrame,
  streakTierFor,
} from '../../../core/constants/streak-tiers';
import { StreakFrameComponent } from '../streak-frame/streak-frame.component';

type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [HlmAvatarImports, StreakFrameComponent],
  template: `
    <div class="relative isolate rounded-full shrink-0" [class]="sizeClass()">
      @if (tier() || crown()) {
        <app-streak-frame [tier]="tier()" [crown]="!!crown()" [lite]="lite()" [ribbon]="ribbon()" />
      }
      <div
        class="relative z-[5] rounded-full shrink-0 overflow-hidden bg-card ring-1 ring-border/40"
        [class]="sizeClass()"
        [style.box-shadow]="hexFrame() ? '0 0 0 3px ' + hexFrame() : null"
      >
        <hlm-avatar [class]="sizeClass()">
          @if (avatarUrl()) {
            <img hlmAvatarImage [src]="avatarUrl()!" [alt]="username() + ' avatar'" class="object-cover w-full h-full" />
          }
          <span hlmAvatarFallback class="font-semibold bg-primary/15 text-primary" [class]="fallbackSizeClass()">
            @if (initials()) {
              {{ initials() }}
            } @else {
              ?
            }
          </span>
        </hlm-avatar>
      </div>
    </div>
  `,
})
export class UserAvatarComponent {
  readonly avatarUrl = input<string | null | undefined>(null);
  readonly username = input<string>('');
  // A hex colour, 'streak' (or null: frame follows the streak tier) or 'none'.
  readonly frameColor = input<string | null | undefined>(null);
  // The member's streak in the current context (rama 19): their streak in the group
  // when shown inside one, their highest current streak outside. null = unknown.
  readonly streak = input<number | null | undefined>(null);
  // Crown for the group's best current streak — shown whatever the frame setting.
  readonly crown = input<boolean | null | undefined>(false);
  // Day-count ribbon on the largest size (off where the count is already shown).
  readonly showRibbon = input(true);
  readonly size = input<AvatarSize>('md');

  protected readonly initials = computed(() =>
    this.username().slice(0, 2).toUpperCase()
  );

  protected readonly hexFrame = computed(() => {
    const frame = this.frameColor();
    return isHexFrame(frame) ? frame : null;
  });

  protected readonly tier = computed<StreakTierKey | null>(() =>
    frameFollowsStreak(this.frameColor()) ? (streakTierFor(this.streak())?.key ?? null) : null
  );

  // Full frames (particles, ornaments) only where the avatar is big enough to show them.
  protected readonly lite = computed(() => this.size() !== 'lg' && this.size() !== 'xl');

  protected readonly ribbon = computed(() =>
    this.size() === 'xl' && this.showRibbon() && this.tier() ? (this.streak() ?? null) : null
  );

  protected readonly sizeClass = computed(() => ({
    xs: 'size-6',
    sm: 'size-8',
    md: 'size-10',
    lg: 'size-14',
    xl: 'size-24',
  })[this.size()]);

  protected readonly fallbackSizeClass = computed(() => ({
    xs: 'text-[9px]',
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-lg',
    xl: 'text-2xl',
  })[this.size()]);
}
