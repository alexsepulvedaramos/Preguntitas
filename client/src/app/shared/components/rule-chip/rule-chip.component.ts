import { Component, booleanAttribute, computed, input } from '@angular/core';

import { GlyphComponent } from '../glyph/glyph.component';
import { GLYPHS, GlyphName } from '../glyph/glyphs';

// A rule of the question being answered («Tu respuesta será anónima», «Se abre el…», «No cuenta
// para la racha»), shown next to the type chip right before the answers. Each rule takes the tone
// of its own glyph so it reads at a glance; `strong` adds a border and a larger size for the rule
// someone paid for and must not be missed (anonymous votes). Copy comes projected from the caller.
@Component({
  selector: 'app-rule-chip',
  imports: [GlyphComponent],
  host: {
    '[class]': 'classes()',
  },
  template: `<app-glyph [name]="glyph()" size="xs" bare /><ng-content />`,
})
export class RuleChipComponent {
  readonly glyph = input.required<GlyphName>();
  readonly strong = input(false, { transform: booleanAttribute });

  protected readonly classes = computed(
    () =>
      `vp-glyph--${GLYPHS[this.glyph()].tone} inline-flex w-fit shrink-0 items-center gap-1 rounded-full ` +
      'bg-(--vp-glyph-bg) font-semibold text-(--vp-glyph-fg) ' +
      (this.strong()
        ? 'border border-(--vp-glyph-fg)/40 px-3 py-1 text-sm'
        : 'px-2.5 py-0.5 text-xs'),
  );
}
