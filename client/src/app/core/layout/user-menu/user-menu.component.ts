import { Component, computed, inject } from '@angular/core';

import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmAvatarImports } from '@spartan-ng/helm/avatar';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleUserRound, lucideLogOut, lucideUser, lucideX } from '@ng-icons/lucide';

import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    HlmDrawerImports,
    HlmButtonImports,
    HlmAvatarImports,
    NgIcon
  ],
  providers: [
    provideIcons({
      lucideCircleUserRound,
      lucideLogOut,
      lucideUser,
      lucideX
    })
  ],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  public readonly authService = inject(AuthService);

  protected readonly initials = computed(() => {
    const username = this.authService.currentUser()?.username ?? '';
    return username.slice(0, 2).toUpperCase();
  });

  logout(ctx: any) {
    this.authService.logout();
    ctx.close();
  }
}