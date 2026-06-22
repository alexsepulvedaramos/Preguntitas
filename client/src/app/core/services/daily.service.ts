import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { DailyStatus, SelectionSources, SelectQuestion } from '../models/daily.model';
import { CreateVote } from '../models/vote.model';
import { QuestionResult } from '../models/result.model';
import { environment } from '../../../environments/environment';

// Daily flow within a group: current state ({ today, selection }), selection sources,
// the selector's choice for tomorrow, and voting. The voting/selection UI lands in ramas 5–8.
@Injectable({
    providedIn: 'root'
})
export class DailyService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/groups`;

    // Current daily state: today's voting/results AND the next-day selection panel (§4.7)
    getCurrent(groupId: number): Observable<DailyStatus> {
        return this.http.get<DailyStatus>(`${this.baseUrl}/${groupId}/daily/current`);
    }

    // Fetches the questions the day's selector can pick from: the group pool + the base pack
    getSelectionSources(groupId: number): Observable<SelectionSources> {
        return this.http.get<SelectionSources>(`${this.baseUrl}/${groupId}/daily/selection-sources`);
    }

    // The selector sets the next-day question (never activates it early)
    select(groupId: number, dto: SelectQuestion): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${groupId}/daily/select`, dto);
    }

    // Submit a vote on the currently-open question; returns the fresh results
    vote(groupId: number, dto: CreateVote): Observable<QuestionResult> {
        return this.http.post<QuestionResult>(`${this.baseUrl}/${groupId}/daily/vote`, dto);
    }
}
