import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { toast } from '@spartan-ng/brain/sonner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCheck, lucideInfo } from '@ng-icons/lucide';

import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { ShopService } from '../../core/services/shop.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { ThemeService } from '../../core/services/theme.service';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../shared/components/user-name/user-name.component';
import {
  COSMETICS,
  CosmeticEntry,
  CosmeticKind,
  CosmeticRarity,
  Look,
  THEME_SWATCHES,
  cosmeticByKey,
  nameColorClass,
  themeAttr,
} from '../../core/constants/shop-catalog';
import { FIRST_RING_DAYS, FRAME_STREAK, STREAK_TIERS } from '../../core/constants/streak-tiers';
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
  NUDGE_PRICE,
  STREAK_FREEZE_PRICE,
  STREAK_REPAIR_PRICE,
  VOTE_CHANGE_PRICE,
} from '../../core/models/coin.model';
import { GlyphComponent } from '../../shared/components/glyph/glyph.component';
import { GlyphName } from '../../shared/components/glyph/glyphs';
import { CoinComponent } from '../../shared/components/coin/coin.component';
import { CoinTextComponent } from '../../shared/components/coin/coin-text.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';

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
  [CoinReason.Cosmetic]: 'coins.reasons.cosmetic',
  [CoinReason.StreakBonus]: 'coins.reasons.streakBonus',
  [CoinReason.CosmeticRefund]: 'coins.reasons.cosmeticRefund',
  [CoinReason.VoteChange]: 'coins.reasons.voteChange',
  [CoinReason.Nudge]: 'coins.reasons.nudge',
  [CoinReason.FlashAnonymousVotes]: 'coins.reasons.flashAnonymousVotes',
  [CoinReason.FlashAnonymousAuthor]: 'coins.reasons.flashAnonymousAuthor',
  [CoinReason.FlashCapsule]: 'coins.reasons.flashCapsule',
  [CoinReason.FlashExtension]: 'coins.reasons.flashExtension',
};

const REASON_GLYPH: Record<CoinReason, GlyphName> = {
  [CoinReason.Vote]: 'vote',
  [CoinReason.FirstVoter]: 'flash',
  [CoinReason.StreakTier]: 'streak',
  [CoinReason.StreakMilestone]: 'celebrate',
  [CoinReason.SelectorChoice]: 'pick',
  [CoinReason.SelectorCreated]: 'write',
  [CoinReason.StreakFreeze]: 'freeze',
  [CoinReason.StreakFreezeGift]: 'gift',
  [CoinReason.StreakRepair]: 'repair',
  [CoinReason.GuessCorrect]: 'guess',
  [CoinReason.AuthorHint]: 'hint',
  [CoinReason.RaceWon]: 'race',
  [CoinReason.FlashQuestion]: 'flash',
  [CoinReason.Cosmetic]: 'shop',
  [CoinReason.StreakBonus]: 'streak',
  [CoinReason.CosmeticRefund]: 'wallet',
  [CoinReason.VoteChange]: 'changeVote',
  [CoinReason.Nudge]: 'nudge',
  [CoinReason.FlashAnonymousVotes]: 'secretVotes',
  [CoinReason.FlashAnonymousAuthor]: 'anonymous',
  [CoinReason.FlashCapsule]: 'capsule',
  [CoinReason.FlashExtension]: 'extend',
};

type Slot = keyof Look | 'theme';

// Cosmetics sections in the shop (rama 42), each with the profile field that equips it.
const KIND_SECTIONS: readonly {
  kind: CosmeticKind;
  id: string;
  field: 'ringSkinKey' | 'nameColorKey' | 'themeKey';
  slot: Slot;
}[] = [
  { kind: CosmeticKind.RingSkin, id: 'ringSkin', field: 'ringSkinKey', slot: 'ringSkin' },
  { kind: CosmeticKind.NameColor, id: 'nameColor', field: 'nameColorKey', slot: 'nameColor' },
  { kind: CosmeticKind.Theme, id: 'theme', field: 'themeKey', slot: 'theme' },
];

const RARITY_KEYS: Record<CosmeticRarity, TranslationKey> = {
  [CosmeticRarity.Common]: 'shop.rarity.common',
  [CosmeticRarity.Rare]: 'shop.rarity.rare',
  [CosmeticRarity.Epic]: 'shop.rarity.epic',
  [CosmeticRarity.Legendary]: 'shop.rarity.legendary',
};

// Streak shown on the try-on avatar while trying a ring skin without a ring yet (silver).
const PREVIEW_STREAK = STREAK_TIERS[3].minDays;

// The game items sold ahead (rama 45, §17.10), cheapest first. Each is used later from its own
// spot in a group: the streak panel, a member's card, the guess step, the create form, the
// results card (change my vote) or the list of who has voted (nudge).
const SHOP_ITEMS: readonly {
  item: InventoryItem;
  slot: keyof Inventory;
  glyph: GlyphName;
  price: number;
  name: TranslationKey;
  text: TranslationKey;
}[] = [
  {
    item: InventoryItem.Nudge,
    slot: 'nudge',
    glyph: 'nudge',
    price: NUDGE_PRICE,
    name: 'inventory.items.nudge.name',
    text: 'inventory.items.nudge.text',
  },
  {
    item: InventoryItem.AuthorHint,
    slot: 'authorHint',
    glyph: 'hint',
    price: AUTHOR_HINT_PRICE,
    name: 'inventory.items.authorHint.name',
    text: 'inventory.items.authorHint.text',
  },
  {
    item: InventoryItem.VoteChange,
    slot: 'voteChange',
    glyph: 'changeVote',
    price: VOTE_CHANGE_PRICE,
    name: 'inventory.items.voteChange.name',
    text: 'inventory.items.voteChange.text',
  },
  {
    item: InventoryItem.FlashQuestion,
    slot: 'flashQuestion',
    glyph: 'flash',
    price: FLASH_QUESTION_PRICE,
    name: 'inventory.items.flashQuestion.name',
    text: 'inventory.items.flashQuestion.text',
  },
  {
    item: InventoryItem.StreakFreeze,
    slot: 'streakFreeze',
    glyph: 'freeze',
    price: STREAK_FREEZE_PRICE,
    name: 'inventory.items.streakFreeze.name',
    text: 'inventory.items.streakFreeze.text',
  },
  {
    item: InventoryItem.StreakRepair,
    slot: 'streakRepair',
    glyph: 'repair',
    price: STREAK_REPAIR_PRICE,
    name: 'inventory.items.streakRepair.name',
    text: 'inventory.items.streakRepair.text',
  },
];

// Coins & shop (rama 34, §17.2): the balance and an "i" with the explanations in the header
// (rama 46), the game items you can buy ahead (rama 45),
// the cosmetics with a try-on preview on your own avatar (rama 42, §17.10) and the ledger.
@Component({
  selector: 'app-shop-page',
  imports: [PageHeaderComponent, HlmTabsImports, 
    CoinTextComponent,
    CoinComponent,
    GlyphComponent,
    NgIcon,
    UserAvatarComponent,
    UserNameComponent,
    HlmAlertDialogImports,
    HlmBadgeImports,
    HlmButtonImports,
    HlmCardImports,
    HlmItemImports,
    HlmPopoverImports,
    HlmSpinnerImports,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCheck, lucideInfo })],
  templateUrl: './shop-page.component.html',
})
export class ShopPageComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly shopService = inject(ShopService);
  private readonly router = inject(Router);
  private readonly themeService = inject(ThemeService);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly dailyCap = COIN_DAILY_CAP;

  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly items = signal<CoinTransaction[]>([]);
  protected readonly nextBefore = signal<number | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly busy = signal<InventoryItem | null>(null);

  // ── Cosmetics (rama 42) ──
  protected readonly user = this.authService.currentUser;
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly cosmeticBusy = signal<string | null>(null);
  // What is being tried on, per slot (null = trying "nothing", e.g. the default theme).
  protected readonly trying = signal<Partial<Record<Slot, string | null>>>({});
  protected readonly isTrying = computed(() => Object.keys(this.trying()).length > 0);

  private readonly owned = computed(() => new Set(this.user()?.ownedCosmetics ?? []));

  private readonly equipped = computed<Record<Slot, string | null>>(() => {
    const look = this.user()?.look;
    return {
      ringSkin: look?.ringSkin ?? null,
      nameColor: look?.nameColor ?? null,
      theme: this.user()?.themeKey ?? null,
    };
  });

  // What the try-on avatar wears: the equipped items with the tried-on ones over them.
  protected readonly previewLook = computed<Look>(() => {
    const merged = { ...this.equipped(), ...this.trying() };
    return {
      ringSkin: merged.ringSkin ?? null,
      nameColor: merged.nameColor ?? null,
    };
  });

  protected readonly previewStreak = computed(() => {
    const streak = this.user()?.highestStreak ?? 0;
    return streak >= FIRST_RING_DAYS || !this.trying().ringSkin ? streak : PREVIEW_STREAK;
  });

  // Frame on the try-on avatar: a ring skin being tried shows on the streak ring.
  protected readonly previewFrame = computed(() =>
    'ringSkin' in this.trying() && this.trying().ringSkin ? FRAME_STREAK : this.user()?.frameColor
  );

  // Streak for the ring-skin tiles: yours, or silver when you have no ring yet.
  protected readonly ringStreak = computed(() => {
    const streak = this.user()?.highestStreak ?? 0;
    return streak >= FIRST_RING_DAYS ? streak : PREVIEW_STREAK;
  });

  // Open tab: 'items', a cosmetic section id, or 'history'.
  protected readonly tab = signal('items');

  // Rarity chips (1 rare, 2 epic, 3 legendary) in the glyph tile tones, inside the palette.
  protected readonly rarityClass: Record<number, string> = {
    1: 'vp-glyph vp-glyph--sage',
    2: 'vp-glyph vp-glyph--frost',
    3: 'vp-glyph vp-glyph--honey',
  };

  protected readonly sections = computed(() => {
    const owned = this.owned();
    const equipped = this.equipped();
    const trying = this.trying();
    const balance = this.balance();
    return KIND_SECTIONS.map((section) => ({
      ...section,
      title: this.t(`shop.kinds.${section.id}` as TranslationKey),
      hint: this.t(`shop.kindHints.${section.id}` as TranslationKey),
      items: COSMETICS.filter((c) => c.kind === section.kind).map((c) => ({
        ...c,
        label: this.t(c.name),
        rarityLabel: this.t(RARITY_KEYS[c.rarity]),
        owned: owned.has(c.key),
        equipped: equipped[section.slot] === c.key,
        tried: trying[section.slot] === c.key,
        missing: Math.max(0, c.price - balance),
        colorClass: nameColorClass(c.key),
        swatch: THEME_SWATCHES[c.key] ?? null,
        look: {
          ringSkin: c.kind === CosmeticKind.RingSkin ? c.key : null,
          nameColor: null,
        } as Look,
      })),
    }));
  });

  protected readonly defaultThemeEquipped = computed(() => !this.equipped().theme);
  protected readonly defaultThemeTried = computed(() => this.trying().theme === null);
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
      glyph: REASON_GLYPH[item.reason] ?? 'wallet',
      label: this.t(REASON_LABELS[item.reason] ?? 'coins.reasons.vote', {
        name: this.t(cosmeticByKey(item.itemKey)?.name ?? 'shop.cosmeticsTitle'),
      }),
      date: format.format(new Date(item.createdAt)),
    }));
  });

  ngOnDestroy(): void {
    this.themeService.previewPalette.set(undefined);
  }

  // Tap an item to try it on, again to stop. A theme is tried on the whole app.
  tryOn(slot: Slot | null, key: string | null): void {
    if (!slot) return;
    const trying = this.trying();
    const stop = slot in trying && trying[slot] === key;
    this.trying.update((t) => {
      const copy = { ...t };
      if (stop) delete copy[slot];
      else copy[slot] = key;
      return copy;
    });
    if (slot === 'theme') this.themeService.previewPalette.set(stop ? undefined : themeAttr(key));
  }

  stopTrying(): void {
    this.trying.set({});
    this.themeService.previewPalette.set(undefined);
  }

  buyCosmetic(entry: CosmeticEntry): void {
    if (this.cosmeticBusy()) return;
    this.cosmeticBusy.set(entry.key);
    this.shopService.buyCosmetic(entry.key).subscribe({
      next: () => {
        this.cosmeticBusy.set(null);
        toast.success(this.t('shop.bought', { name: this.t(entry.name) }));
        // The purchase is a new ledger row: reload from the first page.
        this.items.set([]);
        this.loadPage(null);
      },
      error: (err) => {
        this.cosmeticBusy.set(null);
        toast.error(apiErrorMessage(err, this.t('shop.buyFailed')));
      },
    });
  }

  // Wears an owned item in its slot, or with null takes the slot's item off.
  equip(section: { field: string; slot: Slot }, key: string | null): void {
    const { field, slot } = section;
    if (this.cosmeticBusy()) return;
    this.cosmeticBusy.set(key ?? field);
    this.userService.updateProfile({ [field]: key ?? '' }).subscribe({
      next: (profile) => {
        this.cosmeticBusy.set(null);
        this.authService.patchCurrentUser({ look: profile.look, themeKey: profile.themeKey });
        // Worn now, so no longer a try-on.
        if (slot in this.trying()) {
          this.trying.update((t) => {
            const copy = { ...t };
            delete copy[slot];
            return copy;
          });
          if (slot === 'theme') this.themeService.previewPalette.set(undefined);
        }
      },
      error: (err) => {
        this.cosmeticBusy.set(null);
        toast.error(apiErrorMessage(err, this.t('shop.equipFailed')));
      },
    });
  }

  ngOnInit(): void {
    // Selector rewards are paid by the daily tick, so refresh the balance on entry.
    this.userService.getProfile().subscribe({
      next: profile =>
        this.authService.patchCurrentUser({
          coins: profile.coins,
          inventory: profile.inventory,
          look: profile.look,
          themeKey: profile.themeKey,
          ownedCosmetics: profile.ownedCosmetics,
          highestStreak: profile.highestStreak,
          title: profile.title,
        }),
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
