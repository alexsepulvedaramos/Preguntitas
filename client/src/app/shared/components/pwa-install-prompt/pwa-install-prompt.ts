import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX } from '@ng-icons/lucide';

import { I18nService } from '../../../core/i18n/i18n.service';
import { injectHideBottomPrompts } from '../../utils/bottom-prompt-route';

@Component({
  selector: 'app-pwa-install-prompt',
  imports: [HlmButtonImports, NgIcon],
  providers: [provideIcons({ lucideX })],
  template: `
    @if (showAndroid() && !hidden()) {
      <div class="fixed bottom-[max(1rem,env(safe-area-inset-bottom))] left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl flex items-center gap-3">
          <img
            src="icons/android-chrome-192x192.png"
            alt="Vaya Preguntita"
            class="w-11 h-11 rounded-xl shrink-0"
          />
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-foreground leading-tight">{{ t('pwa.installTitle') }}</p>
            <p class="text-xs text-muted-foreground mt-0.5">{{ t('pwa.installAndroid') }}</p>
          </div>
          <div class="flex flex-col gap-1.5 shrink-0">
            <button hlmBtn size="sm" type="button" (click)="install()">
              {{ t('pwa.install') }}
            </button>
            <button hlmBtn variant="ghost" size="sm" type="button" class="text-muted-foreground" (click)="dismissAndroid()">
              {{ t('common.notNow') }}
            </button>
          </div>
        </div>
      </div>
    }

    @if (showIos() && !hidden()) {
      <div class="fixed bottom-[max(1rem,env(safe-area-inset-bottom))] left-4 right-4 z-50 sm:left-auto sm:right-4 sm:w-80">
        <div class="bg-card border border-border rounded-2xl p-4 shadow-2xl">
          <div class="flex items-center gap-3 mb-3">
            <img
              src="icons/android-chrome-192x192.png"
              alt="Vaya Preguntita"
              class="w-11 h-11 rounded-xl shrink-0"
            />
            <div class="flex-1">
              <p class="text-sm font-semibold text-foreground leading-tight">{{ t('pwa.installTitle') }}</p>
              <p class="text-xs text-muted-foreground mt-0.5">{{ t('pwa.installIos') }}</p>
            </div>
            <button
              hlmBtn
              variant="ghost"
              size="icon-sm"
              type="button"
              class="shrink-0 text-muted-foreground"
              [attr.aria-label]="t('common.close')"
              (click)="dismissIos()"
            >
              <ng-icon name="lucideX" />
            </button>
          </div>
          <ol class="space-y-2">
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">1.</span>
              <span>{{ t('pwa.iosStep1') }}</span>
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-primary-text shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"/><polyline points="16 6 12 2 8 6"/><line x1="12" y1="2" x2="12" y2="15"/>
              </svg>
            </li>
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">2.</span>
              <span class="[&_strong]:text-foreground" [innerHTML]="tHtml('pwa.iosStep2')"></span>
            </li>
            <li class="flex items-center gap-2 text-xs text-muted-foreground">
              <span class="text-foreground font-semibold shrink-0">3.</span>
              <span class="[&_strong]:text-foreground" [innerHTML]="tHtml('pwa.iosStep3')"></span>
            </li>
          </ol>
        </div>
      </div>
    }
  `,
})
export class PwaInstallPromptComponent implements OnInit {
  private readonly i18n = inject(I18nService);
  protected readonly t = this.i18n.t;
  protected readonly tHtml = this.i18n.tHtml;
  protected showAndroid = signal(false);
  protected showIos = signal(false);
  protected readonly hidden = injectHideBottomPrompts();
  private deferredPrompt: any = null;

  ngOnInit() {
    const isIos = /iphone|ipad|ipod/i.test(navigator.userAgent);
    const isInstalled = (navigator as any).standalone === true;
    if (isIos && !isInstalled) {
      this.showIos.set(true);
    }
  }

  @HostListener('window:beforeinstallprompt', ['$event'])
  onBeforeInstallPrompt(event: Event) {
    event.preventDefault();
    this.deferredPrompt = event;
    this.showAndroid.set(true);
  }

  protected dismissAndroid() {
    this.showAndroid.set(false);
  }

  protected dismissIos() {
    this.showIos.set(false);
  }

  protected async install() {
    if (!this.deferredPrompt) return;
    this.deferredPrompt.prompt();
    await this.deferredPrompt.userChoice;
    this.deferredPrompt = null;
    this.showAndroid.set(false);
  }
}
