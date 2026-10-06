import { Component, inject, input } from '@angular/core';

import { FreeTextResponse } from '../../../../core/models/result.model';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GroupStreaksService } from '../../services/group-streaks.service';

// Presentational list of free-text answers (OpenText answers, or a Custom Poll's "Otro"
// answers). Each answer is a speech bubble next to its author's avatar, with their streak
// frame and crown (rama 19); tapping the avatar opens the member card.
@Component({
  selector: 'app-free-text-responses',
  imports: [UserAvatarComponent],
  templateUrl: './free-text-responses.component.html',
})
export class FreeTextResponsesComponent {
  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);

  public readonly responses = input.required<FreeTextResponse[]>();

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
