
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
}