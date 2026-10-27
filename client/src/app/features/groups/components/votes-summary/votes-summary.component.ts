import { Component, computed, inject, input } from '@angular/core';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';

import { OptionResult } from '../../../../core/models/result.model';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { UserNameComponent } from '../../../../shared/components/user-name/user-name.component';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { I18nService } from '../../../../core/i18n/i18n.service';

// The "who voted what" dialog (spec §4.6), the same for every question type (1.32.2): put it
// inside an <hlm-dialog-content>. Every option with at least one voter, its face (the member it
// names — a one-member Deathmatch team counts as that member — or a bigger team's faces), its
// text and the names of who voted it, in the order the result shows them.
@Component({
  selector: 'app-votes-summary',
  imports: [HlmDialogImports, UserAvatarComponent, UserNameComponent],
  template: `
    <hlm-dialog-header>
      <h3 hlmDialogTitle class="font-display text-lg italic tracking-wider">
        {{ t('results.summary') }}
      </h3>
    </hlm-dialog-header>

    <div class="flex max-h-[60vh] flex-col gap-2 overflow-y-auto py-1">
      @for (row of rows(); track row.option.id) {
        @let option = row.option;
        @let person = row.person;
        <div class="flex flex-col gap-0.5 rounded-default bg-muted/50 px-4 py-3">
          <div class="flex items-center gap-2.5">
            @if (person) {
              <button
                type="button"
                class="shrink-0 rounded-full outline-none focus-visible:ring-2 focus-visible:ring-ring"
                (click)="openMemberCard(person.id)"
                [attr.aria-label]="t('results.memberStats', { name: person.username })"
              >
                <app-user-avatar
                  [avatarUrl]="person.avatarUrl"
                  [username]="person.username"
                  [frameColor]="person.frameColor"
                  [streak]="groupStreaks.streakFor(person.id)"
                  [crown]="groupStreaks.hasCrown(person.id)"
                  [look]="groupStreaks.lookFor(person.id)"
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
            [class]="person ? 'pl-12' : 'pl-1'"
          >
            @for (voter of option.voters; track voter.id) {
              <app-user-name [name]="voter.username" />
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class VotesSummaryComponent {
  public readonly results = input.required<OptionResult[]>();

  protected readonly rows = computed(() =>
    this.results()
      .filter((option) => option.voters.length > 0)
      .map((option) => ({
        option,
        person: option.targetUser ?? (option.teamMembers.length === 1 ? option.teamMembers[0] : null),
      }))
  );

  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
