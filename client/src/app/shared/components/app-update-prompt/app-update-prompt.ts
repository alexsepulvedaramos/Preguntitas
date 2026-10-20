import { Component, inject, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideRefreshCw } from '@ng-icons/lucide';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { HlmSpinnerImports } from '@spartan-ng/helm/spinner';

import { AppUpdateService } from '../../../core/services/app-update.service';
import { I18nService } from '../../../core/i18n/i18n.service';

// "New version" card (release 1.6.1). The app root imports it dynamically only once
// AppUpdateService reports a version ready, so its Helm imports stay out of the initial bundle.
@Component({
  selector: 'app-update-prompt',
  imports: [NgIcon, HlmButtonImports, HlmIconImports, HlmSpinnerImports],
  providers: [provideIcons({ lucideRefreshCw })],
  template: `
    @if (appUpdate.ready()) {
      <div class="fixed top-3 left-4 right-4 z-[60] sm:left-auto sm:right-4 sm:w-80" role="status" aria-live="polite">
        <div class="flex flex-col gap-3 rounded-default border border-border bg-card p-4 shadow-2xl">
          <div class="flex items-start gap-3">
            <span class="flex size-10 shrink-0 items-center justify-center rounded-default bg-primary/10 text-primary-text">
              <ng-icon hlm name="lucideRefreshCw" size="20px" />
            </span>
            <div class="flex min-w-0 flex-1 flex-col gap-0.5">
              <p class="text-sm font-semibold leading-tight text-foreground">{{ t('appUpdate.title') }}</p>
              <p class="text-xs text-muted-foreground">{{ t('appUpdate.text') }}</p>
            </div>
          </div>
          <div class="flex justify-end gap-2">
            <button hlmBtn variant="ghost" size="sm" [disabled]="updating()" (click)="appUpdate.dismiss()">{{ t('appUpdate.later') }}</button>
            <button hlmBtn size="sm" [disabled]="updating()" (click)="update()">
              @if (updating()) {
                <hlm-spinner />
              }
              {{ t('appUpdate.update') }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AppUpdatePromptComponent {
  protected readonly appUpdate = inject(AppUpdateService);
  protected readonly updating = signal(false);
  protected readonly t = inject(I18nService).t;

  protected update(): void {
    this.updating.set(true);
    this.appUpdate.update();
  }
}
