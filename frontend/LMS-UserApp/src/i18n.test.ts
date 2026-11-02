import { afterEach, describe, expect, it } from 'vitest'
import { locale, setLocale, t } from './i18n'

afterEach(() => setLocale('zh-Hant'))

describe('staff app language', () => {
  it('switches between Traditional Chinese and English', () => {
    expect(t('工作台')).toBe('工作台')
    setLocale('en')
    expect(locale.value).toBe('en')
    expect(t('工作台')).toBe('Home')
    expect(t('員工資料')).toBe('Staff profile')
  })
})
