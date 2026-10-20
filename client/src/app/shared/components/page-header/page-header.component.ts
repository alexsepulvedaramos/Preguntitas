import { Component, booleanAttribute, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmIconImports } from '@spartan-ng/helm/icon';

// The one sub-page header (UX audit T5): round back arrow, the title in the display face,
// optional actions on the right (projected). `inLayout` is for screens inside the main
// layout (history, settings), where it becomes the sticky app bar on mobile only; the
// stand-alone pages (shop, profile, info) get a sticky bar at every width.
// Back goes to `backLink` when set, otherwise it emits `back`.
@Component({
  selector: 'app-page-header',
  imports: [RouterLink, NgIcon, HlmButtonImports, HlmIconImports],
  providers: [provideIcons({ lucideArrowLeft })],
  host: {
    class: 'flex items-center gap-2',
    '[class]': 'inLayout() ? inLayoutClass : standaloneClass',
  },
  template: `
    @if (backLink(); as link) {
      <a hlmBtn variant="ghost" size="icon" class="shrink-0 rounded-full" [routerLink]="link" [attr.aria-label]="backLabel()">
        <ng-icon hlm name="lucideArrowLeft" class="size-5! text-foreground" />
      </a>
    } @else {
      <button hlmBtn variant="ghost" size="icon" type="button" class="shrink-0 rounded-full" [attr.aria-label]="backLabel()" (click)="back.emit()">
        <ng-icon hlm name="lucideArrowLeft" class="size-5! text-foreground" />
      </button>
    }
    <h1 class="min-w-0 flex-1 truncate font-display text-lg italic tracking-tight text-foreground">{{ title() }}</h1>
    <ng-content />
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly backLabel = input.required<string>();
  readonly backLink = input<unknown[] | string | null>(null);
  readonly inLayout = input(false, { transform: booleanAttribute });
  readonly back = output<void>();

  protected readonly standaloneClass =
    'sticky top-0 z-10 px-2 py-2 bg-background/95 backdrop-blur border-b border-border';
  // Same bleed as MobileTopBarDirective: sits first in a `p-4` container.
  protected readonly inLayoutClass =
    'max-sm:sticky max-sm:top-0 max-sm:z-40 max-sm:-mx-4 max-sm:-mt-4 max-sm:px-2 max-sm:py-1.5 max-sm:border-b max-sm:border-border max-sm:bg-card/90 max-sm:backdrop-blur-md sm:-ml-2';
}
