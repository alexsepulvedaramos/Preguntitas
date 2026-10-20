import { Component, computed, inject, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';

import { OptionResult, Voter } from '../../../../core/models/result.model';
import { User } from '../../../../core/models/user.model';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';

// Shared result row (rama 7, spec §13 row 7): a compact bar whose width is proportional to
// its percentage, used by every per-type results component. Tapping a row with at least one
// voter opens a dialog listing who voted for it ("who voted for what" — spec §4.6). Rows
// with no voters (e.g. an empty Scale bin) render as a static, non-interactive bar.
@Component({
  selector: 'app-result-option-bar',
  imports: [UserNameComponent, HlmDialogImports, UserAvatarComponent, NgTemplateOutlet],
  templateUrl: './result-option-bar.component.html',
})
export class ResultOptionBarComponent {
  public readonly label = input.required<string>();
  public readonly subtitle = input<string | null>(null);
  public readonly voteCount = input.required<number>();
  public readonly percentage = input.required<number>();
  public readonly barWidth = input<number | null>(null);
  public readonly voters = input.required<Voter[]>();
  public readonly allResults = input<OptionResult[]>([]);
  public readonly leadUser = input<User | null>(null);

  protected readonly hasAnyVoters = computed(() =>
    this.allResults().some(r => r.voters.length > 0)
  );
  // One brand colour for every result (§14 visual rules): the leading option(s) get the
  // strong tint and the rest a soft one, so no option reads as "wrong" and themes recolour it.
  protected readonly isLeader = computed(() => {
    const max = Math.max(0, ...this.allResults().map(r => r.voteCount));
    return max > 0 && this.voteCount() === max;
  });
  protected readonly displayWidth = computed(() => this.barWidth() ?? this.percentage());

  // Members' streak rings inside the group (rama 19) and the member card on tap.
  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
