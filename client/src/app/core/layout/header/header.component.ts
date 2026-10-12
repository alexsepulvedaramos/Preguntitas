import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';

import { UserMenuComponent } from "../user-menu/user-menu.component";
import { LogoWordmarkComponent } from "../logo/logo-wordmark.component";
import { ThemeComponent } from "../theme/theme.component";
import { AuthService } from '../../auth/auth.service';
import { I18nService } from '../../i18n/i18n.service';
import { LanguagePickerComponent } from '../../../shared/components/language-picker/language-picker.component';

// Group screens (detail/history/settings) bring their own sticky top row
const GROUP_ROUTE = /^\/groups\/[^/?#]+/;

@Component({
  selector: 'app-header',
  imports: [RouterLink, UserMenuComponent, LogoWordmarkComponent, ThemeComponent, LanguagePickerComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css',
})
export class HeaderComponent {
  public authService = inject(AuthService);
  protected readonly t = inject(I18nService).t;
  private router = inject(Router);

  // On mobile the global bar steps aside inside a group (rama 18)
  readonly inGroup = toSignal(
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd),
      map(e => GROUP_ROUTE.test(e.urlAfterRedirects)),
    ),
    { initialValue: GROUP_ROUTE.test(this.router.url) },
  );
}
