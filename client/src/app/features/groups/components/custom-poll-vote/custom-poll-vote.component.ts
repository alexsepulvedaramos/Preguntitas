import { Component, computed, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

@Component({
  selector: 'app-custom-poll-vote',
  imports: [HlmButtonImports, HlmInputImports],
  templateUrl: './custom-poll-vote.component.html',
})
export class CustomPollVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly selected = signal<number[]>([]);
  // "Otro" free-text answer state (only relevant when question().allowOther).
  protected readonly otherSelected = signal(false);
  protected readonly otherText = signal('');

  protected readonly minSelections = computed(() => this.question().minSelections ?? 1);
  protected readonly maxSelections = computed(() => this.question().maxSelections ?? 1);
  protected readonly single = computed(() => this.maxSelections() <= 1);

  // Total picks = selected options + an optional "Otro" answer.
  protected readonly totalSelected = computed(
    () => this.selected().length + (this.otherSelected() ? 1 : 0)
  );

  protected readonly isValid = computed(() => {
    if (this.otherSelected() && this.otherText().trim().length === 0) return false;
    const count = this.totalSelected();
    return count >= this.minSelections() && count <= this.maxSelections();
  });

  protected optionClass(id: number): string {
    return voteOptionClass(this.selected().includes(id));
  }

  protected otherClass(): string {
    return voteOptionClass(this.otherSelected());
  }

  toggle(id: number) {
    if (this.single()) {
      this.otherSelected.set(false);
      this.selected.set([id]);
      return;
    }

    const current = this.selected();
    if (current.includes(id)) {
      this.selected.set(current.filter((x) => x !== id));
      return;
    }
    if (this.totalSelected() >= this.maxSelections()) return;
    this.selected.set([...current, id]);
  }

  toggleOther() {
    if (this.otherSelected()) {
      this.otherSelected.set(false);
      return;
    }

    if (this.single()) {
      this.selected.set([]);
      this.otherSelected.set(true);
      return;
    }
    if (this.totalSelected() >= this.maxSelections()) return;
    this.otherSelected.set(true);
  }

  onOtherInput(value: string) {
    this.otherText.set(value);
  }

  submit() {
    if (!this.isValid()) return;
    this.vote.emit({
      selectedOptionIds: this.selected().length > 0 ? this.selected() : null,
      freeText: this.otherSelected() ? this.otherText().trim() : null,
    });
  }
}
