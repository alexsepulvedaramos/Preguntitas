import { Component, computed, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

// Sentinel for "Nadie" — not a real member id, mirrors the backend's allowNobody rule (spec §9).
const NOBODY_ID = 0;

@Component({
  selector: 'app-superlative-vote',
  imports: [HlmButtonImports],
  templateUrl: './superlative-vote.component.html',
})
export class SuperlativeVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly NOBODY_ID = NOBODY_ID;
  protected readonly selected = signal<number | null>(null);

  protected readonly candidates = computed(() =>
    this.members().filter((m) => !this.question().blacklistedUserIds.includes(m.id))
  );

  protected optionClass(id: number): string {
    return voteOptionClass(this.selected() === id);
  }

  select(id: number) {
    this.selected.set(id);
  }

  submit() {
    if (this.selected() === null) return;
    this.vote.emit({ selectedTargetUserId: this.selected() });
  }
}
