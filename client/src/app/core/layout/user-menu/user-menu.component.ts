import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmToggleGroupImports } from '@spartan-ng/helm/toggle-group';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBookOpen,
  lucideCircleUserRound,
  lucideLogOut,
  lucideMegaphone,
  lucideMonitor,
  lucideMoon,
  lucideSettings,
  lucideSun,
  lucideX,
} from '@ng-icons/lucide';

import { AuthService } from '../../auth/auth.service';
import { ReleaseNotesService } from '../../services/release-notes.service';
import { PushNotificationService } from '../../services/push-notification.service';
import { ThemePreference, ThemeService } from '../../services/theme.service';
import { UserAvatarComponent } from '../../../shared/components/user-avatar/user-avatar.component';
import { LanguagePickerComponent } from '../../../shared/components/language-picker/language-picker.component';
import { I18nService } from '../../i18n/i18n.service';
import { TranslationKey } from '../../i18n/translation.types';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    HlmDrawerImports,
    HlmBadgeImports,
    HlmButtonImports,
    HlmToggleGroupImports,
    NgIcon,
    UserAvatarComponent,
    LanguagePickerComponent,
  ],
  providers: [
    provideIcons({
      lucideBookOpen,
      lucideCircleUserRound,
      lucideLogOut,
      lucideMegaphone,
      lucideMonitor,
      lucideMoon,
      lucideSettings,
      lucideSun,
      lucideX,
    })
  ],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  public readonly authService = inject(AuthService);
  public readonly releaseNotes = inject(ReleaseNotesService);
  public readonly theme = inject(ThemeService);
  private readonly push = inject(PushNotificationService);
  private readonly router = inject(Router);
  protected readonly t = inject(I18nService).t;

  protected readonly themeOptions: { value: ThemePreference; label: TranslationKey; icon: string }[] = [
    { value: 'light', label: 'userMenu.themeLight', icon: 'lucideSun' },
    { value: 'dark', label: 'userMenu.themeDark', icon: 'lucideMoon' },
    { value: 'system', label: 'userMenu.themeSystem', icon: 'lucideMonitor' },
  ];

  setTheme(value: ThemePreference | ThemePreference[] | null | undefined): void {
    if (typeof value === 'string') this.theme.setPreference(value);
  }

  navigate(ctx: any, path: string): void {
    ctx.close();
    this.router.navigate([path]);
  }

  logout(ctx: any): void {
    ctx.close();
    // Detach this device's push subscription first — it needs the session still alive.
    void this.push.detachDevice().finally(() => this.authService.logout());
  }
}