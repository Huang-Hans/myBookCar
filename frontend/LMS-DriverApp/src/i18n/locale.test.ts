import { afterEach, describe, expect, it, vi } from 'vitest'
import { locale, localeTag, normalizeLocale, setLocale, t } from './locale'

const storage = new Map<string, string>()
vi.stubGlobal('uni', {
  getStorageSync: (key: string) => storage.get(key),
  setStorageSync: (key: string, value: string) => storage.set(key, value),
})

afterEach(() => { setLocale('EN'); storage.clear() })

describe('driver locale', () => {
  it('normalizes stored legacy language labels', () => {
    expect(normalizeLocale('繁體中文')).toBe('ZH_HANT')
    expect(normalizeLocale('简体中文')).toBe('ZH_HANS')
    expect(normalizeLocale('unknown')).toBe('EN')
  })

  it('switches and persists both Chinese translations immediately', () => {
    setLocale('ZH_HANT')
    expect(locale.value).toBe('ZH_HANT')
    expect(t('Journey Progress')).toBe('行程進度')
    expect(t('Fail')).toBe('不合格')
    expect(localeTag()).toBe('zh-HK')
    expect(storage.get('lmsDriverLocale')).toBe('ZH_HANT')

    setLocale('ZH_HANS')
    expect(t('Journey Progress')).toBe('行程进度')
    expect(localeTag()).toBe('zh-CN')
    expect(storage.get('lmsDriverLocale')).toBe('ZH_HANS')
  })
})
