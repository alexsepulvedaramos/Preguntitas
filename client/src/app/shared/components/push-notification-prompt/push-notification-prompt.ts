import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';
import { AuthService } from '../../../core/auth/auth.service';
import { PushNotificationService } from '../../../core/services/push-notification.service';
import { I18nService } from '../../../core/i18n/i18n.service';
import { GlyphComponent } from '../glyph/glyph.component';

@Component({
  selector: 'app-push-notification-prompt',
  imports: [GlyphComponent],
  standalone: true,
  template: `
    @if (visible()) {
      <div class="fixed bottom-4 left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl">
          <div class="flex items-start gap-3">
            <app-glyph name="bell" size="md" />
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-foreground leading-tight">{{ t('pushPrompt.title') }}</p>
              <p class="text-xs text-muted-foreground mt-0.5 leading-snug">
                {{ t('pushPrompt.text') }}
              </p>
            </div>
            <button
              (click)="dismiss()"
              class="shrink-0 text-muted-foreground hover:text-foreground transition-colors p-1"
              [attr.aria-label]="t('common.close')"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none"
                stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
          <div class="flex gap-2 mt-3">
            <button
              (click)="activate()"
              [disabled]="loading()"
              class="flex-1 text-xs font-semibold bg-accent text-accent-foreground px-3 py-2 rounded-lg
                     hover:opacity-90 transition-opacity disabled:opacity-50 disabled:pointer-events-none"
            >
              {{ loading() ? t('pushPrompt.activating') : t('pushPrompt.activate') }}
            </button>
            <button
              (click)="dismiss()"
              class="text-xs text-muted-foreground px-3 py-2 rounded-lg hover:bg-muted transition-colors"
            >
              {{ t('common.notNow') }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class PushNotificationPromptComponent implements OnInit {
  private readonly pushService = inject(PushNotificationService);
  private readonly auth = inject(AuthService);
  protected readonly t = inject(I18nService).t;

  private readonly delayElapsed = signal(false);
  private readonly closed = signal(false);
  protected loading = signal(false);

  // Reactive rather than a one-off check: on a cold start the session may still be
  // restoring (token refresh against a sleeping API) when the delay elapses.
  // Only for logged-in users: registering the subscription needs an authenticated call.
  protected visible = computed(() =>
    this.delayElapsed()
    && !this.closed()
    && this.auth.isAuthenticated()
    && this.pushService.shouldShowPrompt
  );

  ngOnInit() {
    // Small delay so it doesn't compete with the PWA install prompt visually.
    setTimeout(() => this.delayElapsed.set(true), 4000);
  }

  protected async activate() {
    this.loading.set(true);
    const ok = await this.pushService.requestAndSubscribe();
    this.loading.set(false);
    if (!ok && this.pushService.permission() !== 'denied') {
      toast.error(this.t('pushPrompt.error'));
    }
    this.closed.set(true);
  }

  protected dismiss() {
    this.pushService.dismissPrompt();
    this.closed.set(true);
  }
}
