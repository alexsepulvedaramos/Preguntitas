import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { QuestionService } from '../../../core/services/question.service';
import { QuestionToVoteDto } from '../../../core/models/question.model';

@Component({
  selector: 'app-question-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './question-list.component.html',
  styleUrl: './question-list.component.scss'
})

export class QuestionListComponent implements OnInit {
  questions: QuestionToVoteDto[] = [];

  constructor(private questionService: QuestionService) { }

  ngOnInit(): void {
    const groupIdToTest = 1;

    this.questionService.getQuestionsByGroup(groupIdToTest).subscribe(response => {
      this.questions = response;
      console.log(this.questions);
    });
  }
}