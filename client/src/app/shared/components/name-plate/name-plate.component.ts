import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { PlatesDisplayService } from '../../../core/services/plates-display.service';
import { PlateContext, plateClass, plateIsLight, plateTintColor, renderPlate } from './plate-art';

// A plate (rama 60, spec §17.18): the scene behind a name in the featured places. Fills its
// positioned parent (give the parent `relative isolate` and a radius, and put the content in
// front with `relative z-[1]`); renders nothing when the key isn't a plate.
@Component({
  selector: 'app-name-plate',
  template: '',
  host: {
    'aria-hidden': 'true',
    '[class]': 'hostClass()',
    '[style.--vp-plate-tint]': 'tint()',
    '[innerHTML]': 'markup()',
  },
})
export class NamePlateComponent {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly display = inject(PlatesDisplayService);

  // A ShopCatalog plate key ("plate-neon", "plate-purple").
  readonly plate = input<string | null | undefined>(null);
  // The wearer's streak where the plate is shown: the legendaries grow with its tier.
  readonly streak = input<number | null | undefined>(null);
  readonly context = input<PlateContext>('row');

  protected readonly hostClass = computed(() => {
    const scene = plateClass(this.plate());
    if (!scene) return 'hidden';
    return `vp-plate vp-plate--${this.context()} ${scene}${plateIsLight(this.plate()) ? ' vp-plate--light' : ''}`;
  });

  protected readonly tint = computed(() => plateTintColor(this.plate()));

  // The markup is built only from our own constants (see renderPlate), never from user
  // input, so bypassing sanitization is safe and keeps the SVG and inline styles.
  protected readonly markup = computed<SafeHtml>(() =>
    this.sanitizer.bypassSecurityTrustHtml(renderPlate(this.plate(), this.streak(), this.context(), this.display.doodleStickers()))
  );
}
