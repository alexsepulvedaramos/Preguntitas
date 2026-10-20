import { Component, booleanAttribute, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';

import { GLYPHS, GLYPH_ICONS, GlyphName } from './glyphs';

export type GlyphSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

// Tile side and icon size per size. `xs` is for inline use next to text (usually `bare`).
const SIZES: Record<GlyphSize, { tile: string; icon: string }> = {
  xs: { tile: '1.25rem', icon: '0.875rem' },
  sm: { tile: '1.75rem', icon: '1rem' },
  md: { tile: '2.25rem', icon: '1.25rem' },
  lg: { tile: '3rem', icon: '1.625rem' },
  xl: { tile: '5rem', icon: '2.75rem' },
};

// One of the app's own glyphs (glyphs.ts): a Lucide icon on a soft tile in its fixed tone.
// `bare` drops the tile and keeps only the tinted icon, for inline use inside text and chips;
// `untinted` also drops the tone, so the icon takes the colour of the text around it.
// Decorative: give the surrounding text the meaning.
@Component({
  selector: 'app-glyph',
  imports: [NgIcon],
  providers: [provideIcons(GLYPH_ICONS)],
  host: {
    'aria-hidden': 'true',
    '[class]': 'classes()',
    '[style.width]': 'bare() || untinted() ? null : dims().tile',
    '[style.height]': 'bare() || untinted() ? null : dims().tile',
  },
  template: `<ng-icon [name]="glyph().icon" [size]="dims().icon" />`,
})
export class GlyphComponent {
  readonly name = input.required<GlyphName>();
  readonly size = input<GlyphSize>('md');
  readonly bare = input(false, { transform: booleanAttribute });
  readonly untinted = input(false, { transform: booleanAttribute });

  protected readonly glyph = computed(() => GLYPHS[this.name()]);
  protected readonly dims = computed(() => SIZES[this.size()]);
  protected readonly classes = computed(
    () =>
      'vp-glyph' +
      (this.untinted() ? '' : ` vp-glyph--${this.glyph().tone}`) +
      (this.bare() || this.untinted() ? ' vp-glyph--bare' : ''),
  );
}
