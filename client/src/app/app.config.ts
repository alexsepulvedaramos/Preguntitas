import { ApplicationConfig, provideAppInitializer, provideBrowserGlobalErrorListeners, inject, isDevMode } from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { acceptLanguageInterceptor } from './core/i18n/accept-language.interceptor';
import { I18nService } from './core/i18n/i18n.service';
import { I18nTitleStrategy } from './core/i18n/i18n-title.strategy';
import { provideServiceWorker, SwPush } from '@angular/service-worker';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // The UI language's dictionary is loaded before the first render, so no screen ever
    // flashes raw keys or the wrong language.
    provideAppInitializer(() => inject(I18nService).init()),
    provideRouter(routes, withComponentInputBinding()),
    { provide: TitleStrategy, useExisting: I18nTitleStrategy },
    provideHttpClient(
      withInterceptors([
        acceptLanguageInterceptor,
        authInterceptor
      ])
    ),
    // sw.js wraps ngsw-worker.js with our own push handling (grouping, no banners in-app).
    provideServiceWorker('sw.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    }),
    SwPush,
  ]
};
