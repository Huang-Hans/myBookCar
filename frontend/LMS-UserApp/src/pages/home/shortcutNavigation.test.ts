import { describe, expect, it, vi } from 'vitest'
import { openHomeOverview, openHomeShortcut } from './shortcutNavigation'
import homePage from './index.vue?raw'

describe('home shortcuts in the Android WebView', () => {
  it('opens quick booking and falls back when the page stack rejects navigation', () => {
    const navigateTo = vi.fn()
    const reLaunch = vi.fn()
    openHomeShortcut('booking', { navigateTo, reLaunch } as unknown as Pick<typeof uni, 'navigateTo' | 'reLaunch'>)
    expect(navigateTo).toHaveBeenCalledWith(expect.objectContaining({ url: '/pages/booking/index' }))
    expect(reLaunch).not.toHaveBeenCalled()
    const options = navigateTo.mock.calls[0]?.[0] as { fail: () => void }
    options.fail()
    expect(reLaunch).toHaveBeenCalledWith({ url: '/pages/booking/index' })
  })

  it('opens dispatch and falls back when navigation fails', () => {
    const navigateTo = vi.fn()
    const reLaunch = vi.fn()
    openHomeShortcut('dispatch', { navigateTo, reLaunch } as unknown as Pick<typeof uni, 'navigateTo' | 'reLaunch'>)
    expect(navigateTo).toHaveBeenCalledWith(expect.objectContaining({ url: '/pages/dispatch/index' }))
    const options = navigateTo.mock.calls[0]?.[0] as { fail: () => void }
    options.fail()
    expect(reLaunch).toHaveBeenCalledWith({ url: '/pages/dispatch/index' })
  })

  it.each([
    ['tracking', '/pages/tracking/index'],
    ['dispatch', '/pages/dispatch/index'],
    ['completed', '/pages/bookings/index?filter=COMPLETED'],
  ] as const)('opens the %s overview and preserves the destination', (target, url) => {
    const navigateTo = vi.fn()
    const reLaunch = vi.fn()
    openHomeOverview(target, { navigateTo, reLaunch } as unknown as Pick<typeof uni, 'navigateTo' | 'reLaunch'>)
    expect(navigateTo).toHaveBeenCalledWith(expect.objectContaining({ url }))
    expect(reLaunch).not.toHaveBeenCalled()
    const options = navigateTo.mock.calls[0]?.[0] as { fail: () => void }
    options.fail()
    expect(reLaunch).toHaveBeenCalledWith({ url })
  })

  it('uses native click handlers on the whole shortcut cards', () => {
    expect(homePage).toContain('class="shortcut primary shortcut-from-left" @click="openQuickBooking"')
    expect(homePage).toContain('class="shortcut shortcut-from-right" @click="openDispatch"')
    expect(homePage).not.toContain('shortcut-from-left" @tap=')
    expect(homePage).not.toContain('shortcut-from-right" @tap=')
  })
})
