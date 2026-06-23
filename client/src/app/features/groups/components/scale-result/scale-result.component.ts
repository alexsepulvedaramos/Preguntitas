import { Component, computed, input } from '@angular/core';

import { QuestionToVote } from '../../../../core/models/question.model';
import { OptionResult, QuestionResult } from '../../../../core/models/result.model';
import { ResultOptionBarComponent } from '../result-option-bar/result-option-bar.component';

// Scale results are a distribution over the question's fixed [rangeMin, rangeMax] range,
// not a ranking — so every value in the range renders a bin, including ones with zero votes,
// to show the full shape of the distribution.
@Component({
  selector: 'app-scale-result',
  imports: [ResultOptionBarComponent],
  templateUrl: './scale-result.component.html',
})
export class ScaleResultComponent {
  public readonly question = input.required<QuestionToVote>();
  public readonly result = input.required<QuestionResult>();

  protected readonly bins = computed<OptionResult[]>(() => {
    const min = this.question().rangeMin ?? 1;
    const max = this.question().rangeMax ?? 10;
    const byValue = new Map(this.result().results.map((r) => [r.id, r]));

    const bins: OptionResult[] = [];
    for (let value = min; value <= max; value++) {
      bins.push(
        byValue.get(value) ?? {
          id: value,
          displayText: String(value),
          targetUser: null,
          teamMembers: [],
          voteCount: 0,
          voters: [],
          percentage: 0,
        },
      );
    }
    return bins;
  });

  protected readonly average = computed(() => {
    const total = this.result().totalVotes;
    if (total === 0) return null;
    const sum = this.result().results.reduce((acc, r) => acc + r.id * r.voteCount, 0);
    return Math.round((sum / total) * 10) / 10;
  });
}
