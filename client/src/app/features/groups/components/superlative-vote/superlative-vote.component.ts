import { Component, inject, computed, input, output, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCheck } from '@ng-icons/lucide';

import { QuestionToVote } from '../../../../core/models/question.model';
import { CreateVote } from '../../../../core/models/vote.model';
import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { voteOptionClass } from '../vote/vote-option.util';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';
import { VoteRewardComponent } from '../vote-reward/vote-reward.component';

// Sentinel for "Nadie" — not a real member id, mirrors the backend's allowNobody rule (spec §9).
const NOBODY_ID = 0;

@Component({
  selector: 'app-superlative-vote',
  imports: [VoteRewardComponent, HlmButtonImports, HlmIconImports, NgIcon, UserAvatarComponent, UserNameComponent],
  providers: [provideIcons({ lucideCheck })],
  templateUrl: './superlative-vote.component.html',
})
export class SuperlativeVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();
  protected readonly t = inject(I18nService).t;

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
