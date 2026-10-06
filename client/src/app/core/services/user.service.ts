import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { MemberStats, TitleOption } from '../models/streak.model';

export interface UserProfileDto {
  id: number;
  username: string;
  email: string;
  avatarUrl: string | null;
  frameColor: string | null;
  highestStreak: number;
  highestStreakEver: number;
  selectedTitleKey: string | null; // null = automatic (highest unlocked), 'none' = hidden
  title: string | null;
  unlockedTitles: TitleOption[];
}

export interface UpdateProfileRequest {
  username?: string;
  email?: string;
  avatarUrl?: string | null;
  frameColor?: string | null;
  titleKey?: string; // 'auto', 'none' or an unlocked tier key
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
