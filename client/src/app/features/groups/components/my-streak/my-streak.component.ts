import { Component, computed, inject, input, output, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { toast } from '@spartan-ng/brain/sonner';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AuthService } from '../../../../core/auth/auth.service';
import { ShopService } from '../../../../core/services/shop.service';
import { MyStreak } from '../../../../core/models/streak.model';
import { ShopItem } from '../../../../core/models/coin.model';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

// "Your streak in this group" (rama 35, §17.9): a small 🔥 chip in the group's top bar
// (rama 36) that turns orange when the streak is at risk today. Tapping it opens the details —
// best streak, the freezes held and the button to buy more, and the repair when a lost streak
// can still be bought back. Drawer on mobile, dialog on desktop.
@Component({
  selector: 'app-my-streak',
  imports: [NgTemplateOutlet, HlmButtonImports, HlmDialogImports, HlmDrawerImports, HlmSpinnerImports],
  templateUrl: './my-streak.component.html',
})
export class MyStreakComponent {
  private readonly shopService = inject(ShopService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  readonly groupId = input.required<number>();
  readonly streak = input.required<MyStreak>();
  // Not voted yet and the question closes soon: the streak is at stake.
  readonly danger = input(false);
  // The streak after a purchase, for the parent to merge into daily/current.
  readonly changed = output<MyStreak>();

  protected readonly busy = signal<ShopItem | null>(null);
  protected readonly open = signal(false);
  protected readonly state = computed(() => (this.open() ? 'open' : 'closed'));
  protected readonly isDesktop = signal(
    typeof window !== 'undefined' && window.matchMedia('(min-width: 640px)').matches,
  );

  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly slots = computed(() =>
    Array.from({ length: this.streak().maxFreezes }, (_, i) => i < this.streak().freezes),
  );
  protected readonly atLimit = computed(() => this.streak().freezes >= this.streak().maxFreezes);
  protected readonly missingCoins = computed(() => Math.max(0, this.streak().freezePrice - this.balance()));

  buyFreeze(): void {
    this.purchase('streakFreeze', () => toast.success(this.t('streakFreeze.bought')), 'streakFreeze.buyFailed');
  }

  // Also reached from the "lost streak" notice in the group view.
  repair(): void {
    const lost = this.streak().repairableStreak;
    this.purchase(
      'streakRepair',
      (fresh) => toast.success(this.t('streakFreeze.repaired', { count: fresh.current || lost || 0 })),
      'streakFreeze.repairFailed',
    );
  }

  private purchase(item: ShopItem, onSuccess: (fresh: MyStreak) => void, failKey: 'streakFreeze.buyFailed' | 'streakFreeze.repairFailed'): void {
    if (this.busy()) return;
    this.busy.set(item);
    this.shopService.purchase({ item, groupId: this.groupId() }).subscribe({
      next: (res) => {
        this.busy.set(null);
        this.changed.emit(res.myStreak);
        onSuccess(res.myStreak);
      },
      error: (err) => {
        this.busy.set(null);
        toast.error(apiErrorMessage(err, this.t(failKey)));
      },
    });
  }
}
