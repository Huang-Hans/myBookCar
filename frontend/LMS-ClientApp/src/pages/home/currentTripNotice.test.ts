import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import bannerSource from '../../components/CurrentTripBanner.vue?raw'

describe('current trip notice', () => {
  it('routes the notice to the current trip details instead of the bookings list', () => {
    const descriptor = parse(bannerSource).descriptor
    const template = descriptor.template?.content ?? ''
    const script = descriptor.scriptSetup?.content ?? ''

    expect(template).toMatch(/class="trip-banner-open" @tap="openCurrentTrip"/)
    expect(script).toContain('currentCustomerTripId.value')
    expect(script).toContain('uni.navigateTo({ url: orderDetailsRoute(currentCustomerTripId.value) })')
  })
})
