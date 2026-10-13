import { Component, ViewContainerRef, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PwaInstallPromptComponent } from './shared/components/pwa-install-prompt/pwa-install-prompt';
import { PushNotificationPromptComponent } from './shared/components/push-notification-prompt/push-notification-prompt';
import { AppUpdateService } from './core/services/app-update.service';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/services/theme.service';
import { themeAttr } from './core/constants/shop-catalog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PwaInstallPromptComponent, PushNotificationPromptComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly appUpdate = inject(AppUpdateService);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);

  constructor() {
    // The shop theme follows the signed-in account (rama 42). Until the profile arrives the
    // user restored from the token has no themeKey yet, so the stored one stays; signing
    // out drops it.
    let signedIn = false;
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        signedIn = true;
        if (user.themeKey !== undefined) this.themeService.setPalette(themeAttr(user.themeKey));
      } else if (signedIn) {
        signedIn = false;
        this.themeService.setPalette(null);
      }
    });

    // Update checks run from startup; the prompt (and its Helm imports) is only fetched
    // once a new version is ready — a plain dynamic import, lighter than @defer's runtime.
    effect(async () => {
      if (!this.appUpdate.ready()) return;
      const { AppUpdatePromptComponent } = await import('./shared/components/app-update-prompt/app-update-prompt');
      if (this.viewContainer.length === 0) this.viewContainer.createComponent(AppUpdatePromptComponent);
    });
  }
}
