import { beforeEach, describe, expect, it, vi } from 'vitest'

import { TAB_REFRESH_KEY, consumeTabRefresh, markTabRefresh } from './tabNavigation'

describe('tab navigation refresh marker', () => {
  const storage = new Map<string, string>()
  const uniMock = {
    setStorageSync: vi.fn((key: string, value: string) => { storage.set(key, value) }),
    getStorageSync: vi.fn((key: string) => storage.get(key) ?? ''),
    removeStorageSync: vi.fn((key: string) => { storage.delete(key) }),
  }
  vi.stubGlobal('uni', uniMock)

  beforeEach(() => { storage.clear(); uniMock.setStorageSync.mockClear(); uniMock.getStorageSync.mockClear(); uniMock.removeStorageSync.mockClear() })

  it('sets a one-shot marker for the tapped tab', () => {
    markTabRefresh('bookings')
    expect(uniMock.setStorageSync).toHaveBeenCalledWith(TAB_REFRESH_KEY, 'bookings')
  })

  it('consumes the marker only for the matching tab and clears it', () => {
    markTabRefresh('account')
    expect(consumeTabRefresh('home')).toBe(false)
    expect(storage.has(TAB_REFRESH_KEY)).toBe(true)
    expect(consumeTabRefresh('account')).toBe(true)
    expect(storage.has(TAB_REFRESH_KEY)).toBe(false)
  })

  it('returns false when no marker was set, so plain page opens do not refetch', () => {
    expect(consumeTabRefresh('home')).toBe(false)
    expect(consumeTabRefresh('bookings')).toBe(false)
    expect(consumeTabRefresh('account')).toBe(false)
  })
})
