export interface AuthResponse {
    userId: number;
    username: string;
    email: string;
    avatarUrl: string | null;
    accessToken: string;
    accessTokenExpiresAt: Date;
    refreshToken: string;
    refreshTokenExpiresAt: Date;
}