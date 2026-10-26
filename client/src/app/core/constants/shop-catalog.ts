import { TranslationKey } from '../i18n/translation.types';
import { FRAME_COLOR_KEYS, FrameColorKey } from './streak-tiers';

// Shop cosmetics (rama 42, spec §17.10; rethought in rama 57, §17.15 item 8). Mirrors the
// backend Helpers/ShopCatalog.cs (keys, kinds, prices, rarity, sale windows) — keep both in
// sync. The art lives in shared/components/streak-frame/ring-finishes.ts: pure CSS/SVG, like
// the streak frames.

// Mirrors the backend CosmeticKind / CosmeticRarity — never renumber.
export enum CosmeticKind {
  RingSkin = 0, // a ring finish, drawn over the streak ring's material
  Accessory = 1, // retired (rama 43, §17.15)
  NameColor = 2, // retired (rama 57)
  Badge = 3, // retired (§17.15)
  Title = 4, // retired (§17.15)
  Theme = 5, // retired (rama 57)
  FrameColor = 6, // a plain frame colour (rama 57): "frame-<key>"
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
}

export interface CosmeticEntry {
  key: string;
  kind: CosmeticKind;
  price: number;
  rarity: CosmeticRarity;
  name: TranslationKey;
  // Only sold in a window the server's catalog gives (rama 57); owners wear it forever.
  seasonal?: boolean;
}

const item = (key: string, kind: CosmeticKind, price: number, rarity = CosmeticRarity.Common): CosmeticEntry => ({
  key,
  kind,
  price,
  rarity,
  name: `shop.items.${key}` as TranslationKey,
});

// The catalog key of a frame colour ("purple" → "frame-purple") and back.
export const frameColorItemKey = (color: FrameColorKey) => `frame-${color}`;
export function frameColorOf(key: string): FrameColorKey | null {
  const color = key.startsWith('frame-') ? key.slice('frame-'.length) : '';
  return (FRAME_COLOR_KEYS as readonly string[]).includes(color) ? (color as FrameColorKey) : null;
}

// Price steps for the finishes (§17.15 item 8): how much a finish shows and moves, and how much
// it changes from tier to tier.
export const FINISH_PRICES = [400, 800, 1500] as const;

export const COSMETICS: readonly CosmeticEntry[] = [
  item('ring-volt', CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
  item('ring-flare', CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
  item('ring-blizzard', CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
  item('ring-doodle', CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
  item('ring-orbit', CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
  item('ring-aurora', CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
  item('ring-neon', CosmeticKind.RingSkin, 1500, CosmeticRarity.Legendary),
  item('ring-arcade', CosmeticKind.RingSkin, 1500, CosmeticRarity.Legendary),

  // Seasonal (Halloween 2026): only on sale for a week; the catalog from the server says when.
  { ...item('ring-witch', CosmeticKind.RingSkin, 50), seasonal: true },

  ...FRAME_COLOR_KEYS.map((color) => item(frameColorItemKey(color), CosmeticKind.FrameColor, 200)),
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

  // Name colours and app themes (rama 57).
  ...['coral', 'tangerine', 'gold', 'mint', 'sky', 'lavender', 'pink', 'lime'].map((c) => item(`color-${c}`, CosmeticKind.NameColor, 250)),
  ...['ocean', 'strawberry', 'grape', 'tangerine'].map((t) => item(`theme-${t}`, CosmeticKind.Theme, 250)),
];

// Any cosmetic the ledger may mention, retired ones included.
export function cosmeticByKey(key: string | null | undefined): CosmeticEntry | null {
  return COSMETICS.find((c) => c.key === key) ?? RETIRED_COSMETICS.find((c) => c.key === key) ?? null;
}
