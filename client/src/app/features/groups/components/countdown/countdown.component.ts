import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';

import { I18nService } from '../../../../core/i18n/i18n.service';

const URGENT_THRESHOLD_MS = 5 * 60 * 1000;

// Live countdown to a target ISO instant (e.g. today.closesAt / selection.activatesAt — the
// next T, spec §4.7). Presentation-only: ticks every second, recomputes the remaining label.
// Emits `reachedZero` once per target when the countdown hits 0, so the parent can refetch
// without waiting for a manual reload.
@Component({
  selector: 'app-countdown',
  imports: [],
  templateUrl: './countdown.component.html',
})
export class CountdownComponent {
  public readonly target = input<string | null>(null);
  public readonly compact = input(false);
  public readonly reachedZero = output<void>();
  protected readonly t = inject(I18nService).t;

  private readonly now = signal(Date.now());
  private lastTargetEmitted: string | null = null;

  private readonly diffMs = computed(() => {
    const target = this.target();
    return target ? new Date(target).getTime() - this.now() : null;
  });

  protected readonly urgent = computed(() => {
    const diffMs = this.diffMs();
    return diffMs !== null && diffMs <= URGENT_THRESHOLD_MS;
  });

  // Reached zero: the next question is due any moment (the parent refetches).
  protected readonly due = computed(() => {
    const diffMs = this.diffMs();
    return diffMs !== null && diffMs <= 0;
  });

  protected readonly label = computed(() => {
    const diffMs = this.diffMs();
    if (diffMs === null || diffMs <= 0) return null;

    const totalSeconds = Math.floor(diffMs / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    const d = `${days}${this.t('countdown.days')}`;
    const h = `${hours}${this.t('countdown.hours')}`;
    const m = `${minutes}${this.t('countdown.minutes')}`;
    const s = `${seconds}${this.t('countdown.seconds')}`;
    if (days > 0) return `${d} ${h} ${m} ${s}`;
    if (hours > 0) return `${h} ${m} ${s}`;
    if (minutes > 0) return `${m} ${s}`;
    return s;
  });

  constructor() {
    const intervalId = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(intervalId));

    effect(() => {
      const target = this.target();
      const diffMs = this.diffMs();
      if (target && diffMs !== null && diffMs <= 0 && this.lastTargetEmitted !== target) {
        this.lastTargetEmitted = target;
        this.reachedZero.emit();
      }
    });
  }
}
