import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { MemberStats, TitleOption } from '../models/streak.model';
import { CoinHistoryPage, Inventory } from '../models/coin.model';
import { Look } from '../constants/shop-catalog';

export interface UserProfileDto {
  id: number;
  username: string;
  email: string;
  avatarUrl: string | null;
  frameColor: string | null;
  highestStreak: number;
  highestStreakEver: number;
  selectedTitleKey: string | null; // null = automatic (highest unlocked), 'none' = hidden
  preferredLanguage: string;
  title: string | null;
  unlockedTitles: TitleOption[];
  coins: number;
  inventory: Inventory; // rama 45
  // Shop cosmetics (rama 42): the equipped finish and every owned key (finishes and frame
  // colours, rama 57)
  look: Look | null;
  ownedCosmetics: string[];
}

export interface UpdateProfileRequest {
  username?: string;
  email?: string;
  avatarUrl?: string | null;
  frameColor?: string | null;
  titleKey?: string; // 'auto', 'none' or an unlocked tier key
  preferredLanguage?: string;
  // Equipped finish (rama 42, rama 57): an owned key, '' to take it off, omitted = unchanged.
  // Wearing one puts the frame back on the streak ring.
  ringSkinKey?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/users`;
  private readonly http = inject(HttpClient);

  getProfile() {
    return this.http.get<UserProfileDto>(`${this.apiUrl}/me`);
  }

  // Own streak and stats in each group (profile card, rama 19)
  getMyGroupStats() {
    return this.http.get<MemberStats[]>(`${this.apiUrl}/me/group-stats`);
  }

  // Coin ledger, newest first (rama 34)
  getCoinHistory(before?: number | null) {
    const params: Record<string, number> = before ? { before } : {};
    return this.http.get<CoinHistoryPage>(`${this.apiUrl}/me/coins/history`, { params });
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.http.put<UserProfileDto>(`${this.apiUrl}/me`, request);
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.put(`${this.apiUrl}/me/password`, request);
  }

  uploadAvatar(file: Blob, contentType: string) {
    const formData = new FormData();
    formData.append('file', file, `avatar.${contentType.split('/')[1] ?? 'jpg'}`);
    return this.http.post<{ avatarUrl: string }>(`${this.apiUrl}/me/avatar`, formData);
  }
}
