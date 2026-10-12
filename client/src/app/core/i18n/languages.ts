// Languages the UI is translated into (rama 23). Spanish is the original and the
// fallback for any browser language we don't support (developer decision 2026-10-12).
export const LANGUAGES = ['es', 'en', 'de'] as const;
export type Lang = (typeof LANGUAGES)[number];

export const DEFAULT_LANG: Lang = 'es';

// Each language's own name, so a player who can't read the current UI still finds theirs.
export const LANGUAGE_NAMES: Record<Lang, string> = {
  es: 'Español',
  en: 'English',
  de: 'Deutsch',
};

// Locale for Intl date/number formatting. en-GB keeps day-month order and the 24 h clock,
// like the rest of the app.
export const LOCALES: Record<Lang, string> = {
  es: 'es-ES',
  en: 'en-GB',
  de: 'de-DE',
};

export function isLang(value: unknown): value is Lang {
  return typeof value === 'string' && (LANGUAGES as readonly string[]).includes(value);
}

// First supported language in the browser's preference list (navigator.languages), matched
// by primary subtag ("en-US" → "en"). Never IP geolocation — browser language is the standard.
export function browserLang(): Lang {
  const preferred = typeof navigator === 'undefined'
    ? []
    : navigator.languages?.length ? navigator.languages : [navigator.language];
  for (const tag of preferred) {
    const primary = tag?.toLowerCase().split('-')[0];
    if (isLang(primary)) return primary;
  }
  return DEFAULT_LANG;
}
