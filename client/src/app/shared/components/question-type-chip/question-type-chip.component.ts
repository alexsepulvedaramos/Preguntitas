import { Component, computed, inject, input } from '@angular/core';

import { QuestionType } from '../../../core/enums/question-type.enum';
import { QUESTION_TYPE_GLYPH, QUESTION_TYPE_LABELS } from '../../../core/constants/question-type-labels';
import { I18nService } from '../../../core/i18n/i18n.service';
import { GlyphComponent } from '../glyph/glyph.component';

// The question-type chip used everywhere a type is named: one neutral style with the type's
// own glyph, instead of a colour per type (colour is kept for meaning, §14 visual rules).
@Component({
  selector: 'app-question-type-chip',
  imports: [GlyphComponent],
  host: {
    class:
      'inline-flex w-fit shrink-0 items-center gap-1 rounded-full bg-secondary px-2.5 py-0.5 text-xs font-semibold text-secondary-foreground',
  },
  template: `<app-glyph [name]="glyph()" size="xs" bare />{{ label() }}`,
})
export class QuestionTypeChipComponent {
  readonly type = input.required<QuestionType>();

  private readonly t = inject(I18nService).t;
  protected readonly glyph = computed(() => QUESTION_TYPE_GLYPH[this.type()]);
  protected readonly label = computed(() => this.t(QUESTION_TYPE_LABELS[this.type()]));
}
