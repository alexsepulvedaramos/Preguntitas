import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleUserRound, lucideLogOut, lucideSettings, lucideX } from '@ng-icons/lucide';

import { AuthService } from '../../auth/auth.service';
import { UserAvatarComponent } from '../../../shared/components/user-avatar/user-avatar.component';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    HlmDrawerImports,
    HlmButtonImports,
    NgIcon,
    UserAvatarComponent,
  ],
  providers: [
    provideIcons({
      lucideCircleUserRound,
      lucideLogOut,
      lucideSettings,
      lucideX,
    })
  ],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  public readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  goToProfile(ctx: any): void {
    ctx.close();
    this.router.navigate(['/profile']);
  }

  logout(ctx: any): void {
    this.authService.logout();
    ctx.close();
  }
}