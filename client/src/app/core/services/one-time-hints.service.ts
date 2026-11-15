import { Injectable, inject } from '@angular/core';

import { SeenService } from './seen.service';

// Hints shown once to point at a new feature (§17.15: the flash button and "change my vote";
// rama 56: press and hold to react). Kept per account since rama 56 (SeenService).
export type OneTimeHint = 'flashButton' | 'changeVote' | 'reactionHold';

@Injectable({ providedIn: 'root' })
export class OneTimeHintsService {
  private readonly seen = inject(SeenService);

  // Reads signals: call it from a computed so the hint goes away wherever it is dismissed.
  isPending(hint: OneTimeHint): boolean {
    return this.seen.ready() && !this.seen.get(`hint:${hint}`);
  }

  dismiss(hint: OneTimeHint): void {
    this.seen.set(`hint:${hint}`, '1');
  }
}
