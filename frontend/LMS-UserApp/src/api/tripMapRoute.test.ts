import { describe, expect, it } from 'vitest'
import type { TripLocation } from '../../../shared/tripLocations/contracts'
import { buildDrivingUrl, parseDrivingRoute, tripMapMetrics } from './tripMapRoute'

function point(seq: number, latitude: number, longitude: number, recordedAt: string): TripLocation {
  return { seq, latitude, longitude, recordedAt, speedKmh: 30, headingDegrees: 90 }
}

describe('staff trip map route', () => {
  it('requests the planned AMap driving route between catalog stops', () => {
    expect(buildDrivingUrl('key', [113.54, 22.19], [113.58, 22.15])).toContain('/v3/direction/driving?')
    expect(buildDrivingUrl('', [113.54, 22.19], [113.58, 22.15])).toBeNull()
  })

  it('extracts a road path and rejects missing or malformed paths', () => {
    expect(parseDrivingRoute({ status: '1', route: { paths: [{ distance: '7000', duration: '900', steps: [{ polyline: '113.54,22.19;113.55,22.18;113.58,22.15' }] }] } })).toEqual({
      points: [[113.54, 22.19], [113.55, 22.18], [113.58, 22.15]], distanceMeters: 7000, durationSeconds: 900,
    })
    expect(parseDrivingRoute({ status: '0' })).toBeNull()
    expect(parseDrivingRoute({ status: '1', route: { paths: [{ steps: [{ polyline: ',;invalid' }] }] } })).toBeNull()
  })

  it('reports live speed, pickup distance and ETA without counting GPS gaps as travel', () => {
    const first = point(1, 22.19, 113.54, '2026-10-22T10:00:00Z')
    const nearby = point(2, 22.1901, 113.5401, '2026-10-22T10:00:03Z')
    const afterGap = point(3, 22.17, 113.57, '2026-10-22T10:05:00Z')
    const route = { points: [[113.54, 22.19], [113.58, 22.15]] as [number, number][], distanceMeters: 7000, durationSeconds: 900 }
    const metrics = tripMapMetrics(route, [113.54, 22.19], [first, nearby, afterGap], true)
    expect(metrics.coveredKm).toBeGreaterThan(0)
    expect(metrics.coveredKm).toBeLessThan(0.1)
    expect(metrics.distanceToPickupKm).toBeGreaterThan(0)
    expect(metrics.speedKmh).toBe(30)
    expect(metrics.etaMinutes).toBe(15)
    expect(tripMapMetrics(route, [113.54, 22.19], [first], false).speedKmh).toBe(0)
  })
})
