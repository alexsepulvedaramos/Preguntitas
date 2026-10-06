import { Component, input, computed } from '@angular/core';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import {
  StreakTierKey,
  frameFollowsStreak,
  isHexFrame,
  streakTierFor,
} from '../../../core/constants/streak-tiers';

interface FlameTongue {
  angle: number;
  delay: number;
  duration: number;
}

// Flame tongues around the top of the ring per tier (ember has none).
const TONGUE_ANGLES: Record<StreakTierKey, number[]> = {
  ember: [],
  'flame-small': [-30, 0, 30],
  'flame-intense': [-60, -30, 0, 30, 60],
  'flame-blue': [-60, -30, 0, 30, 60],
  'flame-purple': [-90, -60, -30, 0, 30, 60, 90],
  'flame-gold': [-90, -60, -30, 0, 30, 60, 90],
};

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [HlmAvatarImports],
  template: `
    <div class="relative rounded-full shrink-0" [class]="sizeClass()">
      <div
        class="rounded-full shrink-0 overflow-hidden bg-card ring-1 ring-border/40"
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

      @if (tier(); as t) {
        <span class="vp-streak" [class]="'vp-streak-' + t + ' vp-streak-size-' + size()" aria-hidden="true">
          <span class="vp-streak__glow"></span>
          <span class="vp-streak__band"></span>
          @for (tongue of tongues(); track $index) {
            <span
              class="vp-streak__tongue"
              [style.--angle]="tongue.angle + 'deg'"
              [style.--delay]="tongue.delay + 's'"
              [style.--dur]="tongue.duration + 's'"
            >
              <svg viewBox="0 0 10 16">
                <path class="vp-streak__outer" d="M5 0C7 4 10 7 10 11a5 5 0 0 1-10 0C0 7 3 4 5 0Z" />
                <path class="vp-streak__inner" d="M5 6C6 8 7.5 9.5 7.5 11.5a2.5 2.5 0 0 1-5 0C2.5 9.5 4 8 5 6Z" />
              </svg>
            </span>
          }
        </span>
      }
    </div>
  `,
})
export class UserAvatarComponent {
  readonly avatarUrl = input<string | null | undefined>(null);
  readonly username = input<string>('');
  // A hex colour, 'streak' (or null: ring follows the streak tier) or 'none'.
  readonly frameColor = input<string | null | undefined>(null);
  // The member's streak in the current context (rama 19): their streak in the group
  // when shown inside one, their highest current streak outside. null = unknown.
  readonly streak = input<number | null | undefined>(null);
  readonly size = input<'xs' | 'sm' | 'md' | 'lg' | 'xl'>('md');

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

  protected readonly tongues = computed<FlameTongue[]>(() => {
    const tier = this.tier();
    if (!tier) return [];
    // Deterministic per-tongue variation so the flames don't flicker in sync.
    return TONGUE_ANGLES[tier].map((angle, i) => ({
      angle,
      delay: (i * 0.37) % 0.9,
      duration: 0.7 + ((i * 0.23) % 0.5),
    }));
  });

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
