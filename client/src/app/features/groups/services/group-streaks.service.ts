import { Injectable, inject, signal } from '@angular/core';

import { GroupsService } from './groups.service';
import { GroupMember } from '../models/group.models';
import { Look } from '../../../core/constants/shop-catalog';

// Streaks, shop looks and join dates of the members of the group currently on screen
// (rama 19, rama 42). Avatars and names inside a group read from here, so results, voters
// and chat can show each member's ring, cosmetics and seniority without every DTO carrying
// them.
export interface MemberFace {
  avatarUrl: string | null;
  frameColor: string | null;
}

@Injectable({ providedIn: 'root' })
export class GroupStreaksService {
  private readonly groupsService = inject(GroupsService);

  private readonly groupId = signal<number | null>(null);
  private readonly streaks = signal<ReadonlyMap<number, number>>(new Map());
  private readonly crownUserId = signal<number | null>(null);
  private readonly looks = signal<ReadonlyMap<number, Look | null>>(new Map());
  private readonly joinedAts = signal<ReadonlyMap<number, string>>(new Map());
  private readonly faces = signal<ReadonlyMap<number, MemberFace>>(new Map());

  // Called by screens that already loaded the member list.
  set(groupId: number, members: GroupMember[]): void {
    this.groupId.set(groupId);
    this.streaks.set(new Map(members.map((m) => [m.id, m.currentStreak])));
    this.crownUserId.set(members.find((m) => m.hasCrown)?.id ?? null);
    this.looks.set(new Map(members.map((m) => [m.id, m.look ?? null])));
    this.joinedAts.set(new Map(members.map((m) => [m.id, m.joinedAt])));
    this.faces.set(new Map(members.map((m) => [m.id, { avatarUrl: m.avatarUrl, frameColor: m.frameColor }])));
  }

  // Loads the members when this group isn't the one already cached.
  ensure(groupId: number): void {
    if (this.groupId() === groupId) return;
    this.groupId.set(groupId);
    this.streaks.set(new Map());
    this.crownUserId.set(null);
    this.looks.set(new Map());
    this.joinedAts.set(new Map());
    this.faces.set(new Map());
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

  lookFor(userId: number | null | undefined): Look | null {
    if (userId == null) return null;
    return this.looks().get(userId) ?? null;
  }

  joinedAtFor(userId: number | null | undefined): string | null {
    if (userId == null) return null;
    return this.joinedAts().get(userId) ?? null;
  }

  // Photo and frame setting, for avatars on DTOs that only carry a user id (chat).
  faceFor(userId: number | null | undefined): MemberFace | null {
    if (userId == null) return null;
    return this.faces().get(userId) ?? null;
  }

  hasCrown(userId: number | null | undefined): boolean {
    return userId != null && this.crownUserId() === userId;
  }
}
