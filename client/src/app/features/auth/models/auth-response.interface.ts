export interface AuthResponse {
    userId: number;
    username: string;
    email: string;
    avatarUrl: string | null;
    preferredLanguage?: string; // the account's UI language (rama 23)
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
}