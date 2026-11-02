import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import serviceInfoSource from './index.vue?raw'
import tripBannerSource from '../../components/CurrentTripBanner.vue?raw'

describe('service booking action layout', () => {
  it('keeps Book Now in the viewport flow above the tab bar and trip banner', () => {
    const servicePage = parse(serviceInfoSource).descriptor
    const banner = parse(tripBannerSource).descriptor
    const template = servicePage.template?.content ?? ''
    const script = servicePage.scriptSetup?.content ?? ''
    const styles = servicePage.styles.map((style) => style.content).join('\n')
    const bannerStyles = banner.styles.map((style) => style.content).join('\n')

    expect(script).toContain("import { isTripBannerVisible } from '@/data/customerNoticeStore'")
    expect(template).toContain(":class=\"{ 'has-trip-banner': isTripBannerVisible }\"")
    expect(template).toContain('<view class="action-bar"><button class="book-button"')
    expect(styles).toMatch(/\.service-page\s*\{[^}]*display:\s*flex;[^}]*height:\s*100dvh;[^}]*padding-bottom:\s*calc\(56px \+ env\(safe-area-inset-bottom\) \+ var\(--trip-banner-height\)\);/)
    expect(styles).toMatch(/\.service-page\.has-trip-banner\s*\{\s*--trip-banner-height:\s*52px;/)
    expect(bannerStyles).toMatch(/\.trip-banner\s*\{[^}]*height:52px;/)
    expect(styles).toMatch(/\.scroll\s*\{[^}]*min-height:\s*0;[^}]*flex:\s*1 1 0;/)
    expect(styles).toMatch(/\.action-bar\s*\{[^}]*position:\s*relative;[^}]*flex:\s*none;/)
    expect(styles).not.toMatch(/\.action-bar\s*\{[^}]*position:\s*fixed;/)
  })
})
