import { Component, input } from '@angular/core';

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
}
