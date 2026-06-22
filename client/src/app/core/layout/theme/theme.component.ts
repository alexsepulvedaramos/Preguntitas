import { Component, inject } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { tablerCircleHalf2 } from '@ng-icons/tabler-icons';

import { ThemeService } from '../../services/theme.service';

@Component({
    selector: 'app-theme',
    imports: [HlmButtonImports, NgIcon],
    providers: [provideIcons({ tablerCircleHalf2 })],
    template: `    <button
      hlmBtn
      variant="ghost"
      class="w-9 h-9 p-0 flex items-center justify-center rounded-full hover:bg-muted transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring"
      aria-label="Cambiar tema"
      (click)="themeService.toggleTheme()"
    >
      <ng-icon hlm name="tablerCircleHalf2" class="size-6! text-foreground" />
      <span class="sr-only">Cambiar tema</span>
    </button>
    `
})
export class ThemeComponent {
    public themeService = inject(ThemeService);
}
