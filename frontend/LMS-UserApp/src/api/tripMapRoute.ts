import type { TripLocation } from '../../../shared/tripLocations/contracts'
import { splitTrackSegments } from './tripTrackSegments'

export type MapPosition = [longitude: number, latitude: number]

export interface DrivingRoute {
  points: MapPosition[]
  distanceMeters: number
  durationSeconds: number
}

export interface TripMapMetrics {
  coveredKm: number
  distanceToPickupKm: number
  speedKmh: number
  etaMinutes: number
}

function isPosition(position: MapPosition): boolean {
  return Number.isFinite(position[0]) && Number.isFinite(position[1])
    && Math.abs(position[0]) <= 180 && Math.abs(position[1]) <= 90
}

// WINSON-DEVIATION: The external AMap driving API requires its key in the query string.
export function buildDrivingUrl(key: string, origin: MapPosition, destination: MapPosition): string | null {
  if (!key.trim() || !isPosition(origin) || !isPosition(destination)) return null
  const query = new URLSearchParams({
    key: key.trim(), origin: origin.join(','), destination: destination.join(','),
    strategy: '0', extensions: 'base', output: 'json',
  })
  return `https://restapi.amap.com/v3/direction/driving?${query}`
}

export function parseDrivingRoute(value: unknown): DrivingRoute | null {
  if (!value || typeof value !== 'object') return null
  const response = value as { status?: unknown; route?: { paths?: Array<{ distance?: unknown; duration?: unknown; steps?: Array<{ polyline?: unknown }> }> } }
  if (response.status !== '1') return null
  const path = response.route?.paths?.[0]
  if (!path) return null
  const points = (path.steps ?? []).flatMap((step) => typeof step.polyline === 'string' ? step.polyline.split(';') : [])
    .map((pair): MapPosition | null => {
      const parts = pair.split(',')
      if (parts.length !== 2 || parts.some((part) => !part.trim())) return null
      const position: MapPosition = [Number(parts[0]), Number(parts[1])]
      return isPosition(position) ? position : null
    }).filter((position): position is MapPosition => position !== null)
  if (points.length < 2) return null
  return { points, distanceMeters: Math.max(0, Number(path.distance) || 0), durationSeconds: Math.max(0, Number(path.duration) || 0) }
}

export function distanceMeters(from: MapPosition, to: MapPosition): number {
  const radians = Math.PI / 180
  const latitudeDelta = (to[1] - from[1]) * radians
  const longitudeDelta = (to[0] - from[0]) * radians
  const haversine = Math.sin(latitudeDelta / 2) ** 2
    + Math.cos(from[1] * radians) * Math.cos(to[1] * radians) * Math.sin(longitudeDelta / 2) ** 2
  return 12_742_000 * Math.asin(Math.min(1, Math.sqrt(haversine)))
}

export function tripMapMetrics(route: DrivingRoute | null, origin: MapPosition | null,
  points: readonly TripLocation[], isSharing: boolean): TripMapMetrics {
  const coveredMeters = splitTrackSegments(points).reduce((total, segment) => total + segment.slice(1).reduce((distance, point, index) => {
    const previous = segment[index]
    return previous ? distance + distanceMeters([previous.longitude, previous.latitude], [point.longitude, point.latitude]) : distance
  }, 0), 0)
  const latest = isSharing ? points[points.length - 1] : undefined
  const distanceToPickup = latest && origin ? distanceMeters([latest.longitude, latest.latitude], origin) : 0
  return {
    coveredKm: coveredMeters / 1_000,
    distanceToPickupKm: distanceToPickup / 1_000,
    speedKmh: latest?.speedKmh ?? 0,
    etaMinutes: route?.durationSeconds ? Math.max(1, Math.ceil(route.durationSeconds / 60)) : 0,
  }
}
