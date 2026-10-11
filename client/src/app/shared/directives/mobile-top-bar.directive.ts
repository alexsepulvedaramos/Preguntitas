import { Directive } from '@angular/core';

/**
 * Turns a screen's own top row into the sticky app bar on mobile, where the global
 * header is hidden inside a group (rama 18). Expects to sit first in a `p-4` container:
 * the negative margins bleed it to the edges. From `sm` up it stays a plain row.
 */
@Directive({
  selector: '[appMobileTopBar]',
  standalone: true,
  host: {
    class:
      'max-sm:sticky max-sm:top-0 max-sm:z-40 max-sm:-mx-4 max-sm:-mt-4 max-sm:px-4 max-sm:py-2.5 max-sm:border-b max-sm:border-border max-sm:bg-card/90 max-sm:backdrop-blur-md',
  },
})
export class MobileTopBarDirective {}
