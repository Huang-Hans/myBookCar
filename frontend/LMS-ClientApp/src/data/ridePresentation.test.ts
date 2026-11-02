import { describe, expect, it } from 'vitest'
import { displayDeparture, findRide, rideListIcon, rides } from './ridePresentation'

describe('ride presentation', () => {
  it('uses the Figma vehicle assets and capacities', () => {
    expect(rides).toHaveLength(3)
    expect(findRide('eqs')).toMatchObject({ model: 'EQS SUV 680 4MATIC', capacity: 4, image: '/static/figma/ride-eqs.webp' })
    expect(findRide('s580').capacity).toBe(3)
    expect(findRide('unknown').id).toBe('eqs')
  })

  it('formats the route departure without changing the booking value', () => {
    expect(displayDeparture('2026-10-14 11:00')).toBe('2026-10-14 · 11:00')
    expect(displayDeparture('Now')).toBe('Now')
  })

  it('uses one consistent list icon for every ride in the selected service type', () => {
    expect(rideListIcon('limousine')).toBe('/static/figma/ride-eqs.webp')
    expect(rideListIcon('party')).toBe('/static/figma/shuttle.webp')
    expect(rideListIcon('cross-border')).toBe('/static/figma/mpv.webp')
  })
})
