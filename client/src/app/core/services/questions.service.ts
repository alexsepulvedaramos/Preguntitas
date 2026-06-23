import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateQuestion, Question } from '../models/question.model';
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

    // Unused pool questions for the group, newest first
    getPool(groupId: number): Observable<Question[]> {
        return this.http.get<Question[]>(`${this.baseUrl}/${groupId}/questions/pool`);
    }

    // Adds a question to the pool without selecting it for tomorrow
    create(groupId: number, dto: CreateQuestion): Observable<Question> {
        return this.http.post<Question>(`${this.baseUrl}/${groupId}/questions`, dto);
    }

    // Removes an unused pool question (creator or group admin only)
    delete(groupId: number, questionId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${groupId}/questions/${questionId}`);
    }
}
