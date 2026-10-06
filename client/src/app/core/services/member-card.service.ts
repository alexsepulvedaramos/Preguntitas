import { Injectable, signal } from '@angular/core';

export interface MemberCardTarget {
  // A single group's card (tapping an avatar inside a group)…
  groupId?: number;
  userId: number;
  // …or the current user's own card with stats for every group (profile page).
  allGroups?: boolean;
}

// Opens the member detail card (rama 19). The card itself is rendered once by
// MemberCardComponent in the main layout.
@Injectable({ providedIn: 'root' })
export class MemberCardService {
  readonly target = signal<MemberCardTarget | null>(null);

  open(groupId: number, userId: number): void {
    this.target.set({ groupId, userId });
  }

  openOwn(userId: number): void {
    this.target.set({ userId, allGroups: true });
  }

  close(): void {
    this.target.set(null);
  }
}
