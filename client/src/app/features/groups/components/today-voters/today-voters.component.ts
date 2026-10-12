import { Component, computed, inject, input } from '@angular/core';

import { HlmPopoverImports } from '@spartan-ng/helm/popover';

import { GroupMember } from '../../models/group.models';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { MemberCardService } from '../../../../core/services/member-card.service';

const STACKED_AVATARS = 4;

// "3/6 han votado" (rama 36): who has voted today's question — never what they answered.
// A small avatar stack; tapping it lists who voted and who is still missing.
@Component({
  selector: 'app-today-voters',
  imports: [HlmPopoverImports, UserAvatarComponent],
  template: `
    <hlm-popover [sideOffset]="6" align="end">
      <button
        hlmPopoverTrigger
        type="button"
        class="flex items-center gap-1.5 rounded-full py-0.5 pl-1 pr-1.5 text-xs text-muted-foreground transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        [attr.aria-label]="t('groupDetail.votersAria', { count: voted().length, total: total() })"
      >
        @if (stacked().length > 0) {
          <span class="flex -space-x-1.5" aria-hidden="true">
            @for (m of stacked(); track m.id) {
              <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" size="xs" />
            }
          </span>
        }
        <span class="tabular-nums">{{ t('groupDetail.votersShort', { count: voted().length, total: total() }) }}</span>
      </button>

      <hlm-popover-content *hlmPopoverPortal="let ctx" class="w-64 gap-3 p-3">
        @if (voted().length > 0) {
          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted-foreground">{{ t('groupDetail.votersDone') }}</p>
            @for (m of voted(); track m.id) {
              <button
                type="button"
                class="flex items-center gap-2 rounded-md text-left text-sm text-foreground hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                (click)="openCard(m.id); ctx.close()"
              >
                <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" [frameColor]="m.frameColor" [streak]="m.currentStreak" [crown]="m.hasCrown" size="xs" />
                <span class="truncate">{{ m.username }}</span>
              </button>
            }
          </div>
        }
        @if (pending().length > 0) {
          <div class="flex flex-col gap-2">
            <p class="text-xs font-medium text-muted-foreground">{{ t('groupDetail.votersPending') }}</p>
            @for (m of pending(); track m.id) {
              <button
                type="button"
                class="flex items-center gap-2 rounded-md text-left text-sm text-muted-foreground hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                (click)="openCard(m.id); ctx.close()"
              >
                <app-user-avatar [avatarUrl]="m.avatarUrl" [username]="m.username" [frameColor]="m.frameColor" [streak]="m.currentStreak" [crown]="m.hasCrown" size="xs" />
                <span class="truncate">{{ m.username }}</span>
              </button>
            }
          </div>
        }
      </hlm-popover-content>
    </hlm-popover>
  `,
})
export class TodayVotersComponent {
  private readonly memberCard = inject(MemberCardService);
  protected readonly t = inject(I18nService).t;

  readonly groupId = input.required<number>();
  readonly members = input.required<GroupMember[]>();
  readonly voterIds = input<number[]>([]);
  // The group's size from daily/current, in case the member list is still loading.
  readonly memberCount = input(0);

  protected readonly voted = computed(() => {
    const ids = new Set(this.voterIds());
    return this.members().filter((m) => ids.has(m.id));
  });
  protected readonly pending = computed(() => {
    const ids = new Set(this.voterIds());
    return this.members().filter((m) => !ids.has(m.id));
  });
  protected readonly stacked = computed(() => this.voted().slice(0, STACKED_AVATARS));
  protected readonly total = computed(() => Math.max(this.memberCount(), this.members().length));

  openCard(userId: number): void {
    this.memberCard.open(this.groupId(), userId);
  }
}
