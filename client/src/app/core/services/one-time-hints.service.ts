import { Injectable, signal } from '@angular/core';

// Hints shown once to point at a new feature (§17.15: the flash button and "change my vote").
// Tracked per device in localStorage — no backend, like the release notes.
export type OneTimeHint = 'flashButton' | 'changeVote';

const STORAGE_KEY = 'vp_hints_seen';

function readSeen(): OneTimeHint[] {
  try {
    const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

@Injectable({ providedIn: 'root' })
export class OneTimeHintsService {
  private readonly seen = signal(new Set(readSeen()));

  isPending(hint: OneTimeHint): boolean {
    return !this.seen().has(hint);
  }

  dismiss(hint: OneTimeHint): void {
    if (this.seen().has(hint)) return;
    const next = new Set(this.seen()).add(hint);
    this.seen.set(next);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify([...next]));
    } catch {
      // Storage unavailable (private mode, blocked site data): the hint is hidden for this visit.
    }
  }
}
