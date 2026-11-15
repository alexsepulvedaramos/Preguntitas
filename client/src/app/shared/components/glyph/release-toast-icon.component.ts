import { Component } from '@angular/core';

import { GlyphComponent } from './glyph.component';

// The "what's new" toast's icon: toasts take a component, and a glyph needs its name.
@Component({
  selector: 'app-release-toast-icon',
  imports: [GlyphComponent],
  template: `<app-glyph name="sparkles" size="sm" />`,
})
export class ReleaseToastIconComponent {}
