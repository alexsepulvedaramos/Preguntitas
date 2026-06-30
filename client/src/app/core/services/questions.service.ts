import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateQuestion, HistoryEntry, HistoryPage, Question, QuestionPage } from '../models/question.model';
import { QuestionResult } from '../models/result.model';
import { QuestionType } from '../enums/question-type.enum';
import { environment } from '../../../environments/environment';

// Pool of user-created questions within a group: list, create directly (without
// selecting for tomorrow), and delete. Selecting a question for tomorrow goes through
// DailyService instead (daily/select).
@Injectable({
    providedIn: 'root'
})
export class QuestionsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/groups`;

    // Unused pool questions for the group, paginated (cursor-based, newest first) and
    // optionally filtered by type. Pass `before` (last seen Question.id) for subsequent pages.
    getPool(
        groupId: number,
        opts: { type?: QuestionType; before?: number; pageSize?: number } = {},
    ): Observable<QuestionPage> {
        const params: Record<string, string> = {};
        if (opts.type !== undefined) params['type'] = String(opts.type);
        if (opts.before !== undefined) params['before'] = String(opts.before);
        if (opts.pageSize !== undefined) params['pageSize'] = String(opts.pageSize);
        return this.http.get<QuestionPage>(`${this.baseUrl}/${groupId}/questions/pool`, { params });
    }

    // Adds a question to the pool without selecting it for tomorrow
    create(groupId: number, dto: CreateQuestion): Observable<Question> {
        return this.http.post<Question>(`${this.baseUrl}/${groupId}/questions`, dto);
    }

    // Removes an unused pool question (creator or group admin only)
    delete(groupId: number, questionId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${groupId}/questions/${questionId}`);
    }

    // Paginated history list, most recent first. Pass `before` (YYYY-MM-DD) as cursor for
    // subsequent pages — omit for the first page.
    getHistory(groupId: number, before?: string): Observable<HistoryPage> {
        const params: Record<string, string> = {};
        if (before) params['before'] = before;
        return this.http.get<HistoryPage>(`${this.baseUrl}/${groupId}/history`, { params });
    }

    // Full results for a specific past date. Mirrors GET /questions?date=YYYY-MM-DD.
    getByDate(groupId: number, date: string): Observable<QuestionResult> {
        return this.http.get<QuestionResult>(`${this.baseUrl}/${groupId}/questions`, {
            params: { date },
        });
    }
}
