import type { es } from './translations/es';

// The Spanish dictionary is the source of truth for the shape: en.ts and de.ts are typed
// as Translation, so a missing or extra key in either one fails the build.
export type Translation = typeof es;

// A pluralised entry: t(key, { count }) picks the form with Intl.PluralRules.
// Never name a namespace's only keys "one" and "other" — it would read as plural forms.
export interface PluralForms {
  one: string;
  other: string;
}

type Leaves<T> = {
  [K in keyof T & string]: T[K] extends string | PluralForms ? K : `${K}.${Leaves<T[K]>}`;
}[keyof T & string];

// Every dotted path to a string (or plural) in the dictionary, e.g. 'groups.card.invite'.
// Templates are strictly type-checked, so a typo in a key is a compile error.
export type TranslationKey = Leaves<Translation>;

export type TranslationParams = Record<string, string | number>;
