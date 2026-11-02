import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import activity from '../android/app/src/main/java/com/melco/lms/client/MainActivity.java?raw'
import tabBar from './components/AppTabBar.vue?raw'
import tripBanner from './components/CurrentTripBanner.vue?raw'

describe('Android safe-area layout', () => {
  it('keeps the WebView inside system bars, cutouts and the keyboard', () => {
    expect(activity).toContain('setDecorFitsSystemWindows(false)')
    expect(activity).toContain('WindowInsets.Type.systemBars()')
    expect(activity).toContain('WindowInsets.Type.displayCutout()')
    expect(activity).toContain('WindowInsets.Type.ime()')
    expect(activity).toContain('view.setPadding(safeInsets.left, safeInsets.top,')
    expect(activity).toContain('.setInsets(handledTypes, Insets.NONE)')
  })

  it('has no fixed empty spacer below the navigation buttons', () => {
    const tabStyles = parse(tabBar).descriptor.styles[0]?.content ?? ''
    const bannerStyles = parse(tripBanner).descriptor.styles[0]?.content ?? ''
    expect(tabStyles).toContain('height: calc(56px + env(safe-area-inset-bottom))')
    expect(tabStyles).toContain('padding-bottom: env(safe-area-inset-bottom)')
    expect(bannerStyles).toContain('bottom:calc(56px + env(safe-area-inset-bottom))')
    expect(tabStyles).not.toContain('height: calc(83px')
  })
})
