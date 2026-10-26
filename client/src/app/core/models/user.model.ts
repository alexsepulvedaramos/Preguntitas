
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
    // Shop cosmetics (rama 42): the equipped finish and every owned key (finishes and frame
    // colours, rama 57)
    look?: import('../constants/shop-catalog').Look | null;
    ownedCosmetics?: string[];
}