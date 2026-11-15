import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal, untracked } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, filter, firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

type Marks = Record<string, string>;

interface SeenCache {
  // Whose marks these are; null while nobody has logged in on this device.
  userId: string | null;
  marks: Marks;
  // Marks the server hasn't confirmed yet: set while logged out or offline, or carried over
  // from the per-device keys older versions used.
  pending: Marks;
}

const CACHE_KEY = 'vp_seen';
// Where versions before 1.34.1 kept this, per device.
const LEGACY_RELEASE = 'vp_release_seen';
const LEGACY_ANNOUNCED = 'vp_release_announced';
const LEGACY_HINTS = 'vp_hints_seen';
const LEGACY_SUMMARY_PREFIX = 'vp_summary_seen_';

function readCache(): SeenCache {
  try {
    const parsed = JSON.parse(localStorage.getItem(CACHE_KEY) ?? 'null') as Partial<SeenCache> | null;
    if (parsed && typeof parsed === 'object')
      return { userId: parsed.userId ?? null, marks: parsed.marks ?? {}, pending: parsed.pending ?? {} };
  } catch {
    // Unreadable or blocked storage: start empty.
  }
  return { userId: null, marks: {}, pending: {} };
}

function writeCache(cache: SeenCache): void {
  try {
    localStorage.setItem(CACHE_KEY, JSON.stringify(cache));
  } catch {
    // Storage unavailable (private mode, blocked site data): the server copy still holds.
  }
}

// What this device remembered on its own before marks were kept per account. Read once,
// handed to the account that logs in next, and removed.
function takeLegacyMarks(): Marks {
  const marks: Marks = {};
  try {
    const release = localStorage.getItem(LEGACY_RELEASE);
    if (release) marks['release'] = release;
    const announced = localStorage.getItem(LEGACY_ANNOUNCED);
    if (announced) marks['releaseAnnounced'] = announced;

    const hints: unknown = JSON.parse(localStorage.getItem(LEGACY_HINTS) ?? '[]');
    if (Array.isArray(hints))
      for (const hint of hints) if (typeof hint === 'string' && /^[A-Za-z]{1,32}$/.test(hint)) marks[`hint:${hint}`] = '1';

    const summaryKeys: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(LEGACY_SUMMARY_PREFIX)) summaryKeys.push(key);
    }
    for (const key of summaryKeys) {
      const groupId = key.slice(LEGACY_SUMMARY_PREFIX.length);
      const month = localStorage.getItem(key);
      if (/^[1-9]\d*$/.test(groupId) && month && /^\d{4}-\d{2}$/.test(month)) marks[`summary:${groupId}`] = month;
    }

    [LEGACY_RELEASE, LEGACY_ANNOUNCED, LEGACY_HINTS, ...summaryKeys].forEach((key) => localStorage.removeItem(key));
  } catch {
    // Nothing to carry over.
  }
  return marks;
}

// What the player has already seen — release notes, one-off hints, monthly summary cards —
// kept per account on the server (rama 56) so it doesn't come back after logging in again or
// on another device. A local copy answers at once and covers being offline; the server's
// answer replaces it. Keys: `release`, `releaseAnnounced`, `hint:<name>`, `summary:<groupId>`.
@Injectable({ providedIn: 'root' })
export class SeenService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly url = `${environment.apiUrl}/users/me/seen`;

  private cache = readCache();
  private readonly marks = signal<Marks>({});
  // False while the account's marks are still on their way: until then nothing is announced
  // as new, so a fresh device doesn't flash badges the player dismissed elsewhere.
  readonly ready = signal(false);
  private readonly ready$ = toObservable(this.ready);

  constructor() {
    const legacy = takeLegacyMarks();
    if (Object.keys(legacy).length) {
      this.cache = { ...this.cache, pending: { ...legacy, ...this.cache.pending } };
      writeCache(this.cache);
    }

    effect(() => {
      const userId = this.auth.currentUser()?.id ?? null;
      untracked(() => this.load(userId == null ? null : String(userId)));
    });
  }

  get(key: string): string | null {
    return this.marks()[key] ?? null;
  }

  set(key: string, value: string): void {
    if (this.marks()[key] === value) return;
    this.cache = {
      ...this.cache,
      marks: { ...this.cache.marks, [key]: value },
      pending: { ...this.cache.pending, [key]: value },
    };
    writeCache(this.cache);
    this.marks.set({ ...this.cache.marks, ...this.cache.pending });
    if (this.cache.userId) this.sync(this.cache.userId, this.save(this.cache.pending));
  }

  whenReady(): Promise<boolean> {
    return firstValueFrom(this.ready$.pipe(filter(Boolean)));
  }

  private load(userId: string | null): void {
    if (!userId) {
      // Logged out: this device's own marks, handed to whoever logs in next.
      this.cache = { userId: null, marks: {}, pending: { ...this.cache.marks, ...this.cache.pending } };
      this.marks.set(this.cache.pending);
      this.ready.set(true);
      return;
    }

    // Another account's marks are not this one's.
    if (this.cache.userId && this.cache.userId !== userId) this.cache = { userId, marks: {}, pending: {} };
    this.cache = { ...this.cache, userId };
    this.marks.set({ ...this.cache.marks, ...this.cache.pending });
    this.ready.set(false);

    const hasPending = Object.keys(this.cache.pending).length > 0;
    this.sync(userId, hasPending ? this.save(this.cache.pending) : this.http.get<Marks>(this.url));
  }

  private save(marks: Marks): Observable<Marks> {
    return this.http.put<Marks>(this.url, { marks });
  }

  private sync(userId: string, request: Observable<Marks>): void {
    const sent = { ...this.cache.pending };
    request.subscribe({
      next: (marks) => {
        if (this.cache.userId !== userId) return;
        // Whatever was set while the request was in flight is still waiting.
        const pending = Object.fromEntries(Object.entries(this.cache.pending).filter(([key, value]) => sent[key] !== value));
        this.cache = { userId, marks, pending };
        writeCache(this.cache);
        this.marks.set({ ...marks, ...pending });
        this.ready.set(true);
      },
      // Offline or the server is waking up: the local copy stands, pending marks go next time.
      error: () => {
        if (this.cache.userId === userId) this.ready.set(true);
      },
    });
  }
}
