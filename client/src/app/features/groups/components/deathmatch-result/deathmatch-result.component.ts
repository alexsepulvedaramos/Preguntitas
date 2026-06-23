import { Component, computed, input } from '@angular/core';

import { QuestionResult } from '../../../../core/models/result.model';
import { ResultOptionBarComponent } from '../result-option-bar/result-option-bar.component';

// One row per team (always both, even at 0 votes — it's a head-to-head comparison, hiding
// a side would look broken). The backend resolves DisplayText as "Jugador1 + Jugador2"
// (same convention as Secret Pairing) so no cross-referencing against question.teams /
// group members is needed here.
@Component({
  selector: 'app-deathmatch-result',
  imports: [ResultOptionBarComponent],
  templateUrl: './deathmatch-result.component.html',
})
export class DeathmatchResultComponent {
  public readonly result = input.required<QuestionResult>();

  protected readonly leadingLabel = computed(() => {
    const results = this.result().results;
    const maxVotes = Math.max(...results.map((r) => r.voteCount), 0);
    const leaders = results.filter((r) => r.voteCount === maxVotes);
    return maxVotes === 0 || leaders.length > 1 ? 'Empate' : leaders[0].displayText;
  });
}
