import { Component, DestroyRef, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EMPTY, forkJoin } from 'rxjs';
import { catchError, exhaustMap, filter } from 'rxjs/operators';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideClock, lucideFlame, lucideHistory, lucideRefreshCw, lucideSettings, lucideShare2, lucideUserPlus, lucideX } from '@ng-icons/lucide';
import { toast } from '@spartan-ng/brain/sonner';

import { GroupsService } from '../../services/groups.service';
import { GroupMember, GroupResponse } from '../../models/group.models';
import { DailyService } from '../../../../core/services/daily.service';
import { DailyStatus } from '../../../../core/models/daily.model';
import { StreakUpdate, VoteResponse } from '../../../../core/models/streak.model';
import { AuthService } from '../../../../core/auth/auth.service';
import { UserService } from '../../../../core/services/user.service';
import { GroupStreaksService } from '../../services/group-streaks.service';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { VoteComponent } from '../vote/vote.component';
import { ResultsComponent } from '../results/results.component';
import { CountdownComponent } from '../countdown/countdown.component';
import { SelectQuestionDialogComponent } from '../select-question-dialog/select-question-dialog.component';
import { WordStaggerPipe } from '../../../../shared/pipes/word-stagger.pipe';
import { PullToRefreshDirective } from '../../../../shared/directives/pull-to-refresh.directive';
import { MobileTopBarDirective } from '../../../../shared/directives/mobile-top-bar.directive';
import { GroupChatComponent } from '../group-chat/group-chat.component';
import { StreakCelebrationComponent } from '../streak-celebration/streak-celebration.component';
import { shareInvite } from '../../utils/invite-share';
import { visiblePolling } from '../../../../shared/utils/visible-polling';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translation.types';
import { InviteQrComponent } from '../invite-qr/invite-qr.component';

export const DAILY_POLL_INTERVAL_MS = 30_000;

// Group-detail screen (rama 5, spec §13 row 5): shows today's voting/results state
// and the next-day selection panel from `daily/current` (§4.7). The voting UI itself
// is `app-vote` (rama 6); full results visualization lands in rama 7. The selection
// panel's picker is `app-select-question-dialog` (rama 8).
@Component({
  selector: 'app-group-detail',
  imports: [
    RouterLink,
    HlmButtonImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    NgIcon,
    VoteComponent,
    ResultsComponent,
    CountdownComponent,
    SelectQuestionDialogComponent,
    WordStaggerPipe,
    PullToRefreshDirective,
    MobileTopBarDirective,
    GroupChatComponent,
    HlmAlertImports,
    StreakCelebrationComponent,
    InviteQrComponent,
  ],
  providers: [
    provideIcons({ lucideArrowLeft, lucideClock, lucideFlame, lucideHistory, lucideRefreshCw, lucideSettings, lucideShare2, lucideUserPlus, lucideX }),
  ],
  templateUrl: './group-detail.component.html',
  styleUrl: './group-detail.component.css',
})
export class GroupDetailComponent implements OnInit {
  private readonly groupsService = inject(GroupsService);
  private readonly dailyService = inject(DailyService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly groupStreaks = inject(GroupStreaksService);
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  protected readonly t = inject(I18nService).t;

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  protected readonly chat = viewChild<GroupChatComponent>('chat');
  private readonly selectDialog = viewChild<SelectQuestionDialogComponent>('selectDialog');

  public readonly group = signal<GroupResponse | null>(null);
  public readonly daily = signal<DailyStatus | null>(null);
  public readonly members = signal<GroupMember[]>([]);
  public readonly loading = signal(true);
  public readonly refreshing = signal(false);
  public readonly error = signal<TranslationKey | null>(null);

  // Streak celebration after a vote that extends the streak (rama 19).
  public readonly celebration = signal<StreakUpdate | null>(null);
  private openSelectorAfterCelebration = false;

  ngOnInit() {
    this.loadAll();
    this.startDailyPolling();
  }

  refresh() {
    this.refreshing.set(true);
    this.chat()?.reload();

    this.dailyService.getCurrent(this.numericGroupId()).subscribe({
      next: (daily) => {
        this.daily.set(daily);
        this.refreshing.set(false);
      },
      error: () => {
        this.refreshing.set(false);
      },
    });
  }

  questionTypeLabel(type: QuestionType): string {
    return this.t(QUESTION_TYPE_LABELS[type]);
  }

  questionTypeBadgeClass(type: QuestionType): string {
    return QUESTION_TYPE_BADGE_CLASS[type];
  }

  responseCountLabel(totalVotes: number): string {
    return this.t('groupDetail.votedCount', { count: totalVotes, total: this.members().length });
  }

  // The backend already returns the fresh QuestionResultDto (plus the streak change) from
  // POST vote, so update the local state in place instead of re-fetching daily/current.
  onVoted(response: VoteResponse) {
    const daily = this.daily();
    if (!daily) return;

    const { results, streak } = response;
    this.daily.set({
      ...daily,
      today: {
        ...daily.today,
        status: 'results',
        userHasVoted: true,
        results,
      },
      myStreak: { current: streak.current, best: streak.best, lostStreak: null },
    });
    this.applyOwnStreak(streak.current);
    // A newly unlocked title may become the one shown (automatic selection).
    if (streak.titleUnlocked)
      this.userService.getProfile().subscribe({ next: (p) => this.authService.patchCurrentUser({ title: p.title }) });
    // A tier change or milestone posts an automatic chat message — show it right away.
    if (streak.isTierUp || streak.milestoneLabel) this.chat()?.reload();

    // If this user is the selector for the next question, auto-open the picker —
    // after the celebration when one is shown.
    const isSelector = !!daily.selection?.isCurrentUserSelector;
    if (streak.current > streak.previous) {
      this.openSelectorAfterCelebration = isSelector;
      this.celebration.set(streak);
    } else if (isSelector) {
      setTimeout(() => this.selectDialog()?.triggerOpen(), 600);
    }
  }

  onCelebrationDismissed() {
    this.celebration.set(null);
    if (this.openSelectorAfterCelebration) {
      this.openSelectorAfterCelebration = false;
      setTimeout(() => this.selectDialog()?.triggerOpen(), 300);
    }
  }

  dismissLostStreak() {
    const daily = this.daily();
    if (!daily) return;
    this.daily.set({ ...daily, myStreak: { ...daily.myStreak, lostStreak: null } });
    this.dailyService.dismissLostStreak(this.numericGroupId()).subscribe();
  }

  // Pre-start prompt (rama 21): share the invite so the group reaches its 2nd member.
  async inviteFriend() {
    const group = this.group();
    if (!group) return;
    const result = await shareInvite(group.name, group.invitationCode, this.t);
    if (result === 'copied') toast.success(this.t('groupDetail.inviteCopied'));
    else if (result === 'failed') toast.error(this.t('groupDetail.inviteFailed'));
  }

  // Keeps the member list, the in-group frames (the crown may change hands) and the header
  // frame in sync with the new streak.
  private applyOwnStreak(current: number) {
    const members = this.members().map((m) => (m.isCurrentUser ? { ...m, currentStreak: current } : m));
    this.members.set(members);
    this.groupStreaks.set(this.numericGroupId(), members);
    this.groupsService.getGroupMembers(this.numericGroupId()).subscribe({
      next: (fresh) => {
        this.members.set(fresh);
        this.groupStreaks.set(this.numericGroupId(), fresh);
      },
    });

    const highest = this.authService.currentUser()?.highestStreak ?? 0;
    if (current > highest) this.authService.patchCurrentUser({ highestStreak: current });
  }

  // Live refresh (spec §10): while the question is open (voting or results) re-fetch
  // daily/current every 30 s, only when the page is visible. Silent: no skeleton, and the
  // result is merged without disturbing a vote in progress. exhaustMap avoids overlaps.
  private startDailyPolling() {
    visiblePolling(DAILY_POLL_INTERVAL_MS)
      .pipe(
        filter(() => {
          const status = this.daily()?.today.status;
          return !this.loading() && !this.refreshing() && (status === 'voting' || status === 'results');
        }),
        exhaustMap(() => this.dailyService.getCurrent(this.numericGroupId()).pipe(catchError(() => EMPTY))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((fresh) => this.mergeDaily(fresh));
  }

  private mergeDaily(fresh: DailyStatus) {
    const current = this.daily();
    if (!current) return;

    const sameQuestion = current.today.question?.id === fresh.today.question?.id;
    // A poll that was in flight while the user voted can come back stale: keep the local vote.
    if (sameQuestion && current.today.userHasVoted && !fresh.today.userHasVoted) return;
    if (JSON.stringify(current) === JSON.stringify(fresh)) return;

    this.daily.set({
      ...fresh,
      // Same question still waiting for a vote: keep the object so the vote form isn't reset.
      today:
        sameQuestion && fresh.today.status === 'voting' && current.today.status === 'voting'
          ? { ...fresh.today, question: current.today.question }
          : fresh.today,
    });
  }

  private loadAll() {
    this.loading.set(true);
    this.error.set(null);

    // Execute all requests concurrently and wait for them to complete before removing the skeleton
    forkJoin({
      group: this.groupsService.getGroup(this.numericGroupId()),
      daily: this.dailyService.getCurrent(this.numericGroupId()),
      members: this.groupsService.getGroupMembers(this.numericGroupId())
    }).subscribe({
      next: (res) => {
        this.group.set(res.group);
        this.daily.set(res.daily);
        this.members.set(res.members);
        this.groupStreaks.set(this.numericGroupId(), res.members);
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('groupDetail.loadError');
        this.loading.set(false);
      }
    });
  }
}