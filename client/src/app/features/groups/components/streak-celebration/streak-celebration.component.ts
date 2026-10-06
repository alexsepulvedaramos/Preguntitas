import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';

import { AuthService } from '../../../../core/auth/auth.service';
import { StreakUpdate } from '../../../../core/models/streak.model';
import { FIRST_RING_DAYS, FRAME_STREAK, STREAK_TIERS } from '../../../../core/constants/streak-tiers';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';

const COUNT_UP_MS = 700;

// Duolingo-style full-screen celebration after a vote that extends the streak (rama 19).
// Days 1–2 get progress dots towards the first ring; from day 3 a progress bar towards
// the next tier. Milestones and tier-ups also fire confetti. Tap anywhere to dismiss.
@Component({
  selector: 'app-streak-celebration',
  imports: [UserAvatarComponent],
  templateUrl: './streak-celebration.component.html',
  host: {
    '(document:keydown.escape)': 'dismiss()',
  },
})
export class StreakCelebrationComponent implements OnInit, OnDestroy {
  protected readonly authService = inject(AuthService);

  public readonly update = input.required<StreakUpdate>();
  public readonly dismissed = output<void>();

  protected readonly displayedCount = signal(0);
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly firstRingDays = FIRST_RING_DAYS;

  private readonly reducedMotion =
    typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  private countUpTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly isCelebratory = computed(
    () => this.update().isTierUp || !!this.update().milestoneLabel
  );

  protected readonly title = computed(() => {
    const u = this.update();
    if (u.milestoneLabel) return u.milestoneLabel;
    if (u.isTierUp && u.tierName) return `¡${u.tierName}!`;
    if (u.current === 1) return '¡Racha empezada!';
    return '¡Sigue así!';
  });

  protected readonly subtitle = computed(() => {
    const u = this.update();
    if (u.isTierUp && u.current === FIRST_RING_DAYS) return 'Has desbloqueado tu primer anillo';
    if (u.isTierUp) return 'Tu anillo sube de nivel';
    if (u.current === 1) return 'Vota mañana para no perderla';
    return u.current === u.best && u.current > 1 ? '¡Nuevo récord!' : 'Un día más';
  });

  // Days 1–2: ●○○ → ●●○ towards the first ring.
  protected readonly dots = computed(() =>
    Array.from({ length: FIRST_RING_DAYS }, (_, i) => i < this.update().current)
  );

  protected readonly progressPercent = computed(() => {
    const u = this.update();
    if (u.nextTierAt == null) return 100;
    const tierStart = [...STREAK_TIERS].reverse().find((t) => u.current >= t.minDays)?.minDays ?? 0;
    return Math.round(((u.current - tierStart) / (u.nextTierAt - tierStart)) * 100);
  });

  protected readonly nextTierLabel = computed(() => {
    const u = this.update();
    if (u.daysToNextTier == null) return 'Has alcanzado la llama máxima';
    const days = u.daysToNextTier === 1 ? '1 día' : `${u.daysToNextTier} días`;
    return u.current < FIRST_RING_DAYS
      ? `${days} para tu primer anillo`
      : `${days} para ${u.nextTierName}`;
  });

  ngOnInit(): void {
    const { previous, current } = this.update();
    if (this.reducedMotion || current - previous <= 0) {
      this.displayedCount.set(current);
    } else {
      this.displayedCount.set(previous);
      this.countUpTimer = setTimeout(() => this.displayedCount.set(current), COUNT_UP_MS);
    }

    if (this.isCelebratory() && !this.reducedMotion) this.launchConfetti();
  }

  ngOnDestroy(): void {
    if (this.countUpTimer) clearTimeout(this.countUpTimer);
  }

  dismiss(): void {
    this.dismissed.emit();
  }

  private launchConfetti(): void {
    const tier = this.update().tierKey ?? 'ember';
    const styles = getComputedStyle(document.documentElement);
    const colors = [
      styles.getPropertyValue(`--streak-${tier}-a`).trim(),
      styles.getPropertyValue(`--streak-${tier}-b`).trim(),
      styles.getPropertyValue('--accent').trim(),
      styles.getPropertyValue('--primary').trim(),
    ].filter(Boolean);
    import('../../../../shared/utils/confetti').then((m) => m.fireConfetti(colors));
  }
}
