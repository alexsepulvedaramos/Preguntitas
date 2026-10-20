import { Component, input, computed, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import {
  FRAME_SENIORITY,
  FrameLook,
  StreakTierKey,
  frameFollowsStreak,
  isHexFrame,
  streakTierFor,
} from '../../../core/constants/streak-tiers';
import { ACCESSORY_ART, Look, ringSkinLook, seniorityStage } from '../../../core/constants/shop-catalog';
import { needsLightBackdrop } from '../../utils/avatar-backdrop';
import { StreakFrameComponent } from '../streak-frame/streak-frame.component';

type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-user-avatar',
  standalone: true,
  imports: [HlmAvatarImports, StreakFrameComponent],
  template: `
    <div class="relative isolate rounded-full shrink-0" [class]="sizeClass()">
      @if (tier() || frameLook() || crown()) {
        <app-streak-frame [tier]="tier()" [look]="frameLook()" [crown]="!!crown()" [lite]="lite()" [ribbon]="ribbon()" />
      }
      <div
        class="relative z-[5] rounded-full shrink-0 overflow-hidden ring-1 ring-border/40"
        [class]="sizeClass() + (lightBackdrop() ? ' bg-white' : ' bg-card')"
        [style.box-shadow]="hexFrame() ? '0 0 0 3px ' + hexFrame() : null"
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
      @if (accessory()) {
        <div class="absolute inset-0 z-[6] pointer-events-none" aria-hidden="true" [innerHTML]="accessory()"></div>
      }
    </div>
  `,
})
export class UserAvatarComponent {
  private readonly sanitizer = inject(DomSanitizer);

  readonly avatarUrl = input<string | null | undefined>(null);
  readonly username = input<string>('');
  // A hex colour, 'streak' (or null: frame follows the streak tier), 'seniority' or 'none'.
  readonly frameColor = input<string | null | undefined>(null);
  // The member's streak in the current context (rama 19): their streak in the group
  // when shown inside one, their highest current streak outside. null = unknown.
  readonly streak = input<number | null | undefined>(null);
  // Crown for the group's best current streak — shown whatever the frame setting.
  readonly crown = input<boolean | null | undefined>(false);
  // Equipped shop cosmetics (rama 42): ring skin and accessory are drawn here.
  readonly look = input<Look | null | undefined>(null);
  // When the member joined the group in context (outside one: their oldest group) — drives
  // the seniority frame.
  readonly memberSince = input<string | null | undefined>(null);
  // Day-count ribbon on the largest size (off where the count is already shown).
  readonly showRibbon = input(true);
  readonly size = input<AvatarSize>('md');

  protected readonly initials = computed(() =>
    this.username().slice(0, 2).toUpperCase()
  );

  protected readonly lightBackdrop = computed(() => needsLightBackdrop(this.avatarUrl()));

  protected readonly hexFrame = computed(() => {
    const frame = this.frameColor();
    return isHexFrame(frame) ? frame : null;
  });

  protected readonly tier = computed<StreakTierKey | null>(() =>
    frameFollowsStreak(this.frameColor()) ? (streakTierFor(this.streak())?.key ?? null) : null
  );

  // A ring skin restyles the streak ring; the seniority frame replaces it.
  protected readonly frameLook = computed<FrameLook | null>(() => {
    if (this.frameColor() === FRAME_SENIORITY) return seniorityStage(this.memberSince())?.look ?? null;
    const tier = this.tier();
    return tier ? ringSkinLook(this.look()?.ringSkin, tier) : null;
  });

  // The markup comes only from our own constants (ACCESSORY_ART), never from user input.
  protected readonly accessory = computed<SafeHtml | null>(() => {
    const art = ACCESSORY_ART[this.look()?.accessory ?? ''];
    return art
      ? this.sanitizer.bypassSecurityTrustHtml(`<svg viewBox="0 0 100 100" width="100%" height="100%">${art}</svg>`)
      : null;
  });

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
    xs: 'text-2xs',
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-lg',
    xl: 'text-2xl',
  })[this.size()]);
}
