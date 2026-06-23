import { Component, computed, input, linkedSignal, output } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSliderImports } from '@spartan-ng/helm/slider';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';

@Component({
  selector: 'app-scale-vote',
  imports: [HlmButtonImports, HlmSliderImports],
  templateUrl: './scale-vote.component.html',
})
export class ScaleVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly min = computed(() => this.question().rangeMin ?? 1);
  protected readonly max = computed(() => this.question().rangeMax ?? 10);

  // Resets to the midpoint whenever the question (and therefore its range) changes.
  protected readonly value = linkedSignal(() => Math.round((this.min() + this.max()) / 2));

  protected readonly targetUsername = computed(
    () => this.members().find((m) => m.id === this.question().targetUserId)?.username ?? '???'
  );

  onValueChange(value: number[]) {
    this.value.set(value[0]);
  }

  submit() {
    this.vote.emit({ numericValue: this.value() });
  }
}
