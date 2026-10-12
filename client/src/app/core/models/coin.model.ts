// Coin economy (rama 34, §17.2). Mirrors the backend CoinReason — never renumber.
export enum CoinReason {
  Vote = 0,
  FirstVoter = 1,
  StreakTier = 2,
  StreakMilestone = 3,
  SelectorChoice = 4,
  SelectorCreated = 5,
}

// Coins paid by one request (POST daily/vote → coins).
export interface CoinsEarned {
  earned: number;
  balance: number; // after paying
  capReached: boolean; // the daily cap cut a reward short
}

export interface CoinTransaction {
  id: number;
  amount: number;
  reason: CoinReason;
  groupId: number | null;
  groupName: string | null;
  createdAt: string;
}

// GET users/me/coins/history: newest first, 20 per page.
export interface CoinHistoryPage {
  items: CoinTransaction[];
  nextBefore: number | null; // pass as ?before= for the next page
}

// Capped coins (voting, choosing) per local day, across all groups.
export const COIN_DAILY_CAP = 40;
