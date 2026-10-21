import { TranslationKey } from '../i18n/translation.types';
import { FrameLook, STREAK_TIERS, StreakTierKey } from './streak-tiers';

// Shop cosmetics (rama 42, spec §17.10). Mirrors the backend Helpers/ShopCatalog.cs (keys,
// kinds, prices, rarity) — keep both in sync. The art lives here: pure CSS/SVG, like the
// streak frames.

// Mirrors the backend CosmeticKind / CosmeticRarity — never renumber.
export enum CosmeticKind {
  RingSkin = 0,
  Accessory = 1, // retired (rama 43, §17.15)
  NameColor = 2,
  Badge = 3, // retired (§17.15)
  Title = 4, // retired (§17.15)
  Theme = 5,
}

export enum CosmeticRarity {
  Common = 0,
  Rare = 1,
  Epic = 2,
  Legendary = 3,
}

// What other members see on someone (GroupMember.look, MemberStats.look, the own profile).
export interface Look {
  ringSkin: string | null;
  nameColor: string | null;
}

export interface CosmeticEntry {
  key: string;
  kind: CosmeticKind;
  price: number;
  rarity: CosmeticRarity;
  name: TranslationKey;
}

const item = (key: string, kind: CosmeticKind, price: number, rarity = CosmeticRarity.Common): CosmeticEntry => ({
  key,
  kind,
  price,
  rarity,
  name: `shop.items.${key}` as TranslationKey,
});

export const COSMETICS: readonly CosmeticEntry[] = [
  item('ring-neon', CosmeticKind.RingSkin, 400),
  item('ring-pixel', CosmeticKind.RingSkin, 600, CosmeticRarity.Rare),
  item('ring-ice', CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
  item('ring-flames', CosmeticKind.RingSkin, 1000, CosmeticRarity.Legendary),

  item('color-coral', CosmeticKind.NameColor, 250),
  item('color-tangerine', CosmeticKind.NameColor, 250),
  item('color-gold', CosmeticKind.NameColor, 250),
  item('color-mint', CosmeticKind.NameColor, 250),
  item('color-sky', CosmeticKind.NameColor, 250),
  item('color-lavender', CosmeticKind.NameColor, 250),
  item('color-pink', CosmeticKind.NameColor, 250),
  item('color-lime', CosmeticKind.NameColor, 250),

  item('theme-ocean', CosmeticKind.Theme, 250),
  item('theme-strawberry', CosmeticKind.Theme, 250),
  item('theme-grape', CosmeticKind.Theme, 250),
  item('theme-tangerine', CosmeticKind.Theme, 250),
];

// Retired cosmetics (§17.15): no longer sold or worn, and refunded to their owners. Kept
// only so the ledger can still name them.
const RETIRED_COSMETICS: readonly CosmeticEntry[] = [
  // Avatar accessories (rama 43): drawn over the photo, so anyone could fake one with a picture.
  item('acc-sunglasses', CosmeticKind.Accessory, 200),
  item('acc-party-hat', CosmeticKind.Accessory, 200),
  item('acc-moustache', CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
  item('acc-headphones', CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
  item('acc-paper-crown', CosmeticKind.Accessory, 400, CosmeticRarity.Epic),
  item('acc-halo', CosmeticKind.Accessory, 500, CosmeticRarity.Legendary),

  item('badge-fire', CosmeticKind.Badge, 150),
  item('badge-eyes', CosmeticKind.Badge, 150),
  item('badge-popcorn', CosmeticKind.Badge, 150),
  item('badge-teapot', CosmeticKind.Badge, 150),
  item('badge-frog', CosmeticKind.Badge, 150),
  item('badge-coffee', CosmeticKind.Badge, 150),
  item('badge-chili', CosmeticKind.Badge, 150),
  item('badge-unicorn', CosmeticKind.Badge, 150),
  item('badge-nails', CosmeticKind.Badge, 150),
  item('badge-clown', CosmeticKind.Badge, 150),
  item('badge-goat', CosmeticKind.Badge, 150),
  item('badge-brain', CosmeticKind.Badge, 150),

  item('title-pro-gossip', CosmeticKind.Title, 200),
  item('title-undecided', CosmeticKind.Title, 200),
  item('title-devils-advocate', CosmeticKind.Title, 300, CosmeticRarity.Rare),
  item('title-oracle', CosmeticKind.Title, 400, CosmeticRarity.Epic),
];

// Any cosmetic the ledger may mention, retired ones included.
export function cosmeticByKey(key: string | null | undefined): CosmeticEntry | null {
  return COSMETICS.find((c) => c.key === key) ?? RETIRED_COSMETICS.find((c) => c.key === key) ?? null;
}

// ── Name colour ──────────────────────────────────────────────────────────────

// Class with light/dark variants (styles-lime.css, .vp-name--*).
export function nameColorClass(key: string | null | undefined): string {
  return key?.startsWith('color-') ? `vp-name--${key.slice('color-'.length)}` : '';
}

// ── App themes ───────────────────────────────────────────────────────────────

// data-vp-theme value on <html> (styles-lime.css) and a swatch for the shop.
export const THEME_SWATCHES: Record<string, string> = {
  'theme-ocean': '#2563eb',
  'theme-strawberry': '#e11d48',
  'theme-grape': '#9333ea',
  'theme-tangerine': '#ea580c',
};

export function themeAttr(key: string | null | undefined): string | null {
  return key && key in THEME_SWATCHES ? key.slice('theme-'.length) : null;
}

// ── Ring skins ───────────────────────────────────────────────────────────────
// A skin restyles the streak ring but still encodes the tier: the same thresholds, and the
// effects grow with it — humble and still on the first tier, then a glow, a sweeping light,
// particles and ornaments as the streak climbs.

function escalate(base: FrameLook, tier: number, gem: string): FrameLook {
  return {
    ...base,
    aura: tier >= 1 ? base.a : undefined,
    sheen: tier >= 2 ? base.b : undefined,
    sheenDur: 6.5 - tier * 0.5,
    sparks: base.hot && tier >= 1 ? 2 + tier * 2 : undefined,
    twinkles: tier >= 3 ? tier : undefined,
    topGem: tier >= 4 ? gem : undefined,
    motes: tier >= 5 ? tier + 1 : undefined,
    rays: tier >= 6 ? true : undefined,
  };
}

const SKINS: Record<string, (tier: number) => FrameLook> = {
  'ring-neon': (tier) =>
    escalate(
      {
        a: '#ff2bd6', b: '#8af7ff', core: '#ffffff',
        stops: ['#1a0533', '#ff2bd6', '#ffd6fb', '#00d9ff', '#1a0533'],
        glow: tier >= 1 ? '#ff2bd6' : undefined,
      },
      tier,
      '#00e5ff'
    ),
  'ring-pixel': (tier) =>
    escalate(
      {
        a: '#3ae374', b: '#fff200', core: '#ffffff',
        pixel: tier >= 3 ? ['#3ae374', '#17c0eb', '#fff200', '#ff4d4d'] : ['#3ae374', '#17c0eb'],
      },
      tier,
      '#ff4d4d'
    ),
  'ring-ice': (tier) =>
    escalate(
      {
        a: '#5ad1ff', b: '#e8fbff', core: '#ffffff',
        facets: 10 + tier * 2,
        facet: ['#0b3c5d', '#5ad1ff', '#e8fbff'],
        nebula: tier >= 3 ? ['#0b3c5d', '#5ad1ff', '#e0f7ff'] : undefined,
      },
      tier,
      '#bdf3ff'
    ),
  'ring-flames': (tier) =>
    escalate(
      {
        a: '#ff5a00', b: '#ffd23f', core: '#fff3d6',
        stops: ['#3a0500', '#d62400', '#ffb300', '#ff5a00', '#3a0500'],
        hot: true,
      },
      tier,
      '#ffd23f'
    ),
};

export const RING_SKIN_KEYS = Object.keys(SKINS);

export function ringSkinLook(skin: string | null | undefined, tier: StreakTierKey): FrameLook | null {
  const build = skin ? SKINS[skin] : undefined;
  return build ? build(STREAK_TIERS.findIndex((t) => t.key === tier)) : null;
}

// ── Seniority frames (free) ──────────────────────────────────────────────────
// Unlocked by time in the group: 1 month, 6 months, 1 year, 2 years (§17.10).

export const SENIORITY_STAGES: readonly { days: number; name: TranslationKey; look: FrameLook }[] = [
  {
    days: 30,
    name: 'shop.seniority.month',
    look: { a: '#b39d6f', b: '#efe3c4', core: '#ffffff', stops: ['#6b5a3e', '#c9b48a', '#efe3c4', '#b39d6f', '#6b5a3e'], notches: 12 },
  },
  {
    days: 182,
    name: 'shop.seniority.halfYear',
    look: {
      a: '#2f8f83', b: '#9fe3d5', core: '#ffffff', stops: ['#0f3d3e', '#2f8f83', '#9fe3d5', '#2a7d72', '#0f3d3e'],
      notches: 12, studs: 4, stud: ['#e6fffa', '#2a7d72'],
    },
  },
  {
    days: 365,
    name: 'shop.seniority.year',
    look: {
      a: '#c97b6b', b: '#ffd9c9', core: '#ffffff', aura: '#c97b6b',
      stops: ['#5a2a2a', '#c97b6b', '#ffd9c9', '#b8685a', '#5a2a2a'], sheen: '#fff0ea', sheenDur: 5,
      studs: 4, stud: ['#fff0ea', '#a85a4c'], topGem: '#ff8a7a', twinkles: 3,
    },
  },
  {
    days: 730,
    name: 'shop.seniority.twoYears',
    look: {
      a: '#8f84c9', b: '#efeaff', core: '#ffffff', aura: '#8f84c9',
      stops: ['#2b2540', '#8f84c9', '#efeaff', '#7b70b8', '#2b2540'], sheen: '#ffffff', sheenDur: 4,
      engraved: true, studs: 8, stud: ['#ffffff', '#6a5fa8'], topGem: '#b7a8ff', twinkles: 6,
    },
  },
];

export function seniorityDays(joinedAt: string | null | undefined, now = Date.now()): number {
  if (!joinedAt) return 0;
  return Math.max(0, Math.floor((now - new Date(joinedAt).getTime()) / 86_400_000));
}

export function seniorityStage(joinedAt: string | null | undefined) {
  const days = seniorityDays(joinedAt);
  return [...SENIORITY_STAGES].reverse().find((s) => days >= s.days) ?? null;
}
