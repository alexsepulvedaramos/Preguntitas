import { Component, inject, input } from '@angular/core';

import { FreeTextResponse } from '../../../../core/models/result.model';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { ReactionsComponent } from '../reactions/reactions.component';

// Presentational list of free-text answers (OpenText answers, or a Custom Poll's "Otro"
// answers). Each answer is a speech bubble next to its author's avatar, with their streak
// frame and crown (rama 19); tapping the avatar opens the member card. Members react to
// each other's answers below the bubble (rama 48).
@Component({
  selector: 'app-free-text-responses',
  imports: [UserAvatarComponent, ReactionsComponent],
  templateUrl: './free-text-responses.component.html',
})
export class FreeTextResponsesComponent {
  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  public readonly responses = input.required<FreeTextResponse[]>();

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
