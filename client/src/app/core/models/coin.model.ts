import type { MyStreak } from './streak.model';

// Coin economy (rama 34, §17.2). Mirrors the backend CoinReason — never renumber.
export enum CoinReason {
  Vote = 0,
  FirstVoter = 1,
  StreakTier = 2,
  StreakMilestone = 3,
  SelectorChoice = 4,
  SelectorCreated = 5,
  // Spending (rama 35)
  StreakFreeze = 6,
  StreakFreezeGift = 7,
  StreakRepair = 8,
  // Guess the author (rama 36)
  GuessCorrect = 9,
  AuthorHint = 10,
  // Race to select (rama 40)
  RaceWon = 11,
  // Flash question (rama 44)
  FlashQuestion = 12,
  // Shop cosmetics (rama 42)
  Cosmetic = 13,
  // Shop and economy revision (rama 46, §17.15)
  StreakBonus = 14,
  CosmeticRefund = 15, // a retired cosmetic given back at its price
  // Change my vote and toque / nudge (rama 43)
  VoteChange = 16,
  Nudge = 17,
  // Flash variants (rama 50)
  FlashAnonymousVotes = 18,
  FlashCapsule = 19,
  FlashExtension = 20,
  FlashAnonymousAuthor = 21,
}

// Coins paid by one request (POST daily/vote → coins).
export interface CoinsEarned {
  earned: number;
  streakBonus?: number; // the part of `earned` that is the daily streak bonus (§17.15)
  balance: number; // after paying
  capReached: boolean; // the daily cap cut a reward short
}

export interface CoinTransaction {
  id: number;
  amount: number;
  reason: CoinReason;
  groupId: number | null;
  groupName: string | null;
  itemKey?: string | null; // the cosmetic, for CoinReason.Cosmetic (rama 42) and CosmeticRefund
  createdAt: string;
}

// GET users/me/coins/history: newest first, 20 per page.
export interface CoinHistoryPage {
  items: CoinTransaction[];
  nextBefore: number | null; // pass as ?before= for the next page
}

// Capped coins (voting, choosing) per local day, across all groups.
export const COIN_DAILY_CAP = 40;

// POST shop/purchase (rama 35): a streak freeze (a gift when targetUserId is someone
// else) or a streak repair, always in one group.
export type ShopItem = 'streakFreeze' | 'streakRepair';

export interface PurchaseRequest {
  item: ShopItem;
  groupId: number;
  targetUserId?: number;
  useOwned?: boolean; // take one from the inventory instead of paying (rama 45)
}

export interface PurchaseResponse {
  balance: number;
  myStreak: MyStreak; // the buyer's streak in that group, after the purchase
  inventory: Inventory;
}

// Item inventory (rama 45, §17.10): game items bought ahead in the shop and used later in
// any group. Mirrors the backend InventoryItem — never renumber.
export enum InventoryItem {
  FlashQuestion = 0,
  StreakFreeze = 1,
  AuthorHint = 2,
  StreakRepair = 3,
  // Rama 43 (§17.10)
  VoteChange = 4,
  Nudge = 5,
}

// How many of each item the user holds (GET users/me → inventory, and every purchase/use).
export interface Inventory {
  flashQuestion: number;
  streakFreeze: number;
  authorHint: number;
  streakRepair: number;
  voteChange: number;
  nudge: number;
}

export const EMPTY_INVENTORY: Inventory = {
  flashQuestion: 0,
  streakFreeze: 0,
  authorHint: 0,
  streakRepair: 0,
  voteChange: 0,
  nudge: 0,
};

// GET shop (rama 42): the cosmetics catalog, with what you own.
export interface ShopCatalog {
  cosmetics: { key: string; kind: number; price: number; rarity: number; owned: boolean }[];
}

// POST shop/cosmetics
export interface BuyCosmeticResponse {
  balance: number;
  ownedCosmetics: string[];
}

// POST shop/inventory
export interface BuyItemResponse {
  balance: number;
  inventory: Inventory;
}

export const STREAK_FREEZE_PRICE = 100;
export const STREAK_REPAIR_PRICE = 300;

// The author hint in "guess the author" (rama 36, §17.10).
export const AUTHOR_HINT_PRICE = 20;

// Change my vote and toque / nudge (rama 43, §17.10).
export const VOTE_CHANGE_PRICE = 50;
export const NUDGE_PRICE = 10;
export const NUDGE_MESSAGE_MAX = 80;

// A flash question (rama 44, §17.11): launched at once, open 24 h.
export const FLASH_QUESTION_PRICE = 100;

// Flash variants (rama 50, §17.15 item 6): combinable add-ons paid in coins on top of the
// flash, and the +24 h extension any member can buy once per flash.
// Hiding the author never costs less than hiding the votes, so trolling isn't the cheap option.
export const FLASH_ANONYMOUS_AUTHOR_PRICE = 50;
export const FLASH_ANONYMOUS_VOTES_PRICE = 50;
export const FLASH_CAPSULE_PRICE = 50;
export const FLASH_EXTENSION_PRICE = 30;
export const FLASH_CAPSULE_MIN_DAYS = 1;
export const FLASH_CAPSULE_MAX_DAYS = 365;

// Winning the race to select (rama 40, §17.5).
export const RACE_WON_REWARD = 10;

// A right "guess the author" (rama 36, §17.6).
export const AUTHOR_GUESS_REWARD = 5;
