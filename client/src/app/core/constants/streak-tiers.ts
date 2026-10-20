import { TranslationKey } from '../i18n/translation.types';

// Streak tiers, titles and milestones (rama 19, spec §13). Mirrors the backend
// Helpers/StreakTiers.cs — keep both in sync. Titles are translation keys, adapted per
// language (rama 24); material names are UI copy (STREAK_MATERIAL_KEYS).

export type StreakTierKey =
  | 'wood'
  | 'stone'
  | 'bronze'
  | 'silver'
  | 'gold'
  | 'sapphire'
  | 'amethyst';

export interface StreakTier {
  key: StreakTierKey;
  // Unlockable title shown on the profile and member card — render with t(titleKey).
  titleKey: TranslationKey;
  minDays: number;
}

// Below the first tier (days 1–2) there is no frame and no title.
export const STREAK_TIERS: readonly StreakTier[] = [
  { key: 'wood', titleKey: 'streaks.titles.wood', minDays: 3 },
  { key: 'stone', titleKey: 'streaks.titles.stone', minDays: 7 },
  { key: 'bronze', titleKey: 'streaks.titles.bronze', minDays: 15 },
  { key: 'silver', titleKey: 'streaks.titles.silver', minDays: 30 },
  { key: 'gold', titleKey: 'streaks.titles.gold', minDays: 100 },
  { key: 'sapphire', titleKey: 'streaks.titles.sapphire', minDays: 182 },
  { key: 'amethyst', titleKey: 'streaks.titles.amethyst', minDays: 365 },
];

// The frame named inside a sentence ("marco de madera", "wood frame", "Holzrahmen").
export const STREAK_FRAME_KEYS: Record<StreakTierKey, TranslationKey> = {
  wood: 'streaks.frames.wood',
  stone: 'streaks.frames.stone',
  bronze: 'streaks.frames.bronze',
  silver: 'streaks.frames.silver',
  gold: 'streaks.frames.gold',
  sapphire: 'streaks.frames.sapphire',
  amethyst: 'streaks.frames.amethyst',
};

// Frame material name per tier — render with t(STREAK_MATERIAL_KEYS[key]).
export const STREAK_MATERIAL_KEYS: Record<StreakTierKey, TranslationKey> = {
  wood: 'streaks.materials.wood',
  stone: 'streaks.materials.stone',
  bronze: 'streaks.materials.bronze',
  silver: 'streaks.materials.silver',
  gold: 'streaks.materials.gold',
  sapphire: 'streaks.materials.sapphire',
  amethyst: 'streaks.materials.amethyst',
};

export const FIRST_RING_DAYS = STREAK_TIERS[0].minDays;

export function streakTierFor(streak: number | null | undefined): StreakTier | null {
  if (!streak) return null;
  return [...STREAK_TIERS].reverse().find((t) => streak >= t.minDays) ?? null;
}

export function nextStreakTier(streak: number): StreakTier | null {
  return STREAK_TIERS.find((t) => streak < t.minDays) ?? null;
}

// Daily streak bonus (rama 46, §17.15): coins added to every on-time vote, by streak day.
// Mirrors the backend CoinRewards.StreakBonusSteps — keep both in sync.
export const STREAK_BONUS_STEPS: readonly { fromDay: number; coins: number }[] = [
  { fromDay: 3, coins: 1 },
  { fromDay: 7, coins: 2 },
  { fromDay: 15, coins: 3 },
  { fromDay: 30, coins: 4 },
  { fromDay: 100, coins: 5 },
];

export function streakBonus(streak: number): number {
  return [...STREAK_BONUS_STEPS].reverse().find((s) => streak >= s.fromDay)?.coins ?? 0;
}

export function nextStreakBonusStep(streak: number): { fromDay: number; coins: number } | null {
  return STREAK_BONUS_STEPS.find((s) => streak < s.fromDay) ?? null;
}

// Special User.frameColor values besides a plain hex colour.
export const FRAME_STREAK = 'streak';
export const FRAME_NONE = 'none';
// Free seniority frame by time in the group (rama 42, §17.10).
export const FRAME_SENIORITY = 'seniority';

// Special title selections besides a tier key.
export const TITLE_AUTO = 'auto';
export const TITLE_NONE = 'none';

// null/undefined is treated as "streak" (the default since rama 19).
export function frameFollowsStreak(frameColor: string | null | undefined): boolean {
  return frameColor == null || frameColor === FRAME_STREAK;
}

export function isHexFrame(frameColor: string | null | undefined): frameColor is string {
  return !!frameColor && frameColor.startsWith('#');
}

// ── Frame look per material ────────────────────────────────────────────────
// stops: rim gradient (dark → light → dark). Wood and stone are deliberately humble:
// no shine, no glow, no animation. From bronze on, a light sweeps the rim.

export interface FrameLook {
  a: string; // main colour (glow, ribbon, celebration accents)
  b: string; // light colour
  core: string;
  aura?: string;
  stops?: string[];
  facets?: number;
  facet?: [string, string, string];
  nebula?: string[];
  sheen?: string;
  sheenDur?: number;
  grain?: string;
  speckle?: boolean;
  notches?: number;
  hot?: boolean;
  engraved?: boolean;
  rays?: boolean;
  sparks?: number;
  studs?: number;
  stud?: [string, string];
  topGem?: string;
  sideGems?: string;
  twinkles?: number;
  motes?: number;
  // Ring skins (rama 42): a blurred glowing tube over the rim, and an 8-bit rim of square
  // blocks in these colours instead of a gradient band.
  glow?: string;
  pixel?: string[];
}

export const FRAME_LOOKS: Record<StreakTierKey, FrameLook> = {
  wood: {
    a: '#a86a3a', b: '#e0b07c', core: '#f5dcc0',
    stops: ['#3b2312', '#7a4a26', '#b9824f', '#6b3f1f', '#3b2312'], grain: '#2a170a',
  },
  stone: {
    a: '#8d959c', b: '#dfe4e8', core: '#ffffff',
    stops: ['#33373b', '#767c82', '#b4b9be', '#62686e', '#33373b'], speckle: true, notches: 12,
  },
  bronze: {
    a: '#d4752d', b: '#ffc58a', core: '#fff0dc', aura: '#d4752d',
    stops: ['#4a2408', '#b8692a', '#f2b37a', '#a05a22', '#5a2e0c'], sheen: '#ffcf96', sheenDur: 6, hot: true, sparks: 5,
  },
  silver: {
    a: '#9fb2c8', b: '#ffffff', core: '#ffffff', aura: '#b9c6d6',
    stops: ['#4b525c', '#c9d0d9', '#ffffff', '#9aa3ae', '#4b525c'], sheen: '#ffffff', sheenDur: 4.5,
    studs: 4, stud: ['#ffffff', '#8c96a3'], twinkles: 3,
  },
  gold: {
    a: '#ffb72e', b: '#ffe58a', core: '#ffffff', aura: '#ffb72e',
    stops: ['#6b3d00', '#d79a1a', '#fff1b8', '#c98a10', '#6b3d00'], sheen: '#fff8dc', sheenDur: 3.8, engraved: true,
    studs: 8, stud: ['#fff6cf', '#b07a0c'], topGem: '#ff3b5c', twinkles: 6,
  },
  sapphire: {
    a: '#2f6bff', b: '#9fd2ff', core: '#ffffff', aura: '#2f6bff',
    facets: 18, facet: ['#0a2470', '#2f6bff', '#a8d0ff'], nebula: ['#0a2470', '#2f6bff', '#62d0ff'],
    sheen: '#dff0ff', sheenDur: 5, studs: 4, stud: ['#d8ecff', '#2b5fd9'], topGem: '#7fd0ff', twinkles: 5, motes: 5,
  },
  amethyst: {
    a: '#a24bff', b: '#f2b8ff', core: '#ffffff', aura: '#a24bff',
    facets: 22, facet: ['#2a0a50', '#8a2be2', '#f0c4ff'], nebula: ['#3b0a6b', '#c13cff', '#ff7ae0', '#5b1fd1'],
    sheen: '#ffe6ff', sheenDur: 4, rays: true, topGem: '#ff8af0', sideGems: '#e6b3ff', twinkles: 8, motes: 8,
  },
};

// Crown for the group's best current streak — independent of the material.
export const CROWN_LOOK = {
  stops: ['#6b3d00', '#d79a1a', '#fff1b8', '#c98a10', '#6b3d00'],
  gem: '#ff3b5c',
  side: '#4be3a6',
};
