import {
  AfterViewInit, Component, ElementRef, OnDestroy, OnInit, computed, inject, input, output, signal, viewChild,
} from '@angular/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { StreakUpdate } from '../../../../core/models/streak.model';
import { FIRST_RING_DAYS, FRAME_LOOKS, FRAME_STREAK, STREAK_TIERS } from '../../../../core/constants/streak-tiers';
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

  public readonly update = input.required<StreakUpdate>();
  public readonly dismissed = output<void>();

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('particles');

  protected readonly displayedCount = signal(0);
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly firstRingDays = FIRST_RING_DAYS;

  private readonly reducedMotion =
    typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  private countUpTimer: ReturnType<typeof setTimeout> | null = null;
  private active = true;

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
    if (u.isTierUp && u.tierTitle) return `¡${u.tierTitle}!`;
    if (u.current === 1) return '¡Racha empezada!';
    return '¡Sigue así!';
  });

  protected readonly subtitle = computed(() => {
    const u = this.update();
    const material = u.tierMaterial?.toLowerCase();
    if (u.isTierUp && u.current === FIRST_RING_DAYS) return `Primer título desbloqueado y marco de ${material}`;
    if (u.isTierUp && u.titleUnlocked) return `Nuevo título desbloqueado · marco de ${material}`;
    if (u.isTierUp) return `Tu marco vuelve a ser de ${material}`;
    if (u.current === 1) return 'Vota mañana para no perderla';
    return u.current === u.best && u.current > 1 ? '¡Nuevo récord!' : 'Un día más';
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
    if (u.daysToNextTier == null) return 'Has llegado a lo más alto';
    const days = u.daysToNextTier === 1 ? '1 día' : `${u.daysToNextTier} días`;
    return u.current < FIRST_RING_DAYS ? `${days} para tu primer título` : `${days} para ${u.nextTierTitle}`;
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
