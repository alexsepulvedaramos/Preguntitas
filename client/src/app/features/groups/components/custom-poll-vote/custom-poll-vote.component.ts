import { Component, computed, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

@Component({
  selector: 'app-custom-poll-vote',
  imports: [HlmButtonImports],
  templateUrl: './custom-poll-vote.component.html',
})
export class CustomPollVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly selected = signal<number[]>([]);

  protected readonly minSelections = computed(() => this.question().minSelections ?? 1);
  protected readonly maxSelections = computed(() => this.question().maxSelections ?? 1);
  protected readonly single = computed(() => this.maxSelections() <= 1);

  protected readonly isValid = computed(() => {
    const count = this.selected().length;
    return count >= this.minSelections() && count <= this.maxSelections();
  });

  protected optionClass(id: number): string {
    return voteOptionClass(this.selected().includes(id));
  }

  toggle(id: number) {
    if (this.single()) {
      this.selected.set([id]);
      return;
    }

    const current = this.selected();
    if (current.includes(id)) {
      this.selected.set(current.filter((x) => x !== id));
      return;
    }
    if (current.length >= this.maxSelections()) return;
    this.selected.set([...current, id]);
  }

  submit() {
    if (!this.isValid()) return;
    this.vote.emit({ selectedOptionIds: this.selected() });
  }
}
