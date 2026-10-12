import { Injectable, inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { UserService } from '../services/user.service';
import { I18nService } from './i18n.service';
import { Lang, isLang } from './languages';

// Ties the UI language to the player's account: an explicit choice is saved to the account
// (so their other devices and push notifications follow), and the account's language wins
// over the device's when they sign in.
@Injectable({ providedIn: 'root' })
export class LanguagePreferenceService {
  private readonly i18n = inject(I18nService);
  private readonly auth = inject(AuthService);
  private readonly users = inject(UserService);

  change(lang: Lang): void {
    if (lang === this.i18n.lang()) return;
    void this.i18n.use(lang);
    if (!this.auth.isAuthenticated()) return;
    this.users.updateProfile({ preferredLanguage: lang }).subscribe({
      // Best effort: the device keeps the choice; the next change retries the account.
      error: () => {},
    });
  }

  applyAccount(lang: string | null | undefined): void {
    if (isLang(lang)) void this.i18n.use(lang);
  }
}
