import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { MonthlySummary, MonthlySummaryListItem } from '../models/monthly-summary.model';
import { environment } from '../../../environments/environment';

const SEEN_KEY = 'vp_summary_seen_';

// Monthly summaries (§17.12): the months a group has (last 12) and one month's story.
@Injectable({ providedIn: 'root' })
export class MonthlySummaryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/groups`;

  list(groupId: number): Observable<MonthlySummaryListItem[]> {
    return this.http.get<MonthlySummaryListItem[]>(`${this.baseUrl}/${groupId}/monthly-summaries`);
  }

  get(groupId: number, month: string): Observable<MonthlySummary> {
    return this.http.get<MonthlySummary>(`${this.baseUrl}/${groupId}/monthly-summaries/${month}`);
  }

  // The newest month this device has opened, so the "your summary is ready" card shows once.
  lastSeen(groupId: number): string | null {
    try {
      return localStorage.getItem(SEEN_KEY + groupId);
    } catch {
      return null;
    }
  }

  markSeen(groupId: number, month: string): void {
    try {
      const seen = this.lastSeen(groupId);
      if (!seen || month > seen) localStorage.setItem(SEEN_KEY + groupId, month);
    } catch {
      // Storage unavailable: the card just shows again next time.
    }
  }
}
