import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Import the DTOs we created earlier
import { CreateQuestion, QuestionToVote } from '../models/question.model';
import { CreateVote } from '../models/vote.model';
import { QuestionResult } from '../models/result.model';
import { environment } from '../../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private apiUrl = `${environment.apiUrl}/questions`;

  constructor(private http: HttpClient) { }

  // GET /api/questions?groupId=X
  getQuestionsByGroup(groupId: number): Observable<QuestionToVote[]> {
    return this.http.get<QuestionToVote[]>(`${this.apiUrl}?groupId=${groupId}`);
  }

  // GET /api/questions/{id}/results
  getQuestionResults(questionId: number): Observable<QuestionResult> {
    return this.http.get<QuestionResult>(`${this.apiUrl}/${questionId}/results`);
  }

  // POST /api/questions
  createQuestion(question: CreateQuestion): Observable<any> {
    return this.http.post(this.apiUrl, question);
  }

  // POST /api/questions/{id}/vote
  submitVote(vote: CreateVote): Observable<any> {
    return this.http.post(`${this.apiUrl}/${vote.questionId}/vote`, vote);
  }
}