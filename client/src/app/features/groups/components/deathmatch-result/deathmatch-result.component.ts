import { Component, computed, inject, input } from '@angular/core';

import { QuestionResult } from '../../../../core/models/result.model';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { UserAvatarComponent } from '../../../../shared/components/user-avatar/user-avatar.component';
import { GroupStreaksService } from '../../services/group-streaks.service';

// The Deathmatch result (1.32.1). Two teams: face to face, like a scoreboard — the teams side
// by side in their order, faces with frames, the share big, the names once, then the split bar
// and each side's votes. Three or four: a ranking, most voted first, with the rank (ties share
// it), faces, a thin bar, the names, the share and its votes. The clear leader gets the accent.
// Who has voted is shown under the timer, as for every question.
@Component({
  selector: 'app-deathmatch-result',
  imports: [UserAvatarComponent],
  templateUrl: './deathmatch-result.component.html',
})
export class DeathmatchResultComponent {
  public readonly result = input.required<QuestionResult>();
  protected readonly t = inject(I18nService).t;
  protected readonly groupStreaks = inject(GroupStreaksService);

  protected readonly rows = computed(() => {
    const options = [...this.result().results].sort((a, b) => b.voteCount - a.voteCount);
    const top = options[0]?.voteCount ?? 0;
    const leaders = options.filter((o) => o.voteCount === top).length;
    return options.map((option) => ({
      option,
      rank: 1 + options.filter((o) => o.voteCount > option.voteCount).length,
      leads: top > 0 && leaders === 1 && option.voteCount === top,
    }));
  });

  // Face to face keeps the teams in their order (left/right as when voting), not by votes.
  protected readonly faceToFace = computed(() => {
    const byId = new Map(this.rows().map((row) => [row.option.id, row]));
    return this.result().results.map((option) => byId.get(option.id)!);
  });
}
