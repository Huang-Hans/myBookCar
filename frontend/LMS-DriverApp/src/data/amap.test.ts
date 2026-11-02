import { describe, expect, it } from 'vitest'

import { buildAmapDrivingUrl, buildAmapGeocodeUrl, buildAmapPlaceSearchUrl, buildAmapRegeoUrl, buildAmapStaticMapUrl, calculateAmapRouteMetrics, parseAmapPosition, sampleAmapPath, toAmapPosition, wgs84ToGcj02 } from './amap'

describe('AMap helpers', () => {
  it('converts API latitude-longitude values to AMap longitude-latitude positions', () => {
    expect(toAmapPosition({ latitude: 22.1498, longitude: 113.5666 })).toEqual([113.5666, 22.1498])
    expect(parseAmapPosition('113.5915,22.1496')).toEqual([113.5915, 22.1496])
  })

  it('builds AMap geocoding and driving Web Service URLs', () => {
    const geocodeUrl = new URL(buildAmapGeocodeUrl('test-key', 'Altira Macau') ?? '')
    expect(geocodeUrl.hostname).toBe('restapi.amap.com')
    expect(geocodeUrl.searchParams.get('address')).toBe('Altira Macau')
    expect(geocodeUrl.searchParams.get('city')).toBe('澳门')

    const placeUrl = new URL(buildAmapPlaceSearchUrl('test-key', 'City of Dreams Lobby') ?? '')
    expect(placeUrl.pathname).toBe('/v3/place/text')
    expect(placeUrl.searchParams.get('keywords')).toBe('City of Dreams Lobby')
    expect(placeUrl.searchParams.get('citylimit')).toBe('false')

    const drivingUrl = new URL(buildAmapDrivingUrl('test-key', [113.543076, 22.186927], [113.579393, 22.15477]) ?? '')
    expect(drivingUrl.pathname).toBe('/v3/direction/driving')
    expect(drivingUrl.searchParams.get('origin')).toBe('113.543076,22.186927')
  })

  it('builds an AMap static image with route and endpoint markers', () => {
    const url = new URL(buildAmapStaticMapUrl({
      apiKey: 'test-key',
      origin: [113.543076, 22.186927],
      destination: [113.579393, 22.15477],
      currentPosition: [113.55, 22.17],
      path: [[113.543076, 22.186927], [113.56, 22.17], [113.579393, 22.15477]]
    }) ?? '')
    expect(url.pathname).toBe('/v3/staticmap')
    expect(url.searchParams.get('markers')).toContain('A:113.543076,22.186927')
    expect(url.searchParams.get('markers')).toContain('C:113.550000,22.170000')
    expect(url.searchParams.get('paths')).toContain('113.560000,22.170000')
    expect(url.searchParams.get('zoom')).toBeNull()
  })

  it('pins a fixed zoom level on the static image when requested', () => {
    const url = new URL(buildAmapStaticMapUrl({
      apiKey: 'test-key',
      origin: [113.543076, 22.186927],
      destination: [113.579393, 22.15477],
      zoom: 15
    }) ?? '')
    expect(url.searchParams.get('zoom')).toBe('15')
  })

  it('builds an AMap reverse-geocoding URL for the current position', () => {
    const url = new URL(buildAmapRegeoUrl('test-key', [113.566, 22.149]) ?? '')
    expect(url.pathname).toBe('/v3/geocode/regeo')
    expect(url.searchParams.get('location')).toBe('113.566000,22.149000')
    expect(url.searchParams.get('output')).toBe('json')
  })

  it('shifts WGS-84 coordinates onto the GCJ-02 datum inside China and leaves the rest untouched', () => {
    // Macau: navigator.geolocation (WGS-84) must move onto the AMap (GCJ-02) tiles.
    const [macauLng, macauLat] = wgs84ToGcj02([113.566, 22.149])
    expect(Math.abs(macauLng - 113.566)).toBeGreaterThan(0.001)
    expect(Math.abs(macauLat - 22.149)).toBeGreaterThan(0.001)
    expect(Math.abs(macauLng - 113.566)).toBeLessThan(0.02)
    expect(Math.abs(macauLat - 22.149)).toBeLessThan(0.02)
    // Beijing: the standard test vector for the public algorithm.
    const [beijingLng, beijingLat] = wgs84ToGcj02([116.391276, 39.907315])
    expect(beijingLng).toBeCloseTo(116.397517, 5)
    expect(beijingLat).toBeCloseTo(39.908716, 5)
    // Tokyo: outside the China envelope, the position passes through unchanged.
    expect(wgs84ToGcj02([139.6917, 35.6895])).toEqual([139.6917, 35.6895])
  })

  it('limits route points so the static-map URL stays bounded', () => {
    const path = Array.from({ length: 200 }, (_, index) => [113 + index / 1000, 22] as [number, number])
    const sampled = sampleAmapPath(path, 20)
    expect(sampled).toHaveLength(20)
    expect(sampled[0]).toEqual(path[0])
    expect(sampled[sampled.length - 1]).toEqual(path[path.length - 1])
  })

  it('converts the AMap route estimate into dashboard metrics', () => {
    expect(calculateAmapRouteMetrics({ routeDistanceMeters: 2_131, routeDurationSeconds: 455, distanceToPickupMeters: 850, hasCurrentPosition: true })).toEqual({
      coveredKm: 0,
      distanceToPickupKm: .85,
      speedKmh: expect.closeTo(16.86, 2),
      etaMinutes: 8,
      hasCurrentPosition: true,
    })
  })

  it('uses the driver-to-pickup route duration for pickup ETA', () => {
    expect(calculateAmapRouteMetrics({ routeDistanceMeters: 10_000, routeDurationSeconds: 1_200, distanceToPickupMeters: 2_400, approachRouteDurationSeconds: 300, hasCurrentPosition: true })).toMatchObject({ distanceToPickupKm: 2.4, etaMinutes: 5 })
  })
})
