import { Component, inject, input, output, signal } from '@angular/core';

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

const REQUIRED_SELECTIONS = 2;

@Component({
  selector: 'app-secret-pairing-vote',
  imports: [HlmButtonImports, HlmIconImports, NgIcon, UserAvatarComponent, UserNameComponent],
  providers: [provideIcons({ lucideCheck })],
  templateUrl: './secret-pairing-vote.component.html',
})
export class SecretPairingVoteComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly members = input.required<GroupMember[]>();
  public readonly vote = output<CreateVote>();
  protected readonly t = inject(I18nService).t;

  protected readonly selected = signal<number[]>([]);

  protected optionClass(id: number): string {
    return voteOptionClass(this.selected().includes(id));
  }

  toggle(id: number) {
    const current = this.selected();
    if (current.includes(id)) {
      this.selected.set(current.filter((x) => x !== id));
      return;
    }
    if (current.length >= REQUIRED_SELECTIONS) return;
    this.selected.set([...current, id]);
  }

  submit() {
    if (this.selected().length !== REQUIRED_SELECTIONS) return;
    this.vote.emit({ selectedTargetUserIds: this.selected() });
  }
}
