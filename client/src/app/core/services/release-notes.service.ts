import { Injectable, computed, signal } from '@angular/core';

import { LATEST_RELEASE } from '../releases/releases';

// Last release whose notes this device has opened.
const SEEN_KEY = 'vp_release_seen';
// Last release announced with a toast, so the toast shows once per release.
const ANNOUNCED_KEY = 'vp_release_announced';

function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Storage unavailable (private mode, blocked site data): the badge just stays on.
  }
}

// "What's new" state (rama 26). Tracked per device in localStorage — no backend.
@Injectable({ providedIn: 'root' })
export class ReleaseNotesService {
  readonly latest = LATEST_RELEASE;

  private readonly seen = signal(read(SEEN_KEY));

  // The version this device had seen before the current visit (null = never opened the notes).
  readonly seenVersion = this.seen.asReadonly();
  readonly hasUnseen = computed(() => this.seen() !== this.latest.version);

  markSeen(): void {
    write(SEEN_KEY, this.latest.version);
    this.seen.set(this.latest.version);
  }

  // True once per release, and only for devices that had opened an earlier release's notes:
  // brand-new users (and everyone before the notes existed) get the badge, not a toast.
  takeAnnouncement(): boolean {
    if (!this.seen() || !this.hasUnseen()) return false;
    if (read(ANNOUNCED_KEY) === this.latest.version) return false;
    write(ANNOUNCED_KEY, this.latest.version);
    return true;
  }
}
