import { Component, inject, input, output } from '@angular/core';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';

import { SuperlativeVoteComponent } from '../superlative-vote/superlative-vote.component';
import { DeathmatchVoteComponent } from '../deathmatch-vote/deathmatch-vote.component';
import { ScaleVoteComponent } from '../scale-vote/scale-vote.component';
import { SecretPairingVoteComponent } from '../secret-pairing-vote/secret-pairing-vote.component';
import { CustomPollVoteComponent } from '../custom-poll-vote/custom-poll-vote.component';
import { OpenTextVoteComponent } from '../open-text-vote/open-text-vote.component';
import { QuestionTypeChipComponent } from '../../../../shared/components/question-type-chip/question-type-chip.component';

// Presentational half of the voting screen: the question with its type badge and, based
// on the type, one of the per-type pickers. Emits the built CreateVote without sending it,
// so the real group screen (`app-vote`, which owns POST daily/vote) and the /demo
// walkthrough (rama 25, mock data) render the exact same UI.
@Component({
  selector: 'app-vote-form',
  imports: [QuestionTypeChipComponent, 
    SuperlativeVoteComponent,
    DeathmatchVoteComponent,
    ScaleVoteComponent,
    SecretPairingVoteComponent,
    CustomPollVoteComponent,
    OpenTextVoteComponent,
  ],
  templateUrl: './vote-form.component.html',
})
export class VoteFormComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  // Off when the question is already on screen (changing your vote under its results, rama 43).
  public readonly showQuestion = input(true);

  public readonly vote = output<CreateVote>();

  protected readonly QuestionType = QuestionType;
  private readonly t = inject(I18nService).t;

  questionTypeLabel(): string {
    return this.t(QUESTION_TYPE_LABELS[this.question().type]);
  }

}
