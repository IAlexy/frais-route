import { ref, reactive, computed } from 'vue';
import type { Locale, LocaleOption, TranslationDictionary } from './types';
import { fr } from './locales/fr';
import { en } from './locales/en';

export const STORAGE_KEY = 'fraisroute_locale';

const dictionaries = reactive<Record<string, TranslationDictionary>>({
  fr,
  en
});

const localeOptions = ref<LocaleOption[]>([
  { code: 'fr', label: 'FR', flag: '🇫🇷' },
  { code: 'en', label: 'EN', flag: '🇬🇧' }
]);

function detectInitialLocale(): string {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && dictionaries[saved]) {
      return saved;
    }
  } catch {
    // Ignore storage access errors in SSR/test environments
  }
  return 'fr';
}

const currentLocale = ref<Locale>(detectInitialLocale());

export function setLocale(lang: Locale): void {
  const normalized = lang.trim().toLowerCase();
  if (!dictionaries[normalized]) {
    currentLocale.value = 'fr';
  } else {
    currentLocale.value = normalized;
  }

  try {
    localStorage.setItem(STORAGE_KEY, currentLocale.value);
  } catch {
    // Ignore storage errors
  }

  if (typeof document !== 'undefined') {
    document.documentElement.lang = currentLocale.value;
  }
}

export function registerLocale(
  code: string,
  dictionary: TranslationDictionary,
  option?: LocaleOption
): void {
  const normalized = code.trim().toLowerCase();
  dictionaries[normalized] = dictionary;
  if (option && !localeOptions.value.some((o) => o.code === normalized)) {
    localeOptions.value.push({ ...option, code: normalized });
  } else if (!localeOptions.value.some((o) => o.code === normalized)) {
    localeOptions.value.push({
      code: normalized,
      label: normalized.toUpperCase(),
      flag: '🌐'
    });
  }
}

export function t(key: string, params?: Record<string, string | number>): string {
  const activeDict = dictionaries[currentLocale.value] || dictionaries.fr;
  let template = activeDict?.[key] ?? dictionaries.fr[key] ?? key;

  if (params) {
    for (const [paramKey, paramVal] of Object.entries(params)) {
      template = template.replaceAll(`{${paramKey}}`, String(paramVal));
    }
  }

  return template;
}

export function useI18n() {
  return {
    locale: computed(() => currentLocale.value),
    availableLocales: computed(() => localeOptions.value),
    setLocale,
    registerLocale,
    t
  };
}

export { fr, en };
export type { Locale, LocaleOption, TranslationDictionary };
