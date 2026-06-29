import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PwaInstallPromptComponent } from './shared/components/pwa-install-prompt/pwa-install-prompt';
import { PushNotificationPromptComponent } from './shared/components/push-notification-prompt/push-notification-prompt';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PwaInstallPromptComponent, PushNotificationPromptComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
