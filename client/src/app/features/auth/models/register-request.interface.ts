export interface RegisterRequest {
    username: string;
    email: string;
    password: string;
    preferredLanguage?: string; // the device's UI language at sign-up
}