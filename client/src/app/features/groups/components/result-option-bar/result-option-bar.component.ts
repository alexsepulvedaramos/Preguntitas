import { Component, computed, inject, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

import { HlmDialogImports } from '@spartan-ng/helm/dialog';

import { OptionResult, Voter } from '../../../../core/models/result.model';
import { User } from '../../../../core/models/user.model';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { isHexFrame } from '../../../../core/constants/streak-tiers';
import { MemberCardService } from '../../../../core/services/member-card.service';
import { GroupStreaksService } from '../../services/group-streaks.service';

// Cycled by row index so categorical results (Custom Poll, Superlative, Secret Pairing,
// Deathmatch) get a distinct tone per option. Scale leaves `index` unset (always chart-1) —
// it's a single ordered distribution, not separate options, so varying the hue per bin
// would be misleading.
const CHART_COLORS = [
  'bg-chart-1',
  'bg-chart-2',
  'bg-chart-3',
  'bg-chart-4',
  'bg-chart-5',
  'bg-chart-6',
];

// Shared result row (rama 7, spec §13 row 7): a compact bar whose width is proportional to
// its percentage, used by every per-type results component. Tapping a row with at least one
// voter opens a dialog listing who voted for it ("who voted for what" — spec §4.6). Rows
// with no voters (e.g. an empty Scale bin) render as a static, non-interactive bar.
@Component({
  selector: 'app-result-option-bar',
  imports: [HlmDialogImports, UserAvatarComponent, NgTemplateOutlet],
  templateUrl: './result-option-bar.component.html',
})
export class ResultOptionBarComponent {
  public readonly label = input.required<string>();
  public readonly subtitle = input<string | null>(null);
  public readonly voteCount = input.required<number>();
  public readonly percentage = input.required<number>();
  public readonly barWidth = input<number | null>(null);
  public readonly voters = input.required<Voter[]>();
  public readonly allResults = input<OptionResult[]>([]);
  public readonly index = input<number>(0);
  public readonly leadUser = input<User | null>(null);

  protected readonly hasAnyVoters = computed(() =>
    this.allResults().some(r => r.voters.length > 0)
  );
  protected readonly colorClass = computed(
    () => CHART_COLORS[this.index() % CHART_COLORS.length],
  );
  protected readonly displayWidth = computed(() => this.barWidth() ?? this.percentage());
  protected readonly chartColor = computed(
    () => `var(--color-chart-${(this.index() % CHART_COLORS.length) + 1})`
  );

  // Members' streak rings inside the group (rama 19) and the member card on tap.
  protected readonly groupStreaks = inject(GroupStreaksService);
  private readonly memberCard = inject(MemberCardService);

  // When leadUser has a fixed (hex) frame colour, use it as bar fill; otherwise fall back
  // to the chart class ("streak"/"none" frames aren't colours).
  protected readonly fillColor = computed(() => {
    const frame = this.leadUser()?.frameColor;
    return isHexFrame(frame) ? frame : null;
  });

  openMemberCard(userId: number) {
    const groupId = this.groupStreaks.currentGroupId();
    if (groupId != null) this.memberCard.open(groupId, userId);
  }
}
