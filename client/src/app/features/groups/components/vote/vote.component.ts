import { Component, inject, input, output, signal } from '@angular/core';

import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { DailyService } from '../../../../core/services/daily.service';
import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { VoteResponse } from '../../../../core/models/streak.model';
import { GroupMember } from '../../models/group.models';

import { VoteFormComponent } from '../vote-form/vote-form.component';

// Voting dispatcher (rama 6, spec §13 row 6): renders today's question through
// `app-vote-form` (question + per-type picker) and owns the single POST daily/vote call,
// so error/loading handling isn't duplicated across the per-type components.
@Component({
  selector: 'app-vote',
  imports: [HlmSpinnerImports, VoteFormComponent],
  templateUrl: './vote.component.html',
})
export class VoteComponent {
  private readonly dailyService = inject(DailyService);

  public readonly groupId = input.required<number>();
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();

  public readonly voted = output<VoteResponse>();

  public readonly submitting = signal(false);
  public readonly error = signal<string | null>(null);

  submit(dto: CreateVote) {
    this.error.set(null);
    this.submitting.set(true);

    this.dailyService.vote(this.groupId(), dto).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.voted.emit(response);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error ?? 'No se ha podido registrar tu voto.');
      },
    });
  }
}
