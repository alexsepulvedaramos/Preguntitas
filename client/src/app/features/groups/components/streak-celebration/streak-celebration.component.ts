import {
  AfterViewInit, Component, ElementRef, OnDestroy, OnInit, computed, inject, input, output, signal, viewChild,
} from '@angular/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { StreakUpdate } from '../../../../core/models/streak.model';
import { FIRST_RING_DAYS, FRAME_LOOKS, FRAME_STREAK, STREAK_FRAME_KEYS, STREAK_TIERS } from '../../../../core/constants/streak-tiers';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';

const COUNT_UP_MS = 600;
// Accent colours before the first frame (days 1–2): the brand's lime and sage.
const NO_TIER_COLORS = { a: '#7bd88f', b: '#c6f36b', core: '#ffffff' };

// Full-screen celebration after a vote that extends the streak (rama 19): a dark scene
// with rays, the frame "forged" in place, the count popping up and embers rising. Tier-ups
// and milestones add confetti. Days 1–2 show progress dots towards the first frame; later
// days a bar towards the next tier. Tap anywhere (or Esc) to dismiss.
@Component({
  selector: 'app-streak-celebration',
  imports: [UserAvatarComponent],
  templateUrl: './streak-celebration.component.html',
  host: {
    '(document:keydown.escape)': 'dismiss()',
  },
})
export class StreakCelebrationComponent implements OnInit, AfterViewInit, OnDestroy {
  protected readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  public readonly update = input.required<StreakUpdate>();
  // Who is celebrating — defaults to the signed-in user; the /demo walkthrough (rama 25)
  // passes its guest visitor instead.
  public readonly username = input<string | null>(null);
  public readonly avatarUrl = input<string | null>(null);
  public readonly dismissed = output<void>();

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('particles');

  protected readonly displayedCount = signal(0);
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly firstRingDays = FIRST_RING_DAYS;

  private readonly reducedMotion =
    typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  private countUpTimer: ReturnType<typeof setTimeout> | null = null;
  private active = true;

  protected readonly shownUsername = computed(
    () => this.username() ?? this.authService.currentUser()?.username ?? ''
  );
  protected readonly shownAvatarUrl = computed(() =>
    this.username() != null ? this.avatarUrl() : (this.authService.currentUser()?.avatarUrl ?? null)
  );

  protected readonly colors = computed(() => {
    const key = this.update().tierKey;
    return key ? FRAME_LOOKS[key] : NO_TIER_COLORS;
  });

  protected readonly isCelebratory = computed(
    () => this.update().isTierUp || !!this.update().milestoneLabel
  );

  protected readonly title = computed(() => {
    const u = this.update();
    if (u.milestoneLabel) return u.milestoneLabel;
    if (u.isTierUp && u.tierTitle) return this.t('streaks.celebration.tierTitle', { title: u.tierTitle });
    if (u.current === 1) return this.t('streaks.celebration.started');
    return this.t('streaks.celebration.keepGoing');
  });

  protected readonly subtitle = computed(() => {
    const u = this.update();
    const frame = u.tierKey ? this.t(STREAK_FRAME_KEYS[u.tierKey]) : '';
    if (u.isTierUp && u.current === FIRST_RING_DAYS) return this.t('streaks.celebration.firstTitle', { frame });
    if (u.isTierUp && u.titleUnlocked) return this.t('streaks.celebration.newTitle', { frame });
    if (u.isTierUp) return this.t('streaks.celebration.frameBack', { frame });
    if (u.current === 1) return this.t('streaks.celebration.voteTomorrow');
    return u.current === u.best && u.current > 1
      ? this.t('streaks.celebration.newRecord')
      : this.t('streaks.celebration.oneMoreDay');
  });

  // Days 1–2: ●○○ → ●●○ towards the first frame.
  protected readonly dots = computed(() =>
    Array.from({ length: FIRST_RING_DAYS }, (_, i) => i < this.update().current)
  );

  protected readonly progressPercent = computed(() => {
    const u = this.update();
    if (u.nextTierAt == null) return 100;
    const tierStart = [...STREAK_TIERS].reverse().find((t) => u.current >= t.minDays)?.minDays ?? 0;
    return Math.max(4, Math.round(((u.current - tierStart) / (u.nextTierAt - tierStart)) * 100));
  });

  protected readonly nextTierLabel = computed(() => {
    const u = this.update();
    if (u.daysToNextTier == null) return this.t('streaks.celebration.top');
    const days = this.t('common.days', { count: u.daysToNextTier });
    return u.current < FIRST_RING_DAYS
      ? this.t('streaks.celebration.toFirstTitle', { days })
      : this.t('streaks.celebration.toNextTitle', { days, title: u.nextTierTitle ?? '' });
  });

  ngOnInit(): void {
    const { previous, current } = this.update();
    if (this.reducedMotion || current - previous <= 0) {
      this.displayedCount.set(current);
    } else {
      this.displayedCount.set(previous);
      this.countUpTimer = setTimeout(() => this.displayedCount.set(current), COUNT_UP_MS);
    }
  }

  ngAfterViewInit(): void {
    const canvas = this.canvas()?.nativeElement;
    if (!canvas || this.reducedMotion) return;
    const c = this.colors();
    import('../../../../shared/utils/confetti').then((m) =>
      m.playCelebrationParticles(canvas, [c.a, c.b, c.core], this.isCelebratory(), () => this.active)
    );
  }

  ngOnDestroy(): void {
    this.active = false;
    if (this.countUpTimer) clearTimeout(this.countUpTimer);
  }

  dismiss(): void {
    this.dismissed.emit();
  }
}
