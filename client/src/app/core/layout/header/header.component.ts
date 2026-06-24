import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { UserMenuComponent } from "../user-menu/user-menu.component";
import { LogoWordmarkComponent } from "../logo/logo-wordmark.component";
import { ThemeComponent } from "../theme/theme.component";
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, UserMenuComponent, LogoWordmarkComponent, ThemeComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  public authService = inject(AuthService);
}
