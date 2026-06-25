import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PwaInstallPromptComponent } from './shared/components/pwa-install-prompt/pwa-install-prompt';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PwaInstallPromptComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
