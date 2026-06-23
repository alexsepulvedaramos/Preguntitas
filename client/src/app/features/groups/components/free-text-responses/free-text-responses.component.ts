import { Component, input } from '@angular/core';

import { FreeTextResponse } from '../../../../core/models/result.model';

// Presentational list of free-text answers (OpenText answers, or a Custom Poll's "Otro"
// answers). Each row shows who wrote it and what they said.
@Component({
  selector: 'app-free-text-responses',
  imports: [],
  templateUrl: './free-text-responses.component.html',
})
export class FreeTextResponsesComponent {
  public readonly responses = input.required<FreeTextResponse[]>();
}
