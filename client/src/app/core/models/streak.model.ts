import { QuestionResult } from './result.model';
import { StreakTierKey } from '../constants/streak-tiers';

// Mirrors backend StreakUpdateDto — returned with a successful vote.
export interface StreakUpdate {
  previous: number;
  current: number;
  best: number;
  tierKey: StreakTierKey | null;
  tierName: string | null;
  isTierUp: boolean;
  milestoneLabel: string | null;
  nextTierKey: StreakTierKey | null;
  nextTierName: string | null;
  nextTierAt: number | null;
  daysToNextTier: number | null;
}

// POST daily/vote response.
export interface VoteResponse {
  results: QuestionResult;
  streak: StreakUpdate;
}

// The current user's streak in a group (GET daily/current → myStreak).
export interface MyStreak {
  current: number;
  best: number;
  lostStreak: number | null;
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
  totalVotes: number;
  activeQuestions: number;
  participationPercent: number;
  timesSelector: number;
  questionsCreated: number;
}
