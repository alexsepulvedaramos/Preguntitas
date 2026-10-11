import { frameFollowsStreak, nextStreakTier, streakTierFor } from './streak-tiers';

// Mirrors the backend Helpers/StreakTiers.cs (rama 19): the frame appears on day 3.
describe('streak tiers', () => {
  it.each([
    [0, null],
    [2, null],
    [3, 'wood'],
    [6, 'wood'],
    [7, 'stone'],
    [15, 'bronze'],
    [30, 'silver'],
    [100, 'gold'],
    [181, 'gold'],
    [182, 'sapphire'],
    [365, 'amethyst'],
    [1000, 'amethyst'],
  ])('a %i-day streak wears the %s frame', (days, key) => {
    expect(streakTierFor(days)?.key ?? null).toBe(key);
  });

  it('has no frame for a missing streak', () => {
    expect(streakTierFor(null)).toBeNull();
    expect(streakTierFor(undefined)).toBeNull();
  });

  it('points to the next frame to reach, and to none at the top', () => {
    expect(nextStreakTier(0)?.key).toBe('wood');
    expect(nextStreakTier(3)?.key).toBe('stone');
    expect(nextStreakTier(364)?.key).toBe('amethyst');
    expect(nextStreakTier(365)).toBeNull();
  });

  it('follows the streak unless a fixed colour or "none" was chosen', () => {
    expect(frameFollowsStreak(null)).toBe(true);
    expect(frameFollowsStreak('streak')).toBe(true);
    expect(frameFollowsStreak('#ff3b5c')).toBe(false);
    expect(frameFollowsStreak('none')).toBe(false);
  });
});
