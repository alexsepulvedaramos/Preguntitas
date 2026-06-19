import { Component, inject } from '@angular/core';

import { HlmAvatarImports } from '@spartan-ng/helm/avatar';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { tablerCircleHalf2 } from '@ng-icons/tabler-icons';

import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-header',
  imports: [HlmAvatarImports, NgIcon],
  providers: [provideIcons({ tablerCircleHalf2 })],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  public themeService = inject(ThemeService);
}
