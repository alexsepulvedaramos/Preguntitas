// Streak ring tiers and milestones (rama 19, spec §13). Mirrors the backend
// Helpers/StreakTiers.cs — keep both in sync.

export type StreakTierKey =
  | 'ember'
  | 'flame-small'
  | 'flame-intense'
  | 'flame-blue'
  | 'flame-purple'
  | 'flame-gold';

export interface StreakTier {
  key: StreakTierKey;
  name: string;
  minDays: number;
}

// Below the first tier (days 1–2) there is no ring.
export const STREAK_TIERS: readonly StreakTier[] = [
  { key: 'ember', name: 'Brasa', minDays: 3 },
  { key: 'flame-small', name: 'Llama pequeña', minDays: 7 },
  { key: 'flame-intense', name: 'Llama intensa', minDays: 30 },
  { key: 'flame-blue', name: 'Llama azul', minDays: 100 },
  { key: 'flame-purple', name: 'Llama morada', minDays: 182 },
  { key: 'flame-gold', name: 'Llama dorada', minDays: 365 },
];

export const FIRST_RING_DAYS = STREAK_TIERS[0].minDays;

export function streakTierFor(streak: number | null | undefined): StreakTier | null {
  if (!streak) return null;
  return [...STREAK_TIERS].reverse().find((t) => streak >= t.minDays) ?? null;
}

export function nextStreakTier(streak: number): StreakTier | null {
  return STREAK_TIERS.find((t) => streak < t.minDays) ?? null;
}

// Special User.frameColor values besides a plain hex colour.
export const FRAME_STREAK = 'streak';
export const FRAME_NONE = 'none';

// null/undefined is treated as "streak" (the default since rama 19).
export function frameFollowsStreak(frameColor: string | null | undefined): boolean {
  return frameColor == null || frameColor === FRAME_STREAK;
}

export function isHexFrame(frameColor: string | null | undefined): frameColor is string {
  return !!frameColor && frameColor.startsWith('#');
}
