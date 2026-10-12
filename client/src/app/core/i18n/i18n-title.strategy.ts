import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

import { I18nService } from './i18n.service';
import { TranslationKey } from './translation.types';

// Route `title`s are translation keys (rama 23). The tab title is re-translated when the
// language changes; a route without a title keeps the previous one, as Angular's default.
@Injectable({ providedIn: 'root' })
export class I18nTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly i18n = inject(I18nService);
  private readonly key = signal<TranslationKey | null>(null);

  constructor() {
    super();
    effect(() => {
      const key = this.key();
      if (key) this.title.setTitle(this.i18n.t(key));
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const key = this.buildTitle(snapshot);
    if (key !== undefined) this.key.set(key as TranslationKey);
  }
}
