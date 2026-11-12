import { describe, it, expect, beforeEach } from 'vitest'
import {
  fr,
  en,
  t,
  setLocale,
  registerLocale,
  useI18n,
  STORAGE_KEY
} from '@/i18n'

describe('i18n Internationalization Engine Tests', () => {
  beforeEach(() => {
    localStorage.clear()
    setLocale('fr')
  })

  it('enforces strict 1-to-1 key parity and non-empty values between FR and EN dictionaries', () => {
    const frKeys = Object.keys(fr).sort()
    const enKeys = Object.keys(en).sort()

    expect(enKeys).toEqual(frKeys)

    for (const key of frKeys) {
      expect(fr[key].trim().length).toBeGreaterThan(0)
      expect(en[key].trim().length).toBeGreaterThan(0)
    }
  })

  it('translates keys in FR and EN, interpolates parameters, and persists to localStorage', () => {
    const { locale } = useI18n()

    expect(locale.value).toBe('fr')
    expect(t('dashboard.title')).toBe('Frais de route')
    expect(t('dashboard.foldersCount', { count: 4 })).toBe('4 dossier(s)')

    setLocale('en')
    expect(locale.value).toBe('en')
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en')
    expect(document.documentElement.lang).toBe('en')
    expect(t('dashboard.title')).toBe('Travel Expenses')
    expect(t('dashboard.foldersCount', { count: 4 })).toBe('4 report(s)')
  })

  it('falls back gracefully for unknown locale or unknown translation key', () => {
    setLocale('unknown-lang')
    const { locale } = useI18n()
    expect(locale.value).toBe('fr')

    expect(t('non.existent.key')).toBe('non.existent.key')
  })

  it('supports dynamically registering a new language (e.g. Spanish ES)', () => {
    const { availableLocales, locale } = useI18n()

    registerLocale(
      'es',
      {
        ...en,
        'dashboard.title': 'Gastos de viaje',
        'nav.reports': 'Informes'
      },
      { code: 'es', label: 'ES', flag: '🇪🇸' }
    )

    expect(availableLocales.value.some((l) => l.code === 'es')).toBe(true)

    setLocale('es')
    expect(locale.value).toBe('es')
    expect(t('dashboard.title')).toBe('Gastos de viaje')
    expect(t('nav.reports')).toBe('Informes')
  })
})
