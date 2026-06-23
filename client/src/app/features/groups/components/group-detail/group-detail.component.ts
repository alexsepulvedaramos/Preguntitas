import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucideRefreshCw, lucideSparkles } from '@ng-icons/lucide';

import { GroupsService } from '../../services/groups.service';
import { GroupResponse } from '../../models/group.models';
import { DailyService } from '../../../../core/services/daily.service';
import { DailyStatus } from '../../../../core/models/daily.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';

const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  [QuestionType.CustomPoll]: 'Encuesta',
  [QuestionType.Superlative]: 'Superlativo',
  [QuestionType.Deathmatch]: 'Deathmatch',
  [QuestionType.Scale]: 'Escala',
  [QuestionType.SecretPairing]: 'Pareja secreta',
};

// Group-detail screen (rama 5, spec §13 row 5): shows today's voting/results state
// and the next-day selection panel from `daily/current` (§4.7). The voting and
// results sub-components themselves land in ramas 6–7; this screen renders the
// question prompt and a minimal summary while that work is pending.
@Component({
  selector: 'app-group-detail',
  imports: [
    RouterLink,
    DatePipe,
    HlmButtonImports,
    HlmBadgeImports,
    HlmSkeletonImports,
    HlmSpinnerImports,
    NgIcon,
  ],
  providers: [provideIcons({ lucideArrowLeft, lucideRefreshCw, lucideSparkles })],
  templateUrl: './group-detail.component.html',
  styleUrl: './group-detail.component.css',
})
export class GroupDetailComponent implements OnInit {
  private readonly groupsService = inject(GroupsService);
  private readonly dailyService = inject(DailyService);

  public readonly groupId = input.required<string>();
  private readonly numericGroupId = computed(() => Number(this.groupId()));

  public readonly group = signal<GroupResponse | null>(null);
  public readonly daily = signal<DailyStatus | null>(null);
  public readonly loading = signal(true);
  public readonly refreshing = signal(false);
  public readonly error = signal<string | null>(null);

  ngOnInit() {
    this.loadAll();
  }

  refresh() {
    this.refreshing.set(true);

    // Only refresh the daily status to keep the real-time polling strategy lightweight
    this.dailyService.getCurrent(this.numericGroupId()).subscribe({
      next: (daily) => {
        this.daily.set(daily);
        this.refreshing.set(false);
      },
      error: () => {
        // Prevent infinite loading state if the request fails
        this.refreshing.set(false);
      },
    });
  }

  questionTypeLabel(type: QuestionType): string {
    return QUESTION_TYPE_LABELS[type];
  }

  private loadAll() {
    this.loading.set(true);
    this.error.set(null);

    // Execute both requests concurrently and wait for both to complete before removing the skeleton
    forkJoin({
      group: this.groupsService.getGroup(this.numericGroupId()),
      daily: this.dailyService.getCurrent(this.numericGroupId())
    }).subscribe({
      next: (res) => {
        this.group.set(res.group);
        this.daily.set(res.daily);
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