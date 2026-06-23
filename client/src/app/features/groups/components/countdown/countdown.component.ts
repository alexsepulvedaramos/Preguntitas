import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';

const URGENT_THRESHOLD_MS = 5 * 60 * 1000;

// Live countdown to a target ISO instant (e.g. today.closesAt / selection.activatesAt — the
// next T, spec §4.7). Presentation-only: ticks every second, recomputes the remaining label.
@Component({
  selector: 'app-countdown',
  imports: [],
  templateUrl: './countdown.component.html',
})
export class CountdownComponent {
  public readonly target = input<string | null>(null);

  private readonly now = signal(Date.now());

  private readonly diffMs = computed(() => {
    const target = this.target();
    return target ? new Date(target).getTime() - this.now() : null;
  });

  protected readonly urgent = computed(() => {
    const diffMs = this.diffMs();
    return diffMs !== null && diffMs <= URGENT_THRESHOLD_MS;
  });

  protected readonly label = computed(() => {
    const diffMs = this.diffMs();
    if (diffMs === null) return null;
    if (diffMs <= 0) return 'En cualquier momento';

    const totalSeconds = Math.floor(diffMs / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    if (days > 0) return `${days}d ${hours}h ${minutes}m ${seconds}s`;
    if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
    if (minutes > 0) return `${minutes}m ${seconds}s`;
    return `${seconds}s`;
  });

  constructor() {
    const intervalId = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(intervalId));
  }
}
