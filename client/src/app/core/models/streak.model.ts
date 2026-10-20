import type { Look } from '../constants/shop-catalog';
import { QuestionResult } from './result.model';
import { CoinsEarned } from './coin.model';
import type { AuthorGuess } from './daily.model';
import { StreakTierKey } from '../constants/streak-tiers';

// Mirrors backend StreakUpdateDto — returned with a successful vote. The DTO's material
// names are ignored: the client names materials in the UI language from the tier keys.
export interface StreakUpdate {
  previous: number;
  current: number;
  best: number;
  tierKey: StreakTierKey | null;
  tierTitle: string | null;
  isTierUp: boolean;
  titleUnlocked: boolean; // the tier's title was unlocked for the first time ever
  milestoneLabel: string | null;
  nextTierKey: StreakTierKey | null;
  nextTierTitle: string | null;
  nextTierAt: number | null;
  daysToNextTier: number | null;
}

// POST daily/vote response.
export interface VoteResponse {
  results: QuestionResult;
  streak: StreakUpdate;
  coins: CoinsEarned; // rama 34
  guess: AuthorGuess | null; // the "guess the author" step (rama 36)
}

// The current user's streak in a group (GET daily/current → myStreak).
export interface MyStreak {
  current: number;
  best: number;
  lostStreak: number | null;
  // Streak freeze & repair (rama 35, §17.9)
  freezes: number;
  maxFreezes: number;
  freezeSavedStreak: number | null; // "your freeze saved your streak" notice
  freezeGiftFrom: string | null; // who last gave you a freeze, until dismissed
  repairableStreak: number | null; // what a repair would restore; null = no repair now
  freezePrice: number;
  repairPrice: number;
}

// Member detail card (GET groups/{id}/members/{userId}/stats, GET users/me/group-stats).
export interface MemberStats {
  groupId: number;
  groupName: string;
  userId: number;
  username: string;
  avatarUrl: string | null;
  frameColor: string | null;
  currentStreak: number;
  bestStreak: number;
  hasCrown: boolean;
  title: string | null;
  look: Look | null; // shop cosmetics (rama 42)
  joinedAt: string; // behind the seniority frame
  totalVotes: number;
  activeQuestions: number;
  participationPercent: number;
  timesSelector: number;
  questionsCreated: number;
  guessesMade: number; // guess the author (rama 36), skips excluded
  guessesCorrect: number;
  // Duel record (rama 47, §17.15 item 6): closed duels in the group anyone voted on.
  duelsWon: number;
  duelsDrawn: number;
  duelsLost: number;
}

// An unlocked title the user can choose to show (GET users/me → unlockedTitles).
export interface TitleOption {
  key: StreakTierKey;
  title: string;
  material: string;
  minDays: number;
}
