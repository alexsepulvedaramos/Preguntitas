import { Component, input, model } from '@angular/core';

import { HlmCollapsibleImports } from '@spartan-ng/helm/collapsible';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronDown } from '@ng-icons/lucide';

import { GlyphComponent } from '../../../../shared/components/glyph/glyph.component';
import { GlyphName } from '../../../../shared/components/glyph/glyphs';

// A foldable section of the create-question form (rama 50): the essentials stay in view and
// secondary settings ("Más opciones") or paid extras fold away behind one row. The summary
// says what's on inside (as text, or as the icons of what's on), so nothing chosen is hidden
// while folded.
@Component({
  selector: 'app-fold-section',
  host: { class: 'block min-w-0' },
  imports: [HlmCollapsibleImports, NgIcon, GlyphComponent],
  providers: [provideIcons({ lucideChevronDown })],
  template: `
    <hlm-collapsible class="flex min-w-0 flex-col rounded-default border border-border" [expanded]="expanded()" (expandedChange)="expanded.set($event)">
      <button
        hlmCollapsibleTrigger
        type="button"
        class="flex w-full min-w-0 items-center gap-2 rounded-default px-3 py-2.5 text-left transition-colors hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <span class="text-sm font-semibold text-foreground">{{ title() }}</span>
        <!-- Open, the rows inside already show what's on -->
        @if (summaryGlyphs().length && !expanded()) {
          <span class="flex min-w-0 flex-1 items-center gap-1">
            @for (glyph of summaryGlyphs(); track glyph) {
              <app-glyph [name]="glyph" size="xs" />
            }
          </span>
        } @else if (summary()) {
          <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">· {{ summary() }}</span>
        } @else {
          <span class="flex-1"></span>
        }
        <ng-icon
          name="lucideChevronDown"
          size="16"
          aria-hidden="true"
          class="shrink-0 text-muted-foreground transition-transform"
          [class.rotate-180]="expanded()"
        />
      </button>
      <hlm-collapsible-content class="flex flex-col gap-3 px-3 pb-3">
        <ng-content />
      </hlm-collapsible-content>
    </hlm-collapsible>
  `,
})
export class FoldSectionComponent {
  public readonly title = input.required<string>();
  public readonly summary = input<string>('');
  public readonly summaryGlyphs = input<GlyphName[]>([]);
  public readonly expanded = model(false);
}
