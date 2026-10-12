import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { toast } from '@spartan-ng/brain/sonner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideStore } from '@ng-icons/lucide';

import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { ShopService } from '../../core/services/shop.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import {
  AUTHOR_HINT_PRICE,
  COIN_DAILY_CAP,
  CoinReason,
  CoinTransaction,
  EMPTY_INVENTORY,
  FLASH_QUESTION_PRICE,
  Inventory,
  InventoryItem,
  STREAK_FREEZE_PRICE,
  STREAK_REPAIR_PRICE,
} from '../../core/models/coin.model';

const REASON_LABELS: Record<CoinReason, TranslationKey> = {
  [CoinReason.Vote]: 'coins.reasons.vote',
  [CoinReason.FirstVoter]: 'coins.reasons.firstVoter',
  [CoinReason.StreakTier]: 'coins.reasons.streakTier',
  [CoinReason.StreakMilestone]: 'coins.reasons.streakMilestone',
  [CoinReason.SelectorChoice]: 'coins.reasons.selectorChoice',
  [CoinReason.SelectorCreated]: 'coins.reasons.selectorCreated',
  [CoinReason.StreakFreeze]: 'coins.reasons.streakFreeze',
  [CoinReason.StreakFreezeGift]: 'coins.reasons.streakFreezeGift',
  [CoinReason.StreakRepair]: 'coins.reasons.streakRepair',
  [CoinReason.GuessCorrect]: 'coins.reasons.guessCorrect',
  [CoinReason.AuthorHint]: 'coins.reasons.authorHint',
  [CoinReason.RaceWon]: 'coins.reasons.raceWon',
  [CoinReason.FlashQuestion]: 'coins.reasons.flashQuestion',
};

const REASON_EMOJI: Record<CoinReason, string> = {
  [CoinReason.Vote]: '🗳️',
  [CoinReason.FirstVoter]: '⚡',
  [CoinReason.StreakTier]: '🔥',
  [CoinReason.StreakMilestone]: '🎉',
  [CoinReason.SelectorChoice]: '👉',
  [CoinReason.SelectorCreated]: '✍️',
  [CoinReason.StreakFreeze]: '🧊',
  [CoinReason.StreakFreezeGift]: '🎁',
  [CoinReason.StreakRepair]: '🔧',
  [CoinReason.GuessCorrect]: '🕵️',
  [CoinReason.AuthorHint]: '💡',
  [CoinReason.RaceWon]: '⏱️',
  [CoinReason.FlashQuestion]: '⚡',
};

// The game items sold ahead (rama 45, §17.10), cheapest first. Each is used later from its own
// spot in a group: the streak panel, a member's card, the guess step or the create form.
const SHOP_ITEMS: readonly {
  item: InventoryItem;
  slot: keyof Inventory;
  emoji: string;
  price: number;
  name: TranslationKey;
  text: TranslationKey;
}[] = [
  {
    item: InventoryItem.AuthorHint,
    slot: 'authorHint',
    emoji: '💡',
    price: AUTHOR_HINT_PRICE,
    name: 'inventory.items.authorHint.name',
    text: 'inventory.items.authorHint.text',
  },
  {
    item: InventoryItem.StreakFreeze,
    slot: 'streakFreeze',
    emoji: '🧊',
    price: STREAK_FREEZE_PRICE,
    name: 'inventory.items.streakFreeze.name',
    text: 'inventory.items.streakFreeze.text',
  },
  {
    item: InventoryItem.StreakRepair,
    slot: 'streakRepair',
    emoji: '🔧',
    price: STREAK_REPAIR_PRICE,
    name: 'inventory.items.streakRepair.name',
    text: 'inventory.items.streakRepair.text',
  },
  {
    item: InventoryItem.FlashQuestion,
    slot: 'flashQuestion',
    emoji: '⚡',
    price: FLASH_QUESTION_PRICE,
    name: 'inventory.items.flashQuestion.name',
    text: 'inventory.items.flashQuestion.text',
  },
];

// Coins & shop (rama 34, §17.2): the wallet, the game items you can buy ahead (rama 45) and
// the ledger. Cosmetics arrive with rama 42 (§17.10).
@Component({
  selector: 'app-shop-page',
  imports: [NgIcon, HlmAlertDialogImports, HlmButtonImports, HlmCardImports, HlmEmptyImports, HlmItemImports, HlmSpinnerImports],
  providers: [provideIcons({ lucideArrowLeft, lucideStore })],
  templateUrl: './shop-page.component.html',
})
export class ShopPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly shopService = inject(ShopService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly dailyCap = COIN_DAILY_CAP;

  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly items = signal<CoinTransaction[]>([]);
  protected readonly nextBefore = signal<number | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly busy = signal<InventoryItem | null>(null);

  protected readonly shopItems = computed(() => {
    const inventory = this.authService.currentUser()?.inventory ?? EMPTY_INVENTORY;
    const balance = this.balance();
    return SHOP_ITEMS.map(entry => ({
      ...entry,
      name: this.t(entry.name),
      text: this.t(entry.text),
      owned: inventory[entry.slot],
      missing: Math.max(0, entry.price - balance),
    }));
  });

  // Re-derived when the language changes.
  protected readonly rows = computed(() => {
    const format = new Intl.DateTimeFormat(this.i18n.locale(), { day: 'numeric', month: 'short' });
    return this.items().map(item => ({
      ...item,
      emoji: REASON_EMOJI[item.reason] ?? '🪙',
      label: this.t(REASON_LABELS[item.reason] ?? 'coins.reasons.vote'),
      date: format.format(new Date(item.createdAt)),
    }));
  });

  ngOnInit(): void {
    // Selector rewards are paid by the daily tick, so refresh the balance on entry.
    this.userService.getProfile().subscribe({
      next: profile => this.authService.patchCurrentUser({ coins: profile.coins, inventory: profile.inventory }),
    });
    this.loadPage(null);
  }

  buy(item: InventoryItem): void {
    if (this.busy() !== null) return;
    const slot = SHOP_ITEMS.find(entry => entry.item === item)!.slot;
    this.busy.set(item);
    this.shopService.buyItem(item).subscribe({
      next: res => {
        this.busy.set(null);
        toast.success(this.t('inventory.bought', { count: res.inventory[slot] }));
        // The purchase is a new ledger row: reload from the first page.
        this.items.set([]);
        this.loadPage(null);
      },
      error: err => {
        this.busy.set(null);
        toast.error(apiErrorMessage(err, this.t('inventory.buyFailed')));
      },
    });
  }

  loadMore(): void {
    this.loadPage(this.nextBefore());
  }

  goBack(): void {
    this.router.navigate(['/groups']);
  }

  private loadPage(before: number | null): void {
    this.loading.set(true);
    this.error.set(false);
    this.userService.getCoinHistory(before).subscribe({
      next: page => {
        this.items.update(items => [...items, ...page.items]);
        this.nextBefore.set(page.nextBefore);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
