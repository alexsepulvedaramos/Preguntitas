import { Component, OnInit, computed, inject, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideClock, lucideHistory, lucideRefreshCw, lucideSettings } from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { GroupMember, GroupResponse } from '../../models/group.models';
import { DailyService } from '../../../../core/services/daily.service';
import { DailyStatus } from '../../../../core/models/daily.model';
import { QuestionResult } from '../../../../core/models/result.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';
import { QUESTION_TYPE_LABELS } from '../../../../core/constants/question-type-labels';
import { QUESTION_TYPE_BADGE_CLASS } from '../../../../core/constants/question-type-colors';
import { VoteComponent } from '../vote/vote.component';
import { ResultsComponent } from '../results/results.component';
import { CountdownComponent } from '../countdown/countdown.component';
import { SelectQuestionDialogComponent } from '../select-question-dialog/select-question-dialog.component';
import { WordStaggerPipe } from '../../../../shared/pipes/word-stagger.pipe';
import { PullToRefreshDirective } from '../../../../shared/directives/pull-to-refresh.directive';
import { GroupChatComponent } from '../group-chat/group-chat.component';

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
    GroupChatComponent,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideClock, lucideHistory, lucideRefreshCw, lucideSettings })],
  templateUrl: './group-detail.component.html',
  styleUrl: './group-detail.component.css',
})
export class GroupDetailComponent implements OnInit {
  private readonly groupsService = inject(GroupsService);
  private readonly dailyService = inject(DailyService);

  public readonly groupId = input.required<string>();
  public readonly numericGroupId = computed(() => Number(this.groupId()));

  protected readonly chat = viewChild<GroupChatComponent>('chat');

  public readonly group = signal<GroupResponse | null>(null);
  public readonly daily = signal<DailyStatus | null>(null);
  public readonly members = signal<GroupMember[]>([]);
  public readonly loading = signal(true);
  public readonly refreshing = signal(false);
  public readonly error = signal<string | null>(null);

  ngOnInit() {
    this.loadAll();
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
    return QUESTION_TYPE_LABELS[type];
  }

  questionTypeBadgeClass(type: QuestionType): string {
    return QUESTION_TYPE_BADGE_CLASS[type];
  }

  responseCountLabel(totalVotes: number): string {
    const verb = totalVotes === 1 ? 'Ha' : 'Han';
    return `${verb} votado ${totalVotes} de ${this.members().length}`;
  }

  // The backend already returns the fresh QuestionResultDto from POST vote, so update
  // the local state in place instead of re-fetching daily/current.
  onVoted(result: QuestionResult) {
    const daily = this.daily();
    if (!daily) return;

    this.daily.set({
      ...daily,
      today: {
        ...daily.today,
        status: 'results',
        userHasVoted: true,
        results: result,
      },
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
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('No se ha podido cargar la información del grupo o la pregunta del día.');
        this.loading.set(false);
      }
    });
  }
}