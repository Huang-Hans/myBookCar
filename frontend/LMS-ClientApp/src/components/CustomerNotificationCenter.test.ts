import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import centerSource from './CustomerNotificationCenter.vue?raw'
import tabBarSource from './AppTabBar.vue?raw'
import mapSource from '../pages/driver-map/index.vue?raw'

describe('customer notification entry points', () => {
  it('appears on every bottom-navigation page and the full-screen driver map', () => {
    expect(parse(tabBarSource).descriptor.template?.content).toContain('<CustomerNotificationCenter />')
    expect(parse(mapSource).descriptor.template?.content).toContain('<CustomerNotificationCenter placement="floating" />')
  })

  it('exposes unread alerts, the current trip, recent updates, and a new-update toast', () => {
    const descriptor = parse(centerSource).descriptor
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')
    expect(template).toContain('unreadCustomerNoticeCount')
    expect(template).toContain('v-if="customerNotificationBadgeCount" class="notification-badge"')
    expect(template).not.toContain('current-dot')
    expect(template).toContain('currentCustomerTripLabel')
    expect(template).toContain('v-for="notice in customerNotices"')
    expect(template).toContain('v-if="arrival"')
    expect(template).toContain('openTrip(notice.tripId, notice.eventId)')
    expect(styles).toMatch(/\.notification-badge\s*\{[^}]*top:0;\s*right:0;/)
    expect(styles).toContain('white-space:nowrap')
    expect(styles).not.toContain('right:calc(50% - 26px)')
  })

  it('anchors the dialog close control to the top-right corner', () => {
    const descriptor = parse(centerSource).descriptor
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')
    expect(template).toContain('class="sheet-close"')
    expect(styles).toMatch(/\.sheet-heading\s*\{[^}]*position:relative/)
    expect(styles).toMatch(/\.sheet-close\s*\{[^}]*position:absolute;\s*top:0;\s*right:0;/)
  })
})
