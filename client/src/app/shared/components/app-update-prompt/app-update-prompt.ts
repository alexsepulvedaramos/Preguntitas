import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';
import { filter } from 'rxjs';

const HOURLY = 60 * 60 * 1000;

// "New version" prompt (release 1.6.1). The service worker downloads a new build in the
// background but keeps serving the cached one until every tab is closed — and an installed
// PWA on a phone is resumed, not reopened, so players were stuck on old versions. This checks
// for updates whenever the app returns to the foreground and offers a one-tap reload.
// Plain buttons like the sibling root prompts: Helm here would add ~35 kB to the initial bundle.
@Component({
  selector: 'app-update-prompt',
  template: `
    @if (visible()) {
      <div class="fixed top-3 left-4 right-4 z-[60] sm:left-auto sm:right-4 sm:w-80" role="status" aria-live="polite">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl flex items-center gap-3">
          <span class="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-lg" aria-hidden="true">✨</span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-foreground leading-tight">Hay una versión nueva</p>
            <p class="text-xs text-muted-foreground mt-0.5">Actualiza para ver las novedades.</p>
          </div>
          <div class="flex flex-col gap-1.5 shrink-0">
            <button
              (click)="update()"
              [disabled]="updating()"
              class="text-xs font-semibold bg-primary text-primary-foreground px-3 py-1.5 rounded-lg hover:opacity-90 transition-opacity disabled:opacity-60"
            >
              Actualizar
            </button>
            <button
              (click)="visible.set(false)"
              class="text-xs text-muted-foreground px-3 py-1.5 rounded-lg hover:bg-muted transition-colors text-center"
            >
              Luego
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AppUpdatePromptComponent {
  private readonly swUpdate = inject(SwUpdate);

  protected readonly visible = signal(false);
  protected readonly updating = signal(false);

  constructor() {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter(event => event.type === 'VERSION_READY'), takeUntilDestroyed())
      .subscribe(() => this.visible.set(true));

    // A broken cache can't be repaired in place: reload onto the server's version.
    this.swUpdate.unrecoverable
      .pipe(takeUntilDestroyed())
      .subscribe(() => document.location.reload());

    const check = () => {
      if (document.visibilityState === 'visible') this.swUpdate.checkForUpdate().catch(() => {});
    };
    document.addEventListener('visibilitychange', check);
    const timer = setInterval(check, HOURLY);
    inject(DestroyRef).onDestroy(() => {
      document.removeEventListener('visibilitychange', check);
      clearInterval(timer);
    });
  }

  protected async update(): Promise<void> {
    this.updating.set(true);
    try {
      await this.swUpdate.activateUpdate();
    } finally {
      document.location.reload();
    }
  }
}
