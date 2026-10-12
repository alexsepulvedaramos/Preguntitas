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
import { lucideArrowLeft, lucideFlame, lucideGift, lucideHistory, lucideRefreshCw, lucideSettings, lucideShare2, lucideSnowflake, lucideUserPlus, lucideX } from '@ng-icons/lucide';
import { toast } from '@spartan-ng/brain/sonner';
import { COIN_DAILY_CAP, CoinsEarned, RACE_WON_REWARD } from '../../../../core/models/coin.model';

import { GroupsService } from '../../services/groups.service';
import { GroupMember, GroupResponse } from '../../models/group.models';
import { DailyService } from '../../../../core/services/daily.service';
import { AuthorGuess, DailyStatus } from '../../../../core/models/daily.model';
import { MyStreak, StreakUpdate, VoteResponse } from '../../../../core/models/streak.model';
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
import { MyStreakComponent } from '../my-streak/my-streak.component';
import { TodayVotersComponent } from '../today-voters/today-voters.component';
import { AuthorGuessComponent } from '../author-guess/author-guess.component';

export const DAILY_POLL_INTERVAL_MS = 30_000;

// The streak chip turns orange when you haven't voted and the question closes within this
// window (the default of the "racha en peligro" push, rama 19).
const STREAK_DANGER_MS = 3 * 60 * 60 * 1000;

// One dismissible streak notice at a time, in this order of importance.
type StreakNotice = 'lost' | 'freeze' | 'gift' | null;

// Group-detail screen (rama 5, spec §13 row 5): shows today's voting/results state
// and the next-day selection panel from `daily/current` (§4.7). The voting UI itself
// is `app-vote` (rama 6); full results visualization lands in rama 7. The selection
// panel's picker is `app-select-question-dialog` (rama 8). Rama 36 made it calmer so the
// question stands out: the streak became a chip in the top bar, the time left a thin line
// sharing a row with who has voted, notices sit above the question one at a time, and
// "suggest a question" moved below it. After voting, "¿Quién la escribió?" sits under the
// question when it was written by a member. Rama 40: the "¡Elige tú!" race banner.
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
    MyStreakComponent,
    TodayVotersComponent,
    AuthorGuessComponent,
  ],
  providers: [
    provideIcons({ lucideArrowLeft, lucideFlame, lucideGift, lucideHistory, lucideRefreshCw, lucideSettings, lucideShare2, lucideSnowflake, lucideUserPlus, lucideX }),
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
  protected readonly raceReward = RACE_WON_REWARD;

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  protected readonly chat = viewChild<GroupChatComponent>('chat');
  private readonly selectDialog = viewChild<SelectQuestionDialogComponent>('selectDialog');
  private readonly myStreakPanel = viewChild<MyStreakComponent>('myStreak');

  public readonly group = signal<GroupResponse | null>(null);
  public readonly daily = signal<DailyStatus | null>(null);
  public readonly members = signal<GroupMember[]>([]);
  public readonly loading = signal(true);
  public readonly refreshing = signal(false);
  public readonly error = signal<TranslationKey | null>(null);

  // Ticks once a minute so the streak chip notices when the question is about to close.
  private readonly now = signal(Date.now());

  protected readonly notice = computed<StreakNotice>(() => {
    const streak = this.daily()?.myStreak;
    if (streak?.lostStreak) return 'lost';
    if (streak?.freezeSavedStreak) return 'freeze';
    if (streak?.freezeGiftFrom) return 'gift';
    return null;
  });

  protected readonly streakInDanger = computed(() => {
    const today = this.daily()?.today;
    const current = this.daily()?.myStreak.current ?? 0;
    if (!today || today.status !== 'voting' || current < 1 || !today.closesAt) return false;
    return new Date(today.closesAt).getTime() - this.now() <= STREAK_DANGER_MS;
  });

  // Streak celebration after a vote that extends the streak (rama 19).
  public readonly celebration = signal<StreakUpdate | null>(null);
  private openSelectorAfterCelebration = false;

  ngOnInit() {
    this.loadAll();
    this.startDailyPolling();
    const tick = setInterval(() => this.now.set(Date.now()), 60_000);
    this.destroyRef.onDestroy(() => clearInterval(tick));
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

  // The backend already returns the fresh QuestionResultDto (plus the streak change) from
  // POST vote, so update the local state in place instead of re-fetching daily/current.
  onVoted(response: VoteResponse) {
    const daily = this.daily();
    if (!daily) return;

    const { results, streak, coins, guess } = response;
    const me = Number(this.authService.currentUser()?.id);
    const voterIds = daily.today.voterIds ?? [];
    this.daily.set({
      ...daily,
      today: {
        ...daily.today,
        status: 'results',
        userHasVoted: true,
        results,
        guess: guess ?? null,
        voterIds: voterIds.includes(me) ? voterIds : [...voterIds, me],
      },
      myStreak: { ...daily.myStreak, current: streak.current, best: streak.best, lostStreak: null },
    });
    this.applyOwnStreak(streak.current);
    this.announceCoins(coins);
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

  // "¿Quién la escribió?" answered, skipped or narrowed down by the hint (rama 36).
  onGuessChanged(guess: AuthorGuess) {
    const daily = this.daily();
    if (!daily) return;
    this.daily.set({ ...daily, today: { ...daily.today, guess } });
  }

  // "+8 🪙" after voting (rama 34) or a right guess (rama 36), or why nothing was earned when
  // the daily cap is full.
  announceCoins(coins: CoinsEarned | undefined) {
    if (!coins) return;
    this.authService.patchCurrentUser({ coins: coins.balance });
    const capNote = coins.capReached
      ? this.t('coins.capReachedDescription', { cap: COIN_DAILY_CAP })
      : undefined;
    if (coins.earned > 0) toast(this.t('coins.earnedToast', { count: coins.earned }), { description: capNote });
    else if (coins.capReached) toast(this.t('coins.capReachedToast'), { description: capNote });
  }

  // First to choose in the race (§17.5): the turn is yours, plus the race coins.
  onRaceWon(coins: CoinsEarned) {
    toast.success(this.t('groupDetail.raceWon'));
    this.announceCoins(coins);
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

  dismissFreezeNotice() {
    this.patchMyStreak({ freezeSavedStreak: null });
    this.dailyService.dismissFreezeNotice(this.numericGroupId()).subscribe();
  }

  dismissGiftNotice() {
    this.patchMyStreak({ freezeGiftFrom: null });
    this.dailyService.dismissGiftNotice(this.numericGroupId()).subscribe();
  }

  // Repair from the "lost streak" notice: same purchase as the streak section's button.
  repairStreak() {
    this.myStreakPanel()?.repair();
  }

  // A freeze bought or a streak repaired (rama 35).
  onMyStreakChanged(fresh: MyStreak) {
    const previous = this.daily()?.myStreak.current ?? 0;
    this.patchMyStreak(fresh);
    if (fresh.current !== previous) this.applyOwnStreak(fresh.current);
  }

  private patchMyStreak(patch: Partial<MyStreak>) {
    const daily = this.daily();
    if (!daily) return;
    this.daily.set({ ...daily, myStreak: { ...daily.myStreak, ...patch } });
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