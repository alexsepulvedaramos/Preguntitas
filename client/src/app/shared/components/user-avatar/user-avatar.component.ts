import { Component, input, computed, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';

import {
  FRAME_SENIORITY,
  FrameLook,
  StreakTierKey,
  frameFollowsStreak,
  frameColorVar,
  isColorFrame,
  streakTierFor,
} from '../../../core/constants/streak-tiers';
import { ACCESSORY_ART, Look, ringSkinLook, seniorityStage } from '../../../core/constants/shop-catalog';
import { needsLightBackdrop } from '../../utils/avatar-backdrop';
import { StreakFrameComponent } from '../streak-frame/streak-frame.component';

type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

// Accessories drawn across the middle of the face push the initials below or above them.
const INITIALS_SHIFT: Record<string, string> = {
  'acc-sunglasses': '42%',
  'acc-moustache': '-38%',
};

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
        [style.box-shadow]="colorFrame() ? '0 0 0 3px ' + colorFrame() : null"
      >
        <hlm-avatar [class]="sizeClass()">
          @if (avatarUrl()) {
            <img hlmAvatarImage [src]="avatarUrl()!" [alt]="username() + ' avatar'" class="object-cover w-full h-full" />
          }
          <span hlmAvatarFallback class="font-semibold bg-primary/15 text-primary-text" [class]="fallbackSizeClass()">
            <!-- With an accessory, the initials step out of its way (UX audit T8) -->
            <span class="inline-block" [style.transform]="initialsShift()">
              @if (initials()) {
                {{ initials() }}
              } @else {
                ?
              }
            </span>
          </span>
        </hlm-avatar>
      </div>
      @if (accessory()) {
        <div class="vp-accessory absolute inset-0 z-[6] pointer-events-none" aria-hidden="true" [innerHTML]="accessory()"></div>
      }
    </div>
  `,
})
export class UserAvatarComponent {
  private readonly sanitizer = inject(DomSanitizer);

  readonly avatarUrl = input<string | null | undefined>(null);
  readonly username = input<string>('');
  // A palette colour key ('purple', …), 'streak' (or null: frame follows the streak tier), 'seniority' or 'none'.
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

  protected readonly colorFrame = computed(() => {
    const frame = this.frameColor();
    return isColorFrame(frame) ? frameColorVar(frame) : null;
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
  // Not on the two smallest sizes: at 24–32 px the art is only a smudge over the face.
  protected readonly accessory = computed<SafeHtml | null>(() => {
    if (this.size() === 'xs' || this.size() === 'sm') return null;
    const art = ACCESSORY_ART[this.look()?.accessory ?? ''];
    return art
      ? this.sanitizer.bypassSecurityTrustHtml(`<svg viewBox="0 0 100 100" width="100%" height="100%">${art}</svg>`)
      : null;
  });

  // Where the initials move so the accessory doesn't cover them (no photo only).
  protected readonly initialsShift = computed(() => {
    if (this.avatarUrl() || !this.accessory()) return null;
    const shift = INITIALS_SHIFT[this.look()?.accessory ?? ''];
    return shift ? `translateY(${shift}) scale(0.85)` : null;
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
