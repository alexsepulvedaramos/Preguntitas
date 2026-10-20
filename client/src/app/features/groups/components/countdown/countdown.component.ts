import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';

import { HlmProgressImports } from '@spartan-ng/helm/progress';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideClock } from '@ng-icons/lucide';

import { I18nService } from '../../../../core/i18n/i18n.service';

const URGENT_THRESHOLD_MS = 5 * 60 * 1000;
// A question is open for one daily cycle: the `line` bar shows how much of it is left.
const CYCLE_MS = 24 * 60 * 60 * 1000;

// Live countdown to a target ISO instant (e.g. today.closesAt / selection.activatesAt — the
// next T, spec §4.7). Presentation-only: ticks every second, recomputes the remaining label.
// Emits `reachedZero` once per target when the countdown hits 0, so the parent can refetch
// without waiting for a manual reload. The `line` layout (group screen, rama 36) is a thin
// bar led by a clock icon, with "Quedan 5 h 12 min" under it; projected content sits at the
// right of that row.
@Component({
  selector: 'app-countdown',
  imports: [HlmProgressImports, NgIcon],
  providers: [provideIcons({ lucideClock })],
  templateUrl: './countdown.component.html',
})
export class CountdownComponent {
  public readonly target = input<string | null>(null);
  public readonly compact = input(false);
  public readonly line = input(false);
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

  // One time format everywhere (UX audit T12): number, space, unit — "4 h 59 min 5 s".
  protected readonly label = computed(() => {
    const diffMs = this.diffMs();
    if (diffMs === null || diffMs <= 0) return null;

    const totalSeconds = Math.floor(diffMs / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    const d = `${days} ${this.t('countdown.days')}`;
    const h = `${hours} ${this.t('countdown.hours')}`;
    const m = `${minutes} ${this.t('countdown.minutes')}`;
    const s = `${seconds} ${this.t('countdown.seconds')}`;
    if (days > 0) return `${d} ${h} ${m} ${s}`;
    if (hours > 0) return `${h} ${m} ${s}`;
    if (minutes > 0) return `${m} ${s}`;
    return s;
  });

  // Share of the cycle still left, 0–100, for the `line` bar.
  protected readonly remainingPercent = computed(() => {
    const diffMs = this.diffMs();
    if (diffMs === null) return 0;
    return Math.round(Math.min(100, Math.max(0, (diffMs / CYCLE_MS) * 100)));
  });

  // Calmer label for the `line` layout: hours and minutes, seconds only in the last minute.
  protected readonly shortLabel = computed(() => {
    const diffMs = this.diffMs();
    if (diffMs === null || diffMs <= 0) return null;

    const totalSeconds = Math.floor(diffMs / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    if (hours > 0) return `${hours} ${this.t('countdown.hours')} ${minutes} ${this.t('countdown.minutes')}`;
    if (minutes > 0) return `${minutes} ${this.t('countdown.minutes')}`;
    return `${totalSeconds} ${this.t('countdown.seconds')}`;
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
