import { Component, forwardRef, inject, input, output, signal } from '@angular/core';

import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { DailyService } from '../../../../core/services/daily.service';
import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { VoteResponse } from '../../../../core/models/streak.model';
import { FlashState } from '../../../../core/models/daily.model';
import { GroupMember } from '../../models/group.models';

import { VoteFormComponent } from '../vote-form/vote-form.component';
import { VoteRewardSource } from '../vote-reward/vote-reward.component';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { apiErrorMessage } from '../../../../shared/utils/api-error';

// Voting dispatcher (rama 6, spec §13 row 6): renders today's question through
// `app-vote-form` (question + per-type picker) and owns the single POST daily/vote call,
// so error/loading handling isn't duplicated across the per-type components. With a
// `flashId` it votes that flash question instead (rama 44). `reward` is shown on the vote button.
@Component({
  selector: 'app-vote',
  imports: [HlmSpinnerImports, VoteFormComponent],
  templateUrl: './vote.component.html',
  providers: [{ provide: VoteRewardSource, useExisting: forwardRef(() => VoteComponent) }],
})
export class VoteComponent implements VoteRewardSource {
  private readonly dailyService = inject(DailyService);
  protected readonly t = inject(I18nService).t;

  public readonly groupId = input.required<number>();
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly flashId = input<number | null>(null);
  // The group screen draws the title itself, in the card's header (rama 50 card layout).
  public readonly showQuestion = input(true);
  // Coins this vote pays (§17.17), for the «+N 🪙» on the button; null pays nothing.
  public readonly reward = input<number | null>(null);

  public readonly voted = output<VoteResponse>();
  public readonly flashVoted = output<FlashState>();

  public readonly submitting = signal(false);
  public readonly error = signal<string | null>(null);

  submit(dto: CreateVote) {
    this.error.set(null);
    this.submitting.set(true);

    const flashId = this.flashId();
    const onError = (err: unknown) => {
      this.submitting.set(false);
      this.error.set(apiErrorMessage(err, this.t('vote.failed')));
    };

    if (flashId != null) {
      this.dailyService.voteFlash(this.groupId(), flashId, dto).subscribe({
        next: (state) => {
          this.submitting.set(false);
          if (state) this.flashVoted.emit(state);
        },
        error: onError,
      });
      return;
    }

    this.dailyService.vote(this.groupId(), dto).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.voted.emit(response);
      },
      error: onError,
    });
  }
}
