import { Component, computed, effect, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Observable, map } from 'rxjs';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

import { AuthService } from '../../../../core/auth/auth.service';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { UserService } from '../../../../core/services/user.service';
import { MemberStats } from '../../../../core/models/streak.model';
import { FIRST_RING_DAYS, streakTierFor } from '../../../../core/constants/streak-tiers';
import { GroupsService } from '../../services/groups.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';

// Member detail card (rama 19): opened by tapping a member avatar inside a group, or from
// the profile page for your own stats in every group. Spartan drawer on mobile, dialog on
// desktop. Rendered once in the main layout and driven by MemberCardService.
@Component({
  selector: 'app-member-card',
  imports: [NgTemplateOutlet, HlmDialogImports, HlmDrawerImports, HlmSkeletonImports, UserAvatarComponent],
  templateUrl: './member-card.component.html',
})
export class MemberCardComponent {
  private readonly cardService = inject(MemberCardService);
  private readonly groupsService = inject(GroupsService);
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);

  protected readonly isDesktop = signal(
    typeof window !== 'undefined' && window.matchMedia('(min-width: 640px)').matches
  );
  protected readonly target = this.cardService.target;
  protected readonly state = computed(() => (this.target() ? 'open' : 'closed'));

  protected readonly stats = signal<MemberStats[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);

  protected readonly isOwnCard = computed(
    () => this.target()?.userId === this.authService.currentUser()?.id
  );
  protected readonly allGroups = computed(() => !!this.target()?.allGroups);

  // Header data: the single group's stats, or — on the own card — the group with the
  // highest current streak (matches the ring shown outside a group).
  protected readonly headline = computed<MemberStats | null>(() => {
    const list = this.stats();
    if (list.length === 0) return null;
    return list.reduce((best, s) => (s.currentStreak > best.currentStreak ? s : best), list[0]);
  });

  protected readonly firstRingDays = FIRST_RING_DAYS;

  constructor() {
    if (typeof window !== 'undefined') {
      const mq = window.matchMedia('(min-width: 640px)');
      mq.addEventListener('change', (e) => this.isDesktop.set(e.matches));
    }

    effect(() => {
      const target = this.target();
      if (!target) return;
      this.load(target.allGroups ? undefined : target.groupId, target.userId);
    });
  }

  close(): void {
    this.cardService.close();
  }

  tierName(streak: number): string {
    return streakTierFor(streak)?.name ?? 'Sin anillo todavía';
  }

  dots(streak: number): boolean[] {
    return Array.from({ length: FIRST_RING_DAYS }, (_, i) => i < streak);
  }

  private load(groupId: number | undefined, userId: number): void {
    this.loading.set(true);
    this.error.set(false);
    this.stats.set([]);

    const request: Observable<MemberStats[]> =
      groupId == null
        ? this.userService.getMyGroupStats()
        : this.groupsService.getMemberStats(groupId, userId).pipe(map((s) => [s]));

    request.subscribe({
      next: (stats) => {
        this.stats.set(stats);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
