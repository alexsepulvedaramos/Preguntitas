import { Component, computed, input } from '@angular/core';

import { QuestionToVote } from '../../../../core/models/question.model';
import { QuestionResult } from '../../../../core/models/result.model';
import { QuestionType } from '../../../../core/enums/question-type.enum';

import { ResultOptionBarComponent } from '../result-option-bar/result-option-bar.component';
import { ScaleResultComponent } from '../scale-result/scale-result.component';
import { DeathmatchResultComponent } from '../deathmatch-result/deathmatch-result.component';
import { FreeTextResponsesComponent } from '../free-text-responses/free-text-responses.component';

// Results dispatcher (rama 7, spec §13 row 7): mirrors the voting dispatcher (rama 6,
// `app-vote`). Custom Poll, Superlative and Secret Pairing share the exact same shape
// (a list of already-sorted, already-aggregated OptionResultDto rows) so they're rendered
// directly here; only Scale (full numeric distribution) and Deathmatch (team identity)
// need dedicated sub-components.
@Component({
  selector: 'app-results',
  imports: [
    ResultOptionBarComponent,
    ScaleResultComponent,
    DeathmatchResultComponent,
    FreeTextResponsesComponent,
  ],
  templateUrl: './results.component.html',
})
export class ResultsComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly result = input.required<QuestionResult>();

  protected readonly QuestionType = QuestionType;

  // Normalizes bar widths: max-vote option → 100%, rest proportional.
  // If all tied (including zero-vote tie), everyone gets 100% so content is readable.
  protected readonly barWidths = computed(() => {
    const results = this.result().results;
    if (!results.length) return [] as number[];
    const maxVotes = Math.max(...results.map(r => r.voteCount));
    if (maxVotes === 0) return results.map(() => 0);
    return results.map(r => Math.round((r.voteCount / maxVotes) * 100));
  });

}
