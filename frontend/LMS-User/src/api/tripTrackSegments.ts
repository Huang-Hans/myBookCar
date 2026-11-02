import type { TripLocation } from '../../../shared/tripLocations/contracts'

/** Devices report about every 5 seconds; a longer silence means the driver was offline or the app was in the background. */
const MAX_REPORT_GAP_MS = 60_000
/** At 5-second spacing even a motorway drive stays far below this, so a bigger jump is a location source switch or a bad fix. */
const MAX_POINT_JUMP_METERS = 1_000
const EARTH_RADIUS_METERS = 6_371_000

function distanceMeters(from: TripLocation, to: TripLocation): number {
  const radians = Math.PI / 180
  const latitudeDelta = (to.latitude - from.latitude) * radians
  const longitudeDelta = (to.longitude - from.longitude) * radians
  const haversine = Math.sin(latitudeDelta / 2) ** 2
    + Math.cos(from.latitude * radians) * Math.cos(to.latitude * radians) * Math.sin(longitudeDelta / 2) ** 2
  return 2 * EARTH_RADIUS_METERS * Math.asin(Math.min(1, Math.sqrt(haversine)))
}

function isBreak(previous: TripLocation, point: TripLocation): boolean {
  return Date.parse(point.recordedAt) - Date.parse(previous.recordedAt) > MAX_REPORT_GAP_MS
    || distanceMeters(previous, point) > MAX_POINT_JUMP_METERS
}

/**
 * Splits the track wherever reporting paused or the position jumped, so the map never draws a straight line
 * across a stretch that was not actually driven. Segments with a single point are kept; the caller decides
 * whether they can be drawn.
 */
export function splitTrackSegments(points: readonly TripLocation[]): TripLocation[][] {
  const segments: TripLocation[][] = []
  let previous: TripLocation | null = null
  for (const point of points) {
    if (previous === null || isBreak(previous, point)) segments.push([])
    segments[segments.length - 1]?.push(point)
    previous = point
  }
  return segments
}
