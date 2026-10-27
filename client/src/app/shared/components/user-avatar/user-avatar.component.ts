import { Component, input, computed } from '@angular/core';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import {
  StreakTierKey,
  frameFollowsStreak,
  frameColorVar,
  isColorFrame,
  streakTierFor,
} from '../../../core/constants/streak-tiers';
import { Look } from '../../../core/constants/shop-catalog';
import { needsLightBackdrop } from '../../utils/avatar-backdrop';
import { StreakFrameComponent } from '../streak-frame/streak-frame.component';
import { FinishKey, isFinish } from '../streak-frame/ring-finishes';

type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

// How much of a ring finish shows (rama 57): 'auto' = the full one on lg/xl and a lighter one
// (still animated, still growing with the tier) on smaller sizes; 'full' wherever the person
// is the point (a voting option, a free-text answer); 'ring' = the tier ring only, for the
// small "who has voted" dots.
export type AvatarEffects = 'auto' | 'full' | 'ring';

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [HlmAvatarImports, StreakFrameComponent],
  template: `
    <div class="relative isolate rounded-full shrink-0" [class]="sizeClass()">
      @if (tier() || finish() || crown()) {
        <app-streak-frame [tier]="tier()" [finish]="finish()" [crownFinish]="crownFinish()" [crown]="!!crown()" [lite]="lite()" [ribbon]="ribbon()" />
      }
      <div
        class="relative z-[5] rounded-full shrink-0 overflow-hidden ring-1 ring-border/40"
        [class]="sizeClass() + (lightBackdrop() ? ' bg-white' : ' bg-card')"
        [style.box-shadow]="colorFrame() ? '0 0 0 3px ' + colorFrame() : null"
      >
        <hlm-avatar [class]="sizeClass()">
          @if (avatarUrl()) {
            <img hlmAvatarImage [src]="avatarUrl()!" [alt]="username() + ' avatar'" class="object-cover w-full h-full" />
          }
          <span hlmAvatarFallback class="font-semibold bg-primary/15 text-primary-text" [class]="fallbackSizeClass()">
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
  // A palette colour key ('purple', …), 'streak' (or null: frame follows the streak tier) or 'none'.
  readonly frameColor = input<string | null | undefined>(null);
  // The member's streak in the current context (rama 19): their streak in the group
  // when shown inside one, their highest current streak outside. null = unknown.
  readonly streak = input<number | null | undefined>(null);
  // Crown for the group's best current streak — shown whatever the frame setting.
  readonly crown = input<boolean | null | undefined>(false);
  // Equipped shop cosmetics (rama 42): the ring finish is drawn here (rama 57).
  readonly look = input<Look | null | undefined>(null);
  readonly effects = input<AvatarEffects>('auto');
  // Day-count ribbon on the largest size (off where the count is already shown).
  readonly showRibbon = input(true);
  readonly size = input<AvatarSize>('md');

  protected readonly initials = computed(() =>
    this.username().slice(0, 2).toUpperCase()
  );

  protected readonly lightBackdrop = computed(() => needsLightBackdrop(this.avatarUrl()));

  protected readonly colorFrame = computed(() => {
    const frame = this.frameColor();
    return isColorFrame(frame) ? frameColorVar(frame) : null;
  });

  protected readonly tier = computed<StreakTierKey | null>(() =>
    frameFollowsStreak(this.frameColor()) ? (streakTierFor(this.streak())?.key ?? null) : null
  );

  // A finish is drawn over the streak ring, so only while the frame follows the streak.
  protected readonly finish = computed<FinishKey | null>(() => {
    const key = this.look()?.ringSkin;
    return this.effects() !== 'ring' && frameFollowsStreak(this.frameColor()) && isFinish(key) ? key : null;
  });

  // Where only the tier ring is drawn, the crown still wears the finish's style.
  protected readonly crownFinish = computed<FinishKey | null>(() => {
    const key = this.look()?.ringSkin;
    return frameFollowsStreak(this.frameColor()) && isFinish(key) ? key : null;
  });

  // Full frames (particles, ornaments, icons) only where the avatar is big enough to show
  // them, or where it is the point of the screen.
  protected readonly lite = computed(
    () => this.effects() !== 'full' && this.size() !== 'lg' && this.size() !== 'xl'
  );

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
    xs: 'text-2xs',
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-lg',
    xl: 'text-2xl',
  })[this.size()]);
}
