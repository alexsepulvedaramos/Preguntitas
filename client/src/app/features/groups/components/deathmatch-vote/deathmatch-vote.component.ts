import { Component, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { voteOptionClass } from '../vote/vote-option.util';

@Component({
  selector: 'app-deathmatch-vote',
  imports: [HlmButtonImports],
  templateUrl: './deathmatch-vote.component.html',
})
export class DeathmatchVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();

  protected readonly selectedIndex = signal<number | null>(null);

  protected optionClass(index: number): string {
    return voteOptionClass(this.selectedIndex() === index);
  }

  protected teamLabel(team: number[]): string {
    return team.map((id) => this.members().find((m) => m.id === id)?.username ?? '???').join(' y ');
  }

  select(index: number) {
    this.selectedIndex.set(index);
  }

  submit() {
    const index = this.selectedIndex();
    if (index === null) return;
    // Send the team's member ids verbatim so the backend's exact-team-match check (spec §9) succeeds.
    this.vote.emit({ selectedTargetUserIds: this.question().teams[index] });
  }
}
