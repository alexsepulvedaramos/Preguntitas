import { Injectable, inject, signal } from '@angular/core';

import { GroupsService } from './groups.service';
import { GroupMember } from '../models/group.models';

// Streaks of the members of the group currently on screen (rama 19). Avatars inside a
// group read from here, so results/voter avatars can show each member's ring without
// every DTO carrying the streak.
@Injectable({ providedIn: 'root' })
export class GroupStreaksService {
  private readonly groupsService = inject(GroupsService);

  private readonly groupId = signal<number | null>(null);
  private readonly streaks = signal<ReadonlyMap<number, number>>(new Map());
  private readonly crownUserId = signal<number | null>(null);

  // Called by screens that already loaded the member list.
  set(groupId: number, members: GroupMember[]): void {
    this.groupId.set(groupId);
    this.streaks.set(new Map(members.map((m) => [m.id, m.currentStreak])));
    this.crownUserId.set(members.find((m) => m.hasCrown)?.id ?? null);
  }

  // Loads the members when this group isn't the one already cached.
  ensure(groupId: number): void {
    if (this.groupId() === groupId) return;
    this.groupId.set(groupId);
    this.streaks.set(new Map());
    this.crownUserId.set(null);
    this.groupsService.getGroupMembers(groupId).subscribe({
      next: (members) => {
        if (this.groupId() === groupId) this.set(groupId, members);
      },
    });
  }

  currentGroupId(): number | null {
    return this.groupId();
  }

  streakFor(userId: number | null | undefined): number | null {
    if (userId == null) return null;
    return this.streaks().get(userId) ?? null;
  }

  hasCrown(userId: number | null | undefined): boolean {
    return userId != null && this.crownUserId() === userId;
  }
}
