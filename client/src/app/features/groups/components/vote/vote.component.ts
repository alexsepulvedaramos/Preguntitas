import { Component, inject, input, output, signal } from '@angular/core';

import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { DailyService } from '../../../../core/services/daily.service';
import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { QuestionResult } from '../../../../core/models/result.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { GroupMember } from '../../models/group.models';

import { SuperlativeVoteComponent } from '../superlative-vote/superlative-vote.component';
import { DeathmatchVoteComponent } from '../deathmatch-vote/deathmatch-vote.component';
import { ScaleVoteComponent } from '../scale-vote/scale-vote.component';
import { SecretPairingVoteComponent } from '../secret-pairing-vote/secret-pairing-vote.component';
import { CustomPollVoteComponent } from '../custom-poll-vote/custom-poll-vote.component';

const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  [QuestionType.CustomPoll]: 'Encuesta',
  [QuestionType.Superlative]: 'Superlativo',
  [QuestionType.Deathmatch]: 'Deathmatch',
  [QuestionType.Scale]: 'Escala',
  [QuestionType.SecretPairing]: 'Pareja secreta',
};

// Voting dispatcher (rama 6, spec §13 row 6): renders today's question and, based on
// its type, one of the 5 picker sub-components. It owns the single POST daily/vote
// call so error/loading handling isn't duplicated across the per-type components.
@Component({
  selector: 'app-vote',
  imports: [
    HlmBadgeImports,
    HlmSpinnerImports,
    SuperlativeVoteComponent,
    DeathmatchVoteComponent,
    ScaleVoteComponent,
    SecretPairingVoteComponent,
    CustomPollVoteComponent,
  ],
  templateUrl: './vote.component.html',
})
export class VoteComponent {
  private readonly dailyService = inject(DailyService);

  public readonly groupId = input.required<number>();
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();

  public readonly voted = output<QuestionResult>();

  public readonly submitting = signal(false);
  public readonly error = signal<string | null>(null);

  protected readonly QuestionType = QuestionType;

  questionTypeLabel(): string {
    return QUESTION_TYPE_LABELS[this.question().type];
  }

  submit(dto: CreateVote) {
    this.error.set(null);
    this.submitting.set(true);

    this.dailyService.vote(this.groupId(), dto).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.voted.emit(result);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error ?? 'No se ha podido registrar tu voto.');
      },
    });
  }
}
