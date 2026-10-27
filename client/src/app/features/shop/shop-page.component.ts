import { Component, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { toast } from '@spartan-ng/brain/sonner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideCheck, lucideChevronDown, lucideCrown, lucideInfo, lucidePause, lucidePlay, lucideX } from '@ng-icons/lucide';

import { HlmAlertDialogImports } from '@spartan-ng/helm/alert-dialog';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';
import { HlmSheet, HlmSheetImports } from '@spartan-ng/helm/sheet';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';

import { AuthService } from '../../core/auth/auth.service';
import { UserService } from '../../core/services/user.service';
import { ShopService } from '../../core/services/shop.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../shared/components/user-name/user-name.component';
import {
  COSMETICS,
  CosmeticEntry,
  CosmeticKind,
  CosmeticRarity,
  Look,
  cosmeticByKey,
  frameColorItemKey,
} from '../../core/constants/shop-catalog';
import { FRAME_COLOR_KEYS, FRAME_NONE, FRAME_STREAK, STREAK_TIERS, StreakTierKey, frameFollowsStreak, streakTierFor } from '../../core/constants/streak-tiers';
import { FinishKey, finishEvolves, isFinish } from '../../shared/components/streak-frame/ring-finishes';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';
import {
  AUTHOR_HINT_PRICE,
  COIN_DAILY_CAP,
  CatalogCosmetic,
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
  [CoinReason.QuestionPlayed]: 'coins.reasons.questionPlayed',
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
  [CoinReason.QuestionPlayed]: 'write',
};

const RARITY_KEYS: Record<CosmeticRarity, TranslationKey> = {
  [CosmeticRarity.Common]: 'shop.rarity.common',
  [CosmeticRarity.Rare]: 'shop.rarity.rare',
  [CosmeticRarity.Epic]: 'shop.rarity.epic',
  [CosmeticRarity.Legendary]: 'shop.rarity.legendary',
};

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

// The levels a finish is shown on (rama 57): no tier yet, the seven tiers, then the top tier
// wearing the group's crown. The finish tiles run through them in a loop, staggered so they
// don't all change at once; the detail sheet plays them big.
interface FinishStep {
  tier: StreakTierKey | null;
  streak: number;
  crown: boolean;
}
const TOP_TIER = STREAK_TIERS[STREAK_TIERS.length - 1];
const FINISH_STEPS: readonly FinishStep[] = [
  { tier: null, streak: 0, crown: false },
  ...STREAK_TIERS.map((tier) => ({ tier: tier.key, streak: tier.minDays, crown: false })),
  { tier: TOP_TIER.key, streak: TOP_TIER.minDays, crown: true },
];
const STEP_MS = 1600;
const REDUCED_MOTION = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

const RARITY_GROUP_KEYS: Partial<Record<CosmeticRarity, { title: TranslationKey; hint: TranslationKey }>> = {
  [CosmeticRarity.Rare]: { title: 'shop.rarityGroups.rare.title', hint: 'shop.rarityGroups.rare.hint' },
  [CosmeticRarity.Epic]: { title: 'shop.rarityGroups.epic.title', hint: 'shop.rarityGroups.epic.hint' },
  [CosmeticRarity.Legendary]: { title: 'shop.rarityGroups.legendary.title', hint: 'shop.rarityGroups.legendary.hint' },
};

// What a style tile puts on: a ring finish, or a frame (the streak ring, no frame or a colour).
type StyleChoice = { finish: string } | { frame: string };

interface StyleTile {
  key: string; // catalog key, or the frame value for the free frames
  label: string;
  rarityLabel: string | null;
  rarity: CosmeticRarity;
  price: number; // 0 = free
  owned: boolean;
  equipped: boolean;
  tried: boolean;
  missing: number;
  choice: StyleChoice;
  entry: CosmeticEntry | null; // null for the free frames
  // Seasonal (rama 57): how long it is still sold, or that it no longer is.
  seasonLeft: string | null;
  seasonOver: boolean;
  // Coming soon: how long until it goes on sale (not buyable yet).
  seasonSoon: string | null;
  // What the tile's avatar wears.
  frameColor: string;
  look: Look;
  // Finish tiles: where the tile starts in the loop of levels.
  offset: number;
}

// Coins & shop (rama 34, §17.2; rethought in rama 57, §17.15 item 8): the balance in the
// header opens the ledger in a bottom sheet and an "i" holds the explanations; two tabs that
// are always in view — «Objetos», the game items bought ahead (rama 45), and «Estilo»: the
// season's frame, the ring finishes and the frame colours, tried on your own avatar first.
@Component({
  selector: 'app-shop-page',
  imports: [
    NgTemplateOutlet,
    PageHeaderComponent,
    HlmTabsImports,
    CoinTextComponent,
    CoinComponent,
    GlyphComponent,
    NgIcon,
    UserAvatarComponent,
    UserNameComponent,
    HlmAlertDialogImports,
    HlmButtonImports,
    HlmCardImports,
    HlmItemImports,
    HlmPopoverImports,
    HlmSheetImports,
    HlmSpinnerImports,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideCheck, lucideChevronDown, lucideCrown, lucideInfo, lucidePause, lucidePlay, lucideX })],
  templateUrl: './shop-page.component.html',
})
export class ShopPageComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly shopService = inject(ShopService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly dailyCap = COIN_DAILY_CAP;

  protected readonly balance = computed(() => this.authService.currentUser()?.coins ?? 0);
  protected readonly items = signal<CoinTransaction[]>([]);
  protected readonly nextBefore = signal<number | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  private historyLoaded = false;
  protected readonly busy = signal<InventoryItem | null>(null);

  // Open tab: 'items' or 'style' (the seasonal push opens ?tab=style).
  protected readonly tab = signal(this.route.snapshot.queryParamMap.get('tab') === 'style' ? 'style' : 'items');

  // ── Style (rama 42, rama 57) ──
  protected readonly user = this.authService.currentUser;
  protected readonly cosmeticBusy = signal<string | null>(null);
  // What is being tried on: a finish or a frame, over what you wear.
  protected readonly trying = signal<StyleChoice | null>(null);

  // The server's catalog: what is on sale now, the seasonal windows, what you own.
  private readonly catalog = signal<CatalogCosmetic[] | null>(null);
  // Ticks every 30 s for the seasonal countdown.
  private readonly now = signal(Date.now());
  private readonly ticker = setInterval(() => this.now.set(Date.now()), 30_000);

  // ── Levels of a finish (rama 57) ──
  protected readonly steps = FINISH_STEPS;
  // Drives the finish tiles' loop while «Estilo» is open; still under reduced motion, where
  // every tile rests on the top tier with the crown.
  private readonly cycle = signal(0);
  // The finish open in the detail sheet, the level it shows and whether it plays them.
  protected readonly detailKey = signal<string | null>(null);
  protected readonly detailStep = signal(0);
  protected readonly detailPlaying = signal(false);
  private readonly finishSheet = viewChild<HlmSheet>('finishSheet');
  private readonly stepper = setInterval(() => {
    if (REDUCED_MOTION) return;
    if (this.tab() === 'style') this.cycle.update((c) => c + 1);
    if (this.detailKey() && this.detailPlaying()) {
      this.detailStep.update((s) => (s + 1) % FINISH_STEPS.length);
      this.revealStep();
    }
  }, STEP_MS);

  private readonly owned = computed(() => new Set(this.user()?.ownedCosmetics ?? []));
  private readonly wornFinish = computed(() =>
    frameFollowsStreak(this.user()?.frameColor) ? (this.user()?.look?.ringSkin ?? null) : null
  );
  private readonly wornFrame = computed(() => (frameFollowsStreak(this.user()?.frameColor) ? FRAME_STREAK : this.user()?.frameColor ?? FRAME_STREAK));

  // The try-on avatar: what you wear, or what you are trying.
  protected readonly previewLook = computed<Look>(() => {
    const trying = this.trying();
    if (trying && 'finish' in trying) return { ringSkin: trying.finish };
    if (trying) return { ringSkin: null };
    return { ringSkin: this.wornFinish() };
  });
  protected readonly previewFrame = computed(() => {
    const trying = this.trying();
    if (trying && 'frame' in trying) return trying.frame;
    if (trying) return FRAME_STREAK;
    return this.user()?.frameColor;
  });
  protected readonly ringStreak = computed(() => this.user()?.highestStreak ?? 0);

  // Rarity chips (1 rare, 2 epic, 3 legendary) in the glyph tile tones, inside the palette.
  protected readonly rarityClass: Record<number, string> = {
    1: 'vp-glyph vp-glyph--sage',
    2: 'vp-glyph vp-glyph--frost',
    3: 'vp-glyph vp-glyph--honey',
  };

  private tile(entry: CosmeticEntry | null, key: string, label: string, choice: StyleChoice, frameColor: string, look: Look, extra: Partial<StyleTile> = {}): StyleTile {
    const trying = this.trying();
    const equipped =
      'finish' in choice ? this.wornFinish() === choice.finish : !this.wornFinish() && this.wornFrame() === choice.frame;
    const tried = !!trying && JSON.stringify(trying) === JSON.stringify(choice);
    const price = entry?.price ?? 0;
    const owned = !entry || this.owned().has(entry.key);
    return {
      key,
      label,
      rarity: entry?.rarity ?? CosmeticRarity.Common,
      rarityLabel: entry && entry.rarity !== CosmeticRarity.Common ? this.t(RARITY_KEYS[entry.rarity]) : null,
      price,
      owned,
      equipped,
      tried,
      missing: owned ? 0 : Math.max(0, price - this.balance()),
      choice,
      entry,
      seasonLeft: null,
      seasonOver: false,
      seasonSoon: null,
      frameColor,
      look,
      offset: 0,
      ...extra,
    };
  }

  // The season's frames: on sale now, or owned from an earlier season (rama 57).
  protected readonly seasonal = computed<StyleTile[]>(() =>
    (this.catalog() ?? [])
      .filter((c) => c.onSaleUntil !== null)
      .map((c, i) => {
        const entry = cosmeticByKey(c.key);
        if (!entry) return null;
        return this.tile(entry, c.key, this.t(entry.name), { finish: c.key }, FRAME_STREAK, { ringSkin: c.key }, {
          ...this.seasonState(c),
          offset: i * 3 + 1,
        });
      })
      .filter((t): t is StyleTile => t !== null)
  );

  // The ring finishes in one group per rarity (§17.16), cheapest first.
  protected readonly finishGroups = computed(() => {
    const finishes = COSMETICS.filter((c) => c.kind === CosmeticKind.RingSkin && !c.seasonal).map((c, i) =>
      this.tile(c, c.key, this.t(c.name), { finish: c.key }, FRAME_STREAK, { ringSkin: c.key }, { offset: i * 3 })
    );
    return [CosmeticRarity.Rare, CosmeticRarity.Epic, CosmeticRarity.Legendary].map((rarity) => ({
      rarity,
      title: this.t(RARITY_GROUP_KEYS[rarity]!.title),
      hint: this.t(RARITY_GROUP_KEYS[rarity]!.hint),
      tiles: finishes.filter((tile) => tile.rarity === rarity),
    }));
  });

  // The level a finish tile shows now, and its place in the loop.
  protected stepAt(offset: number): { step: FinishStep; index: number } {
    const index = REDUCED_MOTION ? FINISH_STEPS.length - 1 : (this.cycle() + offset) % FINISH_STEPS.length;
    return { step: FINISH_STEPS[index], index };
  }

  protected materialName(tier: StreakTierKey): string {
    return this.t(`streaks.materials.${tier}` as TranslationKey);
  }

  protected growsText(key: string): string {
    return this.t(`shop.grows.${key}` as TranslationKey);
  }

  // "Bronce · 15 d", "Sin nivel", "Con la corona".
  protected stepLabel(step: FinishStep): string {
    if (step.crown) return this.t('shop.detail.levelCrown');
    if (!step.tier) return this.t('shop.detail.none');
    return this.t('shop.detail.level', { tier: this.materialName(step.tier), days: step.streak });
  }

  // The level your best streak reaches now, marked «Tú» in the detail sheet.
  protected readonly myStepIndex = computed(() => {
    const tier = streakTierFor(this.ringStreak());
    return tier ? FINISH_STEPS.findIndex((s) => s.tier === tier.key && !s.crown) : 0;
  });

  // The finish open in the detail sheet, as a tile (price, owned, worn).
  protected readonly detail = computed<StyleTile | null>(() => {
    const key = this.detailKey();
    if (!key) return null;
    return [...this.seasonal(), ...this.finishGroups().flatMap((g) => g.tiles)].find((tile) => tile.key === key) ?? null;
  });
  protected readonly detailEvolves = computed(() => {
    const key = this.detailKey();
    return !!key && isFinish(key) && finishEvolves(key as FinishKey);
  });
  protected readonly detailCaption = computed(() => {
    const step = FINISH_STEPS[this.detailStep()];
    if (step.crown) return { level: this.t('shop.detail.levelCrown'), title: this.t('shop.detail.crownCaption') };
    if (!step.tier) return { level: `${this.t('shop.detail.none')} · ${this.t('shop.detail.noneDaysLong')}`, title: this.t('shop.detail.neutralCaption') };
    return {
      level: `${this.materialName(step.tier)} · ${this.t('shop.detail.daysLong', { days: step.streak })}`,
      title: this.t(`streaks.titles.${step.tier}` as TranslationKey),
    };
  });
  protected readonly detailNote = computed(() => {
    if (!this.detailEvolves()) return this.t('shop.detail.noteFixed');
    const streak = this.ringStreak();
    const tier = streakTierFor(streak);
    if (!tier) return this.t('shop.detail.noteStart');
    return this.t('shop.detail.note', { days: streak, tier: this.materialName(tier.key) });
  });

  // The free streak ring, the plain colours, and no frame.
  protected readonly frames = computed<StyleTile[]>(() => [
    this.tile(null, FRAME_STREAK, this.t('shop.frameStreak'), { frame: FRAME_STREAK }, FRAME_STREAK, { ringSkin: null }),
    ...FRAME_COLOR_KEYS.map((color) => {
      const entry = cosmeticByKey(frameColorItemKey(color))!;
      return this.tile(entry, entry.key, this.t(`profile.frameColors.${color}` as TranslationKey), { frame: color }, color, { ringSkin: null });
    }),
    this.tile(null, FRAME_NONE, this.t('shop.frameNone'), { frame: FRAME_NONE }, FRAME_NONE, { ringSkin: null }),
  ]);

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

  ngOnInit(): void {
    // Selector rewards are paid by the daily tick, so refresh the balance on entry.
    this.userService.getProfile().subscribe({
      next: profile =>
        this.authService.patchCurrentUser({
          coins: profile.coins,
          inventory: profile.inventory,
          frameColor: profile.frameColor,
          look: profile.look,
          ownedCosmetics: profile.ownedCosmetics,
          highestStreak: profile.highestStreak,
          title: profile.title,
        }),
    });
    this.shopService.getCatalog().subscribe({ next: (catalog) => this.catalog.set(catalog.cosmetics) });
  }

  ngOnDestroy(): void {
    clearInterval(this.ticker);
    clearInterval(this.stepper);
  }

  // Opens a finish's levels from the first one, playing unless motion is reduced.
  openDetail(tile: StyleTile): void {
    this.detailKey.set(tile.key);
    this.detailStep.set(REDUCED_MOTION ? FINISH_STEPS.length - 1 : 0);
    this.detailPlaying.set(!REDUCED_MOTION);
    this.finishSheet()?.open();
  }

  // Tapping a level stops the loop there.
  showStep(index: number): void {
    this.detailPlaying.set(false);
    this.detailStep.set(index);
  }

  // Keeps the level being played in view in the sheet's row of levels.
  private revealStep(): void {
    document
      .querySelector(`[data-finish-step="${this.detailStep()}"]`)
      ?.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });
  }

  togglePlaying(): void {
    this.detailPlaying.update((playing) => !playing);
  }

  // Where a seasonal item stands, by the ticking clock, so a coming-soon frame turns buyable
  // the moment its window opens, without reloading.
  private seasonState(c: CatalogCosmetic): Pick<StyleTile, 'seasonLeft' | 'seasonOver' | 'seasonSoon'> {
    const now = this.now();
    const from = c.onSaleFrom ? Date.parse(c.onSaleFrom) : null;
    const until = Date.parse(c.onSaleUntil!);
    if (from !== null && now < from) return { seasonLeft: null, seasonOver: false, seasonSoon: this.timeLeft(c.onSaleFrom!) };
    if (now < until) return { seasonLeft: this.timeLeft(c.onSaleUntil!), seasonOver: false, seasonSoon: null };
    return { seasonLeft: null, seasonOver: true, seasonSoon: null };
  }

  // "6 d 3 h", "5 h 12 min", "8 min": what is left until a seasonal date.
  private timeLeft(until: string): string {
    const minutes = Math.max(0, Math.floor((Date.parse(until) - this.now()) / 60_000));
    const d = Math.floor(minutes / 1440);
    const h = Math.floor((minutes % 1440) / 60);
    const m = minutes % 60;
    const unit = (n: number, key: 'days' | 'hours' | 'minutes') => `${n} ${this.t(`countdown.${key}`)}`;
    if (d > 0) return `${unit(d, 'days')} ${unit(h, 'hours')}`;
    if (h > 0) return `${unit(h, 'hours')} ${unit(m, 'minutes')}`;
    return unit(m, 'minutes');
  }

  // Tap a tile to try it on, again to stop.
  tryOn(choice: StyleChoice): void {
    const trying = this.trying();
    this.trying.set(trying && JSON.stringify(trying) === JSON.stringify(choice) ? null : choice);
  }

  stopTrying(): void {
    this.trying.set(null);
  }

  buyCosmetic(tile: StyleTile): void {
    if (this.cosmeticBusy() || !tile.entry) return;
    const entry = tile.entry;
    this.cosmeticBusy.set(entry.key);
    this.shopService.buyCosmetic(entry.key).subscribe({
      next: () => {
        this.cosmeticBusy.set(null);
        toast.success(this.t('shop.bought', { name: tile.label }));
        // The purchase is a new ledger row: the sheet reloads from the first page next time.
        this.items.set([]);
        this.historyLoaded = false;
      },
      error: (err) => {
        this.cosmeticBusy.set(null);
        toast.error(apiErrorMessage(err, this.t('shop.buyFailed')));
      },
    });
  }

  // Wears a finish or a frame; unequip takes the finish off (back to the plain streak ring).
  equip(tile: StyleTile, unequip = false): void {
    if (this.cosmeticBusy()) return;
    this.cosmeticBusy.set(tile.key);
    const choice = tile.choice;
    const request = 'finish' in choice ? { ringSkinKey: unequip ? '' : choice.finish } : { frameColor: choice.frame };
    this.userService.updateProfile(request).subscribe({
      next: (profile) => {
        this.cosmeticBusy.set(null);
        this.authService.patchCurrentUser({ look: profile.look, frameColor: profile.frameColor });
        // Worn now, so no longer a try-on.
        this.trying.set(null);
      },
      error: (err) => {
        this.cosmeticBusy.set(null);
        toast.error(apiErrorMessage(err, this.t('shop.equipFailed')));
      },
    });
  }

  buy(item: InventoryItem): void {
    if (this.busy() !== null) return;
    const slot = SHOP_ITEMS.find(entry => entry.item === item)!.slot;
    this.busy.set(item);
    this.shopService.buyItem(item).subscribe({
      next: res => {
        this.busy.set(null);
        toast.success(this.t('inventory.bought', { count: res.inventory[slot] }));
        this.items.set([]);
        this.historyLoaded = false;
      },
      error: err => {
        this.busy.set(null);
        toast.error(apiErrorMessage(err, this.t('inventory.buyFailed')));
      },
    });
  }

  // The ledger loads when its sheet first opens.
  openHistory(): void {
    if (this.historyLoaded) return;
    this.historyLoaded = true;
    this.items.set([]);
    this.loadPage(null);
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
