import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { MonthlySummary, MonthlySummaryListItem } from '../models/monthly-summary.model';
import { SeenService } from './seen.service';
import { environment } from '../../../environments/environment';

// Monthly summaries (§17.12): the months a group has (last 12) and one month's story.
@Injectable({ providedIn: 'root' })
export class MonthlySummaryService {
  private readonly http = inject(HttpClient);
  private readonly seen = inject(SeenService);
  private readonly baseUrl = `${environment.apiUrl}/groups`;

  list(groupId: number): Observable<MonthlySummaryListItem[]> {
    return this.http.get<MonthlySummaryListItem[]>(`${this.baseUrl}/${groupId}/monthly-summaries`);
  }

  get(groupId: number, month: string): Observable<MonthlySummary> {
    return this.http.get<MonthlySummary>(`${this.baseUrl}/${groupId}/monthly-summaries/${month}`);
  }

  // The newest month the player has opened, so the "your summary is ready" card shows once.
  // Kept per account since rama 56 (SeenService). Reads signals.
  lastSeen(groupId: number): string | null {
    return this.seen.get(`summary:${groupId}`);
  }

  // False until the account's marks have arrived: nothing is announced as new before that.
  readonly seenReady = this.seen.ready;

  markSeen(groupId: number, month: string): void {
    const seen = this.lastSeen(groupId);
    if (!seen || month > seen) this.seen.set(`summary:${groupId}`, month);
  }
}
