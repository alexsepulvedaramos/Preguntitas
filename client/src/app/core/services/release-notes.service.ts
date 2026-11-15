import { Injectable, computed, inject } from '@angular/core';

import { LATEST_GAME_RELEASE, LATEST_RELEASE, compareVersions } from '../releases/releases';
import { SeenService } from './seen.service';

// Last release whose notes the player has opened.
const SEEN_KEY = 'release';
// Last release announced with a toast, so the toast shows once per release.
const ANNOUNCED_KEY = 'releaseAnnounced';

// "What's new" state (rama 26). Kept per account since rama 56 (SeenService).
@Injectable({ providedIn: 'root' })
export class ReleaseNotesService {
  private readonly seen = inject(SeenService);

  readonly latest = LATEST_RELEASE;
  // What the badge, the header dot and the toast announce: polish-only releases stay quiet.
  readonly announced = LATEST_GAME_RELEASE;

  // The version the player had seen before the current visit (null = never opened the notes).
  readonly seenVersion = computed(() => this.seen.get(SEEN_KEY));
  readonly hasUnseen = computed(() => {
    const seen = this.seenVersion();
    return this.seen.ready() && (!seen || compareVersions(this.announced.version, seen) > 0);
  });

  markSeen(): void {
    this.seen.set(SEEN_KEY, this.latest.version);
  }

  // True once per release, and only for players who had opened an earlier release's notes:
  // brand-new users (and everyone before the notes existed) get the badge, not a toast.
  async takeAnnouncement(): Promise<boolean> {
    await this.seen.whenReady();
    if (!this.seenVersion() || !this.hasUnseen()) return false;
    if (this.seen.get(ANNOUNCED_KEY) === this.announced.version) return false;
    this.seen.set(ANNOUNCED_KEY, this.announced.version);
    return true;
  }
}
