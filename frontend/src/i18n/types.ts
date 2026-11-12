export type Locale = 'fr' | 'en' | string;

export type TranslationDictionary = Record<string, string>;

export interface LocaleOption {
  code: string;
  label: string;
  flag: string;
}
