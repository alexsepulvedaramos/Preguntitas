import { Component, computed, effect, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Observable, map } from 'rxjs';

import { toast } from '@spartan-ng/brain/sonner';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AuthService } from '../../../../core/auth/auth.service';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { UserService } from '../../../../core/services/user.service';
import { ShopService } from '../../../../core/services/shop.service';
import { STREAK_FREEZE_PRICE } from '../../../../core/models/coin.model';
import { apiErrorMessage } from '../../../../shared/utils/api-error';
import { MemberStats } from '../../../../core/models/streak.model';
import { FIRST_RING_DAYS, STREAK_FRAME_KEYS, streakTierFor } from '../../../../core/constants/streak-tiers';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { GroupsService } from '../../services/groups.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';

// Member detail card (rama 19): opened by tapping a member avatar inside a group, or from
// the profile page for your own stats in every group. Spartan drawer on mobile, dialog on
// desktop. Rendered once in the main layout and driven by MemberCardService.
@Component({
  selector: 'app-member-card',
  imports: [
    NgTemplateOutlet,
    HlmButtonImports,
    HlmDialogImports,
    HlmDrawerImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    UserAvatarComponent,
  ],
  templateUrl: './member-card.component.html',
})
export class MemberCardComponent {
  private readonly cardService = inject(MemberCardService);
  private readonly groupsService = inject(GroupsService);
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly shopService = inject(ShopService);
  protected readonly t = inject(I18nService).t;

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

  // Gift a streak freeze (rama 35): on someone else's card inside a group.
  protected readonly freezePrice = STREAK_FREEZE_PRICE;
  protected readonly canGift = computed(() => !!this.target() && !this.allGroups() && !this.isOwnCard());
  protected readonly gifting = signal(false);

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

  giftFreeze(): void {
    const target = this.target();
    const name = this.headline()?.username ?? '';
    if (!target?.groupId || this.gifting()) return;
    this.gifting.set(true);
    this.shopService
      .purchase({ item: 'streakFreeze', groupId: target.groupId, targetUserId: target.userId })
      .subscribe({
        next: () => {
          this.gifting.set(false);
          toast.success(this.t('streakFreeze.gifted', { name }));
        },
        error: (err) => {
          this.gifting.set(false);
          toast.error(apiErrorMessage(err, this.t('streakFreeze.giftFailed')));
        },
      });
  }

  frameLabel(streak: number): string {
    const tier = streakTierFor(streak);
    if (!tier) return this.t('memberCard.noFrame');
    const frame = this.t(STREAK_FRAME_KEYS[tier.key]);
    return frame.charAt(0).toUpperCase() + frame.slice(1);
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
