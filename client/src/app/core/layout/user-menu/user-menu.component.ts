import { Component, inject } from '@angular/core';

import { HlmDrawerImports } from '@spartan-ng/helm/drawer';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleUserRound, lucideLogOut, lucideX } from '@ng-icons/lucide';

import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    HlmDrawerImports,
    HlmButtonImports,
    NgIcon
  ],
  providers: [
    provideIcons({
      lucideCircleUserRound,
      lucideLogOut,
      lucideX
    })
  ],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  public readonly authService = inject(AuthService);

  logout(ctx: any) {
    this.authService.logout();

    ctx.close();
  }
}