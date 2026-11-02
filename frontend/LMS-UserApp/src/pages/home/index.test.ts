import { describe, expect, it } from 'vitest'
import homePage from './index.vue?raw'
import userPage from '../user/index.vue?raw'

describe('home page navigation', () => {
  it('keeps sign out on the user page instead of the home page', () => {
    expect(homePage).not.toContain("t('登出帳戶')")
    expect(homePage).not.toContain('@tap="signOut"')
    expect(userPage).toContain('@tap="signOut"')
  })

  it('keeps the interactive card hit areas free of transforms', () => {
    expect(homePage).not.toContain('animation-name:stat-enter-')
    expect(homePage).not.toContain('animation:shortcut-enter-')
    expect(homePage).toContain('touch-action:manipulation; pointer-events:auto;')
  })

  it('keeps shortcut cards and trip-card entrance animations', () => {
    expect(homePage).toContain('class="shortcut primary shortcut-from-left"')
    expect(homePage).toContain('class="shortcut shortcut-from-right"')
    expect(homePage).toContain('animationDelay: `${index * 110}ms`')
    expect(homePage).toContain('transform:translateY(26px)')
    expect(homePage).toContain('@media (prefers-reduced-motion:reduce)')
  })

  it('opens each overview card in its matching work area', () => {
    expect(homePage).toContain('href="#/pages/tracking/index"')
    expect(homePage).toContain('href="#/pages/dispatch/index"')
    expect(homePage).toContain('href="#/pages/bookings/index?filter=COMPLETED"')
    expect(homePage).toContain('class="stat" @click="openOverview(\'tracking\')"')
    expect(homePage).toContain('class="stat" @click="openOverview(\'dispatch\')"')
    expect(homePage).toContain('class="stat" @click="openOverview(\'completed\')"')
    expect(homePage).not.toContain('class="stat" @tap=')
  })

  it('uses native hash links for the H5 shortcut cards inside the APK WebView', () => {
    expect(homePage).toContain('class="shortcut primary shortcut-from-left" href="#/pages/booking/index"')
    expect(homePage).toContain('class="shortcut shortcut-from-right" href="#/pages/dispatch/index"')
    expect(homePage).toContain('<!-- #ifdef H5 -->')
    expect(homePage).toContain('<!-- #ifndef H5 -->')
  })
})
