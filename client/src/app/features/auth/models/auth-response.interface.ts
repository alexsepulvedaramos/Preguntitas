export interface AuthResponse {
    userId: number;
    username: string;
    email: string;
    avatarUrl: string | null;
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
}