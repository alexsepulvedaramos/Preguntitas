import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Import the DTOs we created earlier
import { CreateQuestionDto, QuestionToVoteDto } from '../models/question.model';
import { CreateVoteDto } from '../models/vote.model';
import { QuestionResultDto } from '../models/result.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private apiUrl = `${environment.apiUrl}/questions`;

  constructor(private http: HttpClient) { }

  // GET /api/questions?groupId=X
  getQuestionsByGroup(groupId: number): Observable<QuestionToVoteDto[]> {
    return this.http.get<QuestionToVoteDto[]>(`${this.apiUrl}?groupId=${groupId}`);
  }

  // GET /api/questions/{id}/results
  getQuestionResults(questionId: number): Observable<QuestionResultDto> {
    return this.http.get<QuestionResultDto>(`${this.apiUrl}/${questionId}/results`);
  }

  // POST /api/questions
  createQuestion(question: CreateQuestionDto): Observable<any> {
    return this.http.post(this.apiUrl, question);
  }

  // POST /api/questions/{id}/vote
  submitVote(vote: CreateVoteDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/${vote.questionId}/vote`, vote);
  }
}