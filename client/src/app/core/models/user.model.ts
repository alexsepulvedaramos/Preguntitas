
export interface User {
    id: number;
    username: string;
    email: string;
    avatarUrl?: string | null;
    frameColor?: string | null;
    // Highest current streak across the user's groups (ring outside a group, rama 19)
    highestStreak?: number;
    // Title shown on the profile (rama 19)
    title?: string | null;
    // Coin wallet balance (rama 34)
    coins?: number;
    // Game items held (rama 45)
    inventory?: import('./coin.model').Inventory;
    // Shop cosmetics (rama 42): what's equipped, the personal app theme, every owned key, and
    // the join date of the oldest group (seniority frame outside a group)
    look?: import('../constants/shop-catalog').Look | null;
    themeKey?: string | null;
    ownedCosmetics?: string[];
    firstJoinedAt?: string | null;
}