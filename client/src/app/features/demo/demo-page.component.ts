import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowRight, lucideCheck, lucideClock, lucideRotateCcw } from '@ng-icons/lucide';
import { catchError, of } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmProgressImports } from '@spartan-ng/helm/progress';

import { AuthService } from '../../core/auth/auth.service';
import { QUESTION_TYPE_BADGE_CLASS } from '../../core/constants/question-type-colors';
import { QUESTION_TYPE_LABELS } from '../../core/constants/question-type-labels';
import { FRAME_STREAK, STREAK_MATERIAL_KEYS } from '../../core/constants/streak-tiers';
import { I18nService } from '../../core/i18n/i18n.service';
import { LanguagePickerComponent } from '../../shared/components/language-picker/language-picker.component';
import { LogoWordmarkComponent } from '../../core/layout/logo/logo-wordmark.component';
import { ThemeComponent } from '../../core/layout/theme/theme.component';
import { QuestionResult } from '../../core/models/result.model';
import { CreateVote } from '../../core/models/vote.model';
import { MemberCardService } from '../../core/services/member-card.service';
import { UserAvatarComponent } from '../../shared/components/user-avatar/user-avatar.component';
import { WordStaggerPipe } from '../../shared/pipes/word-stagger.pipe';
import { CountdownComponent } from '../groups/components/countdown/countdown.component';
import { ResultsComponent } from '../groups/components/results/results.component';
import { StreakCelebrationComponent } from '../groups/components/streak-celebration/streak-celebration.component';
import { VoteFormComponent } from '../groups/components/vote-form/vote-form.component';
import { voteOptionClass } from '../groups/components/vote/vote-option.util';
import { GroupStreaksService } from '../groups/services/group-streaks.service';
import {
  DEMO_DAILY_HOUR,
  DEMO_FRIEND_COUNT,
  DEMO_FRIENDS_VOTING_VISITOR,
  DEMO_GROUP_ID,
  DEMO_LOST_STREAK,
  DEMO_NICKNAME_MAX_LENGTH,
  DEMO_PICKS,
  DEMO_QUESTION,
  DEMO_STEPS,
  DEMO_TEASED_FRAMES,
  DEMO_VISITOR_ID,
  DemoStep,
  demoMembers,
  demoResult,
  demoStreakUpdate,
} from './demo-content';

// Public /demo walkthrough (rama 25): one simulated day in a fictional group, built from
// the real voting, results, avatar and celebration components fed with mock data — no
// account, no API calls. Open to guests and signed-in users alike.
@Component({
  selector: 'app-demo-page',
  imports: [
    FormsModule,
    RouterLink,
    NgIcon,
    HlmButtonImports,
    HlmCardImports,
    HlmFieldImports,
    HlmInputImports,
    HlmProgressImports,
    LogoWordmarkComponent,
    ThemeComponent,
    LanguagePickerComponent,
    UserAvatarComponent,
    WordStaggerPipe,
    CountdownComponent,
    VoteFormComponent,
    ResultsComponent,
    StreakCelebrationComponent,
  ],
  // Demo-scoped instances: result bars read members' streaks/crown from GroupStreaksService
  // and open the member card through MemberCardService. Local copies keep the fictional
  // group out of the app-wide caches, and nothing renders the card here, so taps are inert.
  providers: [
    GroupStreaksService,
    MemberCardService,
    provideIcons({ lucideArrowRight, lucideCheck, lucideClock, lucideRotateCcw }),
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './demo-page.component.html',
  styles: `
    .vp-demo-step {
      animation: vp-demo-step-in 0.35s cubic-bezier(0.16, 1, 0.3, 1) both;
    }
    @keyframes vp-demo-step-in {
      from { opacity: 0; transform: translateY(8px); }
    }
    @media (prefers-reduced-motion: reduce) {
      .vp-demo-step { animation: none; }
    }
  `,
})
export class DemoPageComponent {
  private readonly groupStreaks = inject(GroupStreaksService);
  private readonly authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  protected readonly groupName = computed(() => this.t('demo.groupName'));
  protected readonly question = computed(() => ({ ...DEMO_QUESTION, text: this.t('demo.question') }));
  protected readonly picks = DEMO_PICKS;
  protected readonly teasedFrames = computed(() =>
    DEMO_TEASED_FRAMES.map((frame) => {
      const material = this.t(STREAK_MATERIAL_KEYS[frame.key]);
      return { ...frame, username: material, label: `${material} · ${this.t('common.days', { count: frame.streak })}` };
    })
  );
  protected readonly jumpDays = [1, 2, 3];
  protected readonly dailyTime = `${DEMO_DAILY_HOUR}:00`;
  protected readonly lostStreak = DEMO_LOST_STREAK;
  protected readonly nicknameMaxLength = DEMO_NICKNAME_MAX_LENGTH;
  protected readonly frameStreak = FRAME_STREAK;
  protected readonly typeLabels = QUESTION_TYPE_LABELS;
  protected readonly typeBadge = QUESTION_TYPE_BADGE_CLASS;
  protected readonly optionClass = voteOptionClass;

  // Same check the guards run; the header and the final CTA adapt to it.
  protected readonly signedIn = toSignal(
    this.authService.verifySession().pipe(catchError(() => of(false))),
    { initialValue: false }
  );

  protected readonly step = signal<DemoStep>('intro');
  protected readonly nicknameDraft = signal('');
  protected readonly result = signal<QuestionResult | null>(null);
  protected readonly pickedId = signal<number | null>(null);
  protected readonly celebrating = signal(false);
  protected readonly streakUpdate = computed(() => demoStreakUpdate(this.t));

  // Countdown to the group's next daily time, like the real group screen.
  protected readonly closesAt = nextDailyTime(DEMO_DAILY_HOUR);

  private readonly customNickname = computed(() => this.nicknameDraft().trim().slice(0, DEMO_NICKNAME_MAX_LENGTH));
  protected readonly nickname = computed(() => this.customNickname() || this.t('common.you'));
  // The default "Tú" already says who it is; a chosen name gets a "(tú)" next to it.
  protected readonly hasCustomNickname = computed(() => this.customNickname() !== '');

  protected readonly membersBefore = computed(() => demoMembers(this.nickname(), 'before'));
  protected readonly membersAfter = computed(() =>
    demoMembers(this.nickname(), 'after').sort((a, b) => b.currentStreak - a.currentStreak)
  );

  protected readonly friends = computed(() => this.membersBefore().filter((m) => m.id !== DEMO_VISITOR_ID));

  protected readonly progress = computed(() =>
    Math.round((DEMO_STEPS.indexOf(this.step()) / (DEMO_STEPS.length - 1)) * 100)
  );

  protected readonly votedLabel = computed(() =>
    this.t('demo.results.voted', { voted: this.result()?.totalVotes ?? 0, total: this.membersBefore().length })
  );

  protected readonly punchline = computed(() =>
    DEMO_FRIENDS_VOTING_VISITOR === DEMO_FRIEND_COUNT
      ? this.t('demo.results.punchlineAll', { total: DEMO_FRIEND_COUNT })
      : this.t('demo.results.punchlineSome', { count: DEMO_FRIENDS_VOTING_VISITOR, total: DEMO_FRIEND_COUNT })
  );

  start(): void {
    this.groupStreaks.set(DEMO_GROUP_ID, this.membersBefore());
    this.go('vote');
  }

  onVote(vote: CreateVote): void {
    const target = vote.selectedTargetUserId;
    if (target == null) return;
    this.result.set(demoResult(this.question(), this.membersBefore(), target));
    this.go('results');
  }

  pick(id: number): void {
    this.pickedId.set(id);
  }

  showStreak(): void {
    this.groupStreaks.set(DEMO_GROUP_ID, this.membersAfter());
    this.celebrating.set(true);
  }

  onCelebrationDismissed(): void {
    this.celebrating.set(false);
    this.go('final');
  }

  restart(): void {
    this.result.set(null);
    this.pickedId.set(null);
    this.go('intro');
  }

  go(step: DemoStep): void {
    this.step.set(step);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}

// Next occurrence of `hour`:00 local time, as an ISO string for CountdownComponent.
function nextDailyTime(hour: number): string {
  const next = new Date();
  next.setHours(hour, 0, 0, 0);
  if (next.getTime() <= Date.now()) next.setDate(next.getDate() + 1);
  return next.toISOString();
}
