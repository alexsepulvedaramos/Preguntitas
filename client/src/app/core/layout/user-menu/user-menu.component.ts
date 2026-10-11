import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBookOpen,
  lucideCircleUserRound,
  lucideLogOut,
  lucideMegaphone,
  lucideSettings,
  lucideX,
} from '@ng-icons/lucide';

import { AuthService } from '../../auth/auth.service';
import { ReleaseNotesService } from '../../services/release-notes.service';
import { PushNotificationService } from '../../services/push-notification.service';
import { UserAvatarComponent } from '../../../shared/components/user-avatar/user-avatar.component';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    HlmDrawerImports,
    HlmBadgeImports,
    HlmButtonImports,
    NgIcon,
    UserAvatarComponent,
  ],
  providers: [
    provideIcons({
      lucideBookOpen,
      lucideCircleUserRound,
      lucideLogOut,
      lucideMegaphone,
      lucideSettings,
      lucideX,
    })
  ],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  public readonly authService = inject(AuthService);
  public readonly releaseNotes = inject(ReleaseNotesService);
  private readonly push = inject(PushNotificationService);
  private readonly router = inject(Router);

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