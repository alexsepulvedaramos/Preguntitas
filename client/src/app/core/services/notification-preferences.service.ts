import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface NotificationPreferences {
  newQuestion: boolean;
  selectorTurn: boolean;
  userVoted: boolean;
  newMessage: boolean;
}

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
