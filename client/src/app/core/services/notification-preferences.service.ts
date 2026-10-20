import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface NotificationPreferences {
  newQuestion: boolean;
  selectorTurn: boolean;
  userVoted: boolean;
  newMessage: boolean;
  streakDanger: boolean;
  streakDangerHoursBefore: number; // one of STREAK_DANGER_HOURS
  raceToSelect: boolean; // "¡Elige tú!" — nobody chose 10 minutes before T (§17.5)
  flashQuestion: boolean; // "Pregunta flash" — a member launched one (§17.11)
  monthlySummary: boolean; // "Resumen del mes" — last month's summary is ready (§17.12)
}

export const STREAK_DANGER_HOURS = [1, 2, 3, 4, 6, 8] as const;

@Injectable({ providedIn: 'root' })
export class NotificationPreferencesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/notifications/preferences`;

  get() {
    return this.http.get<NotificationPreferences>(this.url);
  }

  update(prefs: NotificationPreferences) {
    return this.http.put<NotificationPreferences>(this.url, prefs);
  }
}
