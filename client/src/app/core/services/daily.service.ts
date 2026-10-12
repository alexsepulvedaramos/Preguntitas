import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
    AuthorHintResponse,
    DailyStatus,
    FlashState,
    GuessAuthorRequest,
    GuessAuthorResponse,
    LaunchFlashResponse,
    SelectQuestion,
    SelectResponse,
} from '../models/daily.model';
import { CreateVote } from '../models/vote.model';
import { CreateQuestion } from '../models/question.model';
import { QuestionResult } from '../models/result.model';
import { VoteResponse } from '../models/streak.model';
import { ChatMessage } from '../models/chat-message.model';
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

    // The selector sets the next-day question (never activates it early); during the race
    // (§17.5) any member may, and 409 means someone else got there first
    select(groupId: number, dto: SelectQuestion): Observable<SelectResponse> {
        return this.http.post<SelectResponse>(`${this.baseUrl}/${groupId}/daily/select`, dto);
    }

    // The selector keeps the auto-selected question as their choice (§17.4)
    confirmSelection(groupId: number): Observable<SelectResponse> {
        return this.http.post<SelectResponse>(`${this.baseUrl}/${groupId}/daily/select/confirm`, {});
    }

    // Submit a vote on the currently-open question; returns the fresh results plus the
    // voter's streak change (rama 19)
    vote(groupId: number, dto: CreateVote): Observable<VoteResponse> {
        return this.http.post<VoteResponse>(`${this.baseUrl}/${groupId}/daily/vote`, dto);
    }

    // "¿Quién la escribió?" (§17.6): guess the author of the open question, or skip
    guessAuthor(groupId: number, dto: GuessAuthorRequest): Observable<GuessAuthorResponse> {
        return this.http.post<GuessAuthorResponse>(`${this.baseUrl}/${groupId}/daily/guess`, dto);
    }

    // Author hint (§17.10): hides half of the wrong candidates; 409 when coins are short
    // useOwned: spend a hint from the inventory instead of paying (rama 45).
    buyAuthorHint(groupId: number, useOwned = false): Observable<AuthorHintResponse> {
        return this.http.post<AuthorHintResponse>(`${this.baseUrl}/${groupId}/daily/guess/hint`, {}, {
            params: useOwned ? { useOwned: true } : {},
        });
    }

    // Acknowledge the "Has perdido tu racha…" notice so it isn't shown again
    dismissLostStreak(groupId: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${groupId}/daily/streak/dismiss-lost`, {});
    }

    // Acknowledge "Tu congelación salvó tu racha…" (rama 35)
    dismissFreezeNotice(groupId: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${groupId}/daily/streak/dismiss-freeze`, {});
    }

    // Acknowledge "X te ha regalado una congelación" (rama 35)
    dismissGiftNotice(groupId: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${groupId}/daily/streak/dismiss-gift`, {});
    }

    // Buy and launch a flash question at once (rama 44, §17.11); 409 when coins are short or
    // you already launched one today in this group
    // useOwned: launch it with one from the inventory instead of paying (rama 45).
    launchFlash(groupId: number, dto: CreateQuestion, useOwned = false): Observable<LaunchFlashResponse> {
        return this.http.post<LaunchFlashResponse>(`${this.baseUrl}/${groupId}/flash`, dto, {
            params: useOwned ? { useOwned: true } : {},
        });
    }

    // Vote a flash question: the fresh state while it's open, nothing (204) when it's a late
    // vote from the history
    voteFlash(groupId: number, flashId: number, dto: CreateVote): Observable<FlashState | null> {
        return this.http.post<FlashState | null>(`${this.baseUrl}/${groupId}/flash/${flashId}/vote`, dto);
    }

    // History detail of a flash question, gated like a daily one (§17.7)
    getFlash(groupId: number, flashId: number): Observable<QuestionResult> {
        return this.http.get<QuestionResult>(`${this.baseUrl}/${groupId}/flash/${flashId}`);
    }

    // Chat messages for the currently-open daily question
    getChat(groupId: number): Observable<ChatMessage[]> {
        return this.http.get<ChatMessage[]>(`${this.baseUrl}/${groupId}/daily/chat`);
    }

    // Send a message; returns the updated message list
    sendChatMessage(groupId: number, body: string): Observable<ChatMessage[]> {
        return this.http.post<ChatMessage[]>(`${this.baseUrl}/${groupId}/daily/chat`, { body });
    }
}
