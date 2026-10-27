import { Component, inject, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { voteOptionClass } from '../vote/vote-option.util';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { VoteRewardComponent } from '../vote-reward/vote-reward.component';

@Component({
  selector: 'app-deathmatch-vote',
  imports: [VoteRewardComponent, HlmButtonImports, UserAvatarComponent],
  templateUrl: './deathmatch-vote.component.html',
})
export class DeathmatchVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();
  protected readonly t = inject(I18nService).t;

  protected readonly selectedIndex = signal<number | null>(null);

  protected optionClass(index: number): string {
    return voteOptionClass(this.selectedIndex() === index);
  }

  protected teamMembers(team: number[]): GroupMember[] {
    return team.map((id) => this.members().find((m) => m.id === id)).filter((m): m is GroupMember => !!m);
  }

  protected teamLabel(team: number[]): string {
    return team.map((id) => this.members().find((m) => m.id === id)?.username ?? '???').join(' + ');
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
