import { Component, inject } from '@angular/core';

import { HlmAvatarImports } from '@spartan-ng/helm/avatar';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { tablerCircleHalf2 } from '@ng-icons/tabler-icons';
import { lucideCircleUserRound } from '@ng-icons/lucide';

import { ThemeService } from '../../services/theme.service';
import { UserMenuComponent } from "../user-menu/user-menu.component";
import { LogoComponent } from "../logo/logo.component";
import { LogoWordmarkComponent } from "../logo/logo-wordmark.component";
import { ThemeComponent } from "../theme/theme.component";
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-header',
  imports: [HlmAvatarImports, NgIcon, UserMenuComponent, LogoComponent, LogoWordmarkComponent, ThemeComponent],
  providers: [provideIcons({ tablerCircleHalf2, lucideCircleUserRound })],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  public themeService = inject(ThemeService);
  public authService = inject(AuthService);
}
