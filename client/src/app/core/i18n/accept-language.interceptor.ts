import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { I18nService } from './i18n.service';

// Tells the API which language to answer error messages in: the UI language, not the
// browser's own Accept-Language (a player may have picked another one).
export const acceptLanguageInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiUrl)) return next(req);
  const lang = inject(I18nService).lang();
  return next(req.clone({ setHeaders: { 'Accept-Language': lang } }));
};
