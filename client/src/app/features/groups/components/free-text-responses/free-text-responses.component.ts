import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';

import { FreeTextResponse } from '../../../../core/models/result.model';
import { AuthService } from '../../../../core/auth/auth.service';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { OneTimeHintsService } from '../../../../core/services/one-time-hints.service';
import { LongPressDirective } from '../../../../shared/directives/long-press.directive';
import { ReactionsComponent } from '../reactions/reactions.component';

// Presentational list of free-text answers (OpenText answers, or a Custom Poll's "Otro"
// answers). Each answer is a speech bubble next to its author's avatar, with their streak
// frame and crown (rama 19); tapping the avatar opens the member card. Members react to
// each other's answers by pressing and holding the bubble (rama 48; no button per answer
// since rama 56), and a one-off line says so until they have done it once.
@Component({
  selector: 'app-free-text-responses',
  imports: [NgTemplateOutlet, UserAvatarComponent, ReactionsComponent, LongPressDirective],
  templateUrl: './free-text-responses.component.html',
})
export class FreeTextResponsesComponent {
  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  private readonly hints = inject(OneTimeHintsService);
  private readonly authService = inject(AuthService);

  public readonly responses = input.required<FreeTextResponse[]>();

  // Only where there is someone else's answer to react to.
  // The id is read from the JWT claims, so it arrives as a string: compare it as a number.
  protected readonly me = computed(() => Number(this.authService.currentUser()?.id));
  protected readonly showHint = computed(
    () =>
      this.hints.isPending('reactionHold') &&
      this.groupStreaks.currentGroupId() != null &&
      this.responses().some((r) => r.userId !== this.me()),
  );

  react(reactions: ReactionsComponent) {
    reactions.open();
    this.hints.dismiss('reactionHold');
  }

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
