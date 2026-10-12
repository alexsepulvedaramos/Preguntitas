import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';

import { DEFAULT_LANG, LOCALES, Lang, browserLang, isLang } from './languages';
import { PluralForms, Translation, TranslationKey, TranslationParams } from './translation.types';

const STORAGE_KEY = 'vp_lang';

// One lazy chunk per language: only the active dictionary is downloaded (and the service
// worker caches it with the rest of the app's JS).
const LOADERS: Record<Lang, () => Promise<Translation>> = {
  es: () => import('./translations/es').then(m => m.es),
  en: () => import('./translations/en').then(m => m.en),
  de: () => import('./translations/de').then(m => m.de),
};

function readStoredLang(): Lang | null {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    return isLang(value) ? value : null;
  } catch {
    return null;
  }
}

function storeLang(lang: Lang): void {
  try {
    localStorage.setItem(STORAGE_KEY, lang);
  } catch {
    // Storage unavailable: the browser language is used again next time.
  }
}

function resolve(dictionary: Translation | null, key: string): string | PluralForms | undefined {
  let node: unknown = dictionary;
  for (const part of key.split('.')) {
    if (node === null || typeof node !== 'object') return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return typeof node === 'string' || (node !== null && typeof node === 'object')
    ? (node as string | PluralForms)
    : undefined;
}

function interpolate(text: string, params: TranslationParams, escape: (value: string) => string): string {
  return text.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? escape(String(params[name])) : match,
  );
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

// Runtime UI translation (rama 23). The active language and dictionary are signals, so a
// template that calls t() re-renders by itself when the language changes — no reload.
@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly document = inject(DOCUMENT);
  private readonly current = signal<Lang>(DEFAULT_LANG);
  private readonly dictionary = signal<Translation | null>(null);
  private loadToken = 0;

  readonly lang = this.current.asReadonly();
  readonly locale = computed(() => LOCALES[this.current()]);
  private readonly pluralRules = computed(() => new Intl.PluralRules(this.locale()));

  // Runs before the first render: the language picked on this device, else the browser's.
  init(): Promise<void> {
    return this.use(readStoredLang() ?? browserLang(), false);
  }

  // Switches the UI language. `remember` stores it for this device (explicit choices and
  // the account's preference); the browser-detected default isn't stored.
  async use(lang: Lang, remember = true): Promise<void> {
    if (remember) storeLang(lang);
    const token = ++this.loadToken;
    if (lang === this.current() && this.dictionary()) return;

    const dictionary = await LOADERS[lang]();
    if (token !== this.loadToken) return; // a later switch won the race

    this.dictionary.set(dictionary);
    this.current.set(lang);
    this.document.documentElement.lang = lang;
  }

  // Translates a key. `{name}` placeholders are filled from params; a plural entry picks its
  // form from params.count. Arrow function so components can expose it as `t = i18n.t`.
  readonly t = (key: TranslationKey, params?: TranslationParams): string =>
    this.translate(key, params, value => value);

  // For entries that carry markup (<strong>…): params are HTML-escaped, since they may hold
  // user data such as usernames. Bind with [innerHTML].
  readonly tHtml = (key: TranslationKey, params?: TranslationParams): string =>
    this.translate(key, params, escapeHtml);

  formatDate(date: Date | string | number, options: Intl.DateTimeFormatOptions): string {
    return new Date(date).toLocaleDateString(this.locale(), options);
  }

  private translate(key: string, params: TranslationParams | undefined, escape: (v: string) => string): string {
    const entry = resolve(this.dictionary(), key);
    if (entry === undefined) return key;

    let text: string;
    if (typeof entry === 'string') {
      text = entry;
    } else {
      const count = params?.['count'];
      const form = typeof count === 'number' ? this.pluralRules().select(count) : 'other';
      text = (entry as unknown as Record<string, string | undefined>)[form] ?? entry.other;
    }
    return params ? interpolate(text, params, escape) : text;
  }
}
