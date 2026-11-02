import { describe, expect, it } from 'vitest'
import { distanceMeters, isDriverNearRoute } from './driverMapViewport'

const pickup: [number, number] = [113.56009, 22.15873]
const dropoff: [number, number] = [113.57940, 22.15489]

describe('driver map viewport', () => {
  it('includes a driver near the route', () => {
    expect(isDriverNearRoute([113.57, 22.16], pickup, dropoff)).toBe(true)
  })

  it('flags a genuine remote fix instead of plotting it falsely in Macau', () => {
    const driver: [number, number] = [-118.26270, 34.05200]
    expect(isDriverNearRoute(driver, pickup, dropoff)).toBe(false)
    expect(distanceMeters(driver, pickup)).toBeGreaterThan(1_000_000)
  })
})
