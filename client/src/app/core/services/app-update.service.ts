import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

import { APP_VERSION, isUpdateWorthPrompt } from '../releases/releases';

const HOURLY = 60 * 60 * 1000;

// New-version detection (release 1.6.1). The service worker downloads a new build in the
// background but keeps serving the cached one until every tab is closed — and an installed
// PWA on a phone is resumed, not reopened, so players were stuck on old versions. This checks
// for updates whenever the app returns to the foreground; the prompt UI reads `ready`.
// Kept free of UI imports: it is created at startup, the prompt is lazy-loaded.
@Injectable({ providedIn: 'root' })
export class AppUpdateService {
  private readonly swUpdate = inject(SwUpdate);

  private readonly readySignal = signal(false);
  readonly ready = this.readySignal.asReadonly();

  constructor() {
    if (!this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'), takeUntilDestroyed())
      .subscribe(event => {
        // Polish-only updates install quietly with the next load; only game or critical
        // ones interrupt (spec §14 "Game vs polish").
        if (isUpdateWorthPrompt(event.latestVersion.appData, APP_VERSION)) this.readySignal.set(true);
      });

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

  // "Luego": hide until the next app load, which picks the new version up anyway.
  dismiss(): void {
    this.readySignal.set(false);
  }

  async update(): Promise<void> {
    try {
      await this.swUpdate.activateUpdate();
    } finally {
      document.location.reload();
    }
  }
}
