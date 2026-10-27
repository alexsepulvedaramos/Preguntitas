import { Component, inject, input } from '@angular/core';

import { OptionResult } from '../../../../core/models/result.model';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';

// The "who voted what" dialog body (spec §4.6), shared by the result bars and the Deathmatch
// result (1.32.2): every option with at least one voter, its face (the member it names, or the
// team's faces in a Deathmatch), its text and the names of who voted it.
@Component({
  selector: 'app-votes-summary',
  imports: [UserAvatarComponent, UserNameComponent],
  template: `
    <div class="flex max-h-[60vh] flex-col gap-2 overflow-y-auto py-1">
      @for (option of results(); track option.id) {
        @if (option.voters.length > 0) {
          <div class="flex flex-col gap-0.5 rounded-default bg-muted/50 px-4 py-3">
            <div class="flex items-center gap-2.5">
              @if (option.targetUser) {
                <button
                  type="button"
                  class="shrink-0 rounded-full outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  (click)="openMemberCard(option.targetUser.id)"
                  [attr.aria-label]="t('results.memberStats', { name: option.targetUser.username })"
                >
                  <app-user-avatar
                    [avatarUrl]="option.targetUser.avatarUrl"
                    [username]="option.targetUser.username"
                    [frameColor]="option.targetUser.frameColor"
                    [streak]="groupStreaks.streakFor(option.targetUser.id)"
                    [crown]="groupStreaks.hasCrown(option.targetUser.id)"
                    [look]="groupStreaks.lookFor(option.targetUser.id)"
                    effects="full"
                    size="md"
                  />
                </button>
              } @else if (option.teamMembers.length > 0) {
                <div class="flex shrink-0 items-center" aria-hidden="true">
                  @for (member of option.teamMembers; track member.id; let first = $first; let i = $index) {
                    <app-user-avatar
                      [avatarUrl]="member.avatarUrl"
                      [username]="member.username"
                      [frameColor]="member.frameColor"
                      [streak]="groupStreaks.streakFor(member.id)"
                      [crown]="groupStreaks.hasCrown(member.id)"
                      [look]="groupStreaks.lookFor(member.id)"
                      effects="ring"
                      size="sm"
                      class="relative inline-block rounded-full ring-2 ring-background"
                      [class.-ml-1.5]="!first"
                      [style.z-index]="i + 1"
                    />
                  }
                </div>
              }
              <p class="text-base font-bold leading-tight text-foreground">
                {{ option.displayText }}
                <span class="text-sm font-normal text-muted-foreground">&thinsp;({{ option.voters.length }})</span>
              </p>
            </div>
            <div
              class="flex flex-col text-sm leading-snug text-muted-foreground"
              [class]="option.targetUser ? 'pl-12' : 'pl-1'"
            >
              @for (voter of option.voters; track voter.id) {
                <app-user-name [name]="voter.username" />
              }
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class VotesSummaryComponent {
  public readonly results = input.required<OptionResult[]>();

  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
