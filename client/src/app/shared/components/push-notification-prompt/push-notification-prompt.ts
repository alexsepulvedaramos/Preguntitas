import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';
import { AuthService } from '../../../core/auth/auth.service';
import { PushNotificationService } from '../../../core/services/push-notification.service';
import { I18nService } from '../../../core/i18n/i18n.service';
import { GlyphComponent } from '../glyph/glyph.component';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX } from '@ng-icons/lucide';
import { injectHideBottomPrompts } from '../../utils/bottom-prompt-route';

@Component({
  selector: 'app-push-notification-prompt',
  imports: [GlyphComponent, HlmButtonImports, NgIcon],
  providers: [provideIcons({ lucideX })],
  template: `
    @if (visible() && !hidden()) {
      <div class="fixed bottom-[max(1rem,env(safe-area-inset-bottom))] left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
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
              hlmBtn
              variant="ghost"
              size="icon-sm"
              type="button"
              class="shrink-0 text-muted-foreground"
              [attr.aria-label]="t('common.close')"
              (click)="dismiss()"
            >
              <ng-icon name="lucideX" />
            </button>
          </div>
          <div class="flex gap-2 mt-3">
            <button
              hlmBtn
              size="sm"
              type="button"
              class="flex-1 bg-accent text-accent-foreground hover:bg-accent-hover"
              [disabled]="loading()"
              (click)="activate()"
            >
              {{ loading() ? t('pushPrompt.activating') : t('pushPrompt.activate') }}
            </button>
            <button hlmBtn variant="ghost" size="sm" type="button" class="text-muted-foreground" (click)="dismiss()">
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
  protected readonly hidden = injectHideBottomPrompts();
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
