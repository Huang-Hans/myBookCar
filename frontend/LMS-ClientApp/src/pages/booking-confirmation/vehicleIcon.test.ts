import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import { rideListIcon } from '../../data/ridePresentation'
import pageSource from './index.vue?raw'

describe('booking confirmation vehicle icon', () => {
  it('uses the booking service category instead of the ride query parameter', () => {
    const script = parse(pageSource).descriptor.scriptSetup?.content ?? ''
    expect(script).toContain('rideListIcon(booking.value.serviceId)')
    expect(rideListIcon('limousine')).toBe('/static/figma/ride-eqs.webp')
    expect(rideListIcon('party')).toBe('/static/figma/shuttle.webp')
    expect(rideListIcon('cross-border')).toBe('/static/figma/mpv.webp')
  })
})
