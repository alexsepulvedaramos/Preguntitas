import { Component, computed, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';

const MAX_LENGTH = 280;

// OpenText vote: a free-text answer (spec §5.6). Mirrors the other vote sub-components'
// shape so the app-vote dispatcher treats it uniformly.
@Component({
  selector: 'app-open-text-vote',
  imports: [HlmInputImports, HlmButtonImports],
  templateUrl: './open-text-vote.component.html',
})
export class OpenTextVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly maxLength = MAX_LENGTH;
  protected readonly answer = signal('');
  protected readonly isValid = computed(() => {
    const length = this.answer().trim().length;
    return length >= 1 && length <= MAX_LENGTH;
  });

  onInput(value: string) {
    this.answer.set(value);
  }

  submit() {
    if (!this.isValid()) return;
    this.vote.emit({ freeText: this.answer().trim() });
  }
}
