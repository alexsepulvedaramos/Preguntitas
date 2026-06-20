import { Component, inject } from '@angular/core';

import { HlmAvatarImports } from '@spartan-ng/helm/avatar';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { tablerCircleHalf2 } from '@ng-icons/tabler-icons';

import { ThemeService } from '../../services/theme.service';

@Component({
    selector: 'app-theme',
    imports: [HlmAvatarImports, NgIcon],
    providers: [provideIcons({ tablerCircleHalf2 })],
    template: `    <button
      hlmBtn
      variant="ghost"
      class="w-9 h-9 p-0 flex items-center justify-center"
      (click)="themeService.toggleTheme()"
    >
      <ng-icon hlm name="tablerCircleHalf2" class="size-6! text-foreground" />
      <span class="sr-only">Toggle theme</span>
    </button>
    `
})
export class ThemeComponent {
    public themeService = inject(ThemeService);
}
