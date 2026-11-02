import { afterEach, describe, expect, it, vi } from 'vitest'
import { locale, localeTag, normalizeLocale, setLocale, t } from './locale'

const storage = new Map<string, string>()
vi.stubGlobal('uni', {
  getStorageSync: (key: string) => storage.get(key),
  setStorageSync: (key: string, value: string) => storage.set(key, value),
})

afterEach(() => { setLocale('EN'); storage.clear() })

describe('client locale', () => {
  it('normalizes unsupported preferences to English', () => {
    expect(normalizeLocale('ZH_HANT')).toBe('ZH_HANT')
    expect(normalizeLocale('ZH_HANS')).toBe('ZH_HANS')
    expect(normalizeLocale('unknown')).toBe('EN')
  })

  it('switches and persists both Chinese translations immediately', () => {
    setLocale('ZH_HANT')
    expect(locale.value).toBe('ZH_HANT')
    expect(t('Bookings')).toBe('預訂')
    expect(localeTag()).toBe('zh-HK')
    expect(storage.get('lmsClientLocale')).toBe('ZH_HANT')

    setLocale('ZH_HANS')
    expect(t('Bookings')).toBe('预订')
    expect(localeTag()).toBe('zh-CN')
    expect(storage.get('lmsClientLocale')).toBe('ZH_HANS')
  })

  it('keeps the source text for untranslated business data', () => {
    setLocale('ZH_HANT')
    expect(t('City of Dreams')).toBe('City of Dreams')
  })
})
