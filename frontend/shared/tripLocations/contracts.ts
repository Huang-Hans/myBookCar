export interface TripLocation {
  latitude: number
  longitude: number
  headingDegrees: number | null
  speedKmh: number | null
  seq: number
  recordedAt: string
}

export interface TripLocationSnapshot {
  tripId: string
  isSharing: boolean
  location: TripLocation | null
}

export interface TripTrack {
  tripId: string
  isSharing: boolean
  sampled: boolean
  points: TripLocation[]
}

export interface TripLocationEvent {
  schemaVersion: 1
  eventType: 'TRIP_LOCATION_UPDATED' | 'TRIP_LOCATION_ENDED'
  tripId: string
  location: TripLocation | null
}

export interface TripLocationMessage {
  code: number
  message: string
  data: TripLocationEvent
  traceId: string
}

export interface TripLocationResult<T> {
  code: number
  message: string
  data?: T | null
  traceId: string
}

export interface DriverLocationPoint {
  seq: number
  latitude: number
  longitude: number
  accuracyMeters?: number
  headingDegrees?: number
  speedKmh?: number
  recordedAt: string
}

export interface DriverLocationUpload {
  coordinateSystem: 'GCJ02'
  points: DriverLocationPoint[]
}

const inRange = (value: unknown, minimum: number, maximum: number): value is number =>
  typeof value === 'number' && Number.isFinite(value) && value >= minimum && value <= maximum

export function isTripLocation(value: unknown): value is TripLocation {
  if (!value || typeof value !== 'object') return false
  const point = value as Partial<TripLocation>
  return inRange(point.latitude, -90, 90) && inRange(point.longitude, -180, 180)
    && Number.isSafeInteger(point.seq) && (point.seq ?? 0) > 0
    && (point.headingDegrees === null || inRange(point.headingDegrees, 0, 360))
    && (point.speedKmh === null || inRange(point.speedKmh, 0, Number.MAX_VALUE))
    && typeof point.recordedAt === 'string' && point.recordedAt.endsWith('Z') && Number.isFinite(Date.parse(point.recordedAt))
}

export function isTripLocationSnapshot(value: unknown): value is TripLocationSnapshot {
  if (!value || typeof value !== 'object') return false
  const snapshot = value as Partial<TripLocationSnapshot>
  return typeof snapshot.tripId === 'string' && Boolean(snapshot.tripId) && typeof snapshot.isSharing === 'boolean'
    && (snapshot.location === null || isTripLocation(snapshot.location))
    && (snapshot.isSharing || snapshot.location === null)
}

export function isTripTrack(value: unknown): value is TripTrack {
  if (!value || typeof value !== 'object') return false
  const track = value as Partial<TripTrack>
  return typeof track.tripId === 'string' && Boolean(track.tripId)
    && typeof track.isSharing === 'boolean' && typeof track.sampled === 'boolean'
    && Array.isArray(track.points) && track.points.every(isTripLocation)
}

export function isDriverLocationPoint(value: unknown): value is DriverLocationPoint {
  if (!value || typeof value !== 'object') return false
  const point = value as Partial<DriverLocationPoint>
  return isTripLocation({ ...point, headingDegrees: point.headingDegrees ?? null, speedKmh: point.speedKmh ?? null })
    && (point.accuracyMeters === undefined || inRange(point.accuracyMeters, 0, Number.MAX_VALUE))
}

export function isTripLocationMessage(value: unknown): value is TripLocationMessage {
  if (!value || typeof value !== 'object') return false
  const envelope = value as Partial<TripLocationMessage>
  const data = envelope.data
  if (envelope.code !== 20000 || !data || data.schemaVersion !== 1 || !data.tripId) return false
  if (typeof envelope.traceId !== 'string' || typeof data.tripId !== 'string') return false
  return data.eventType === 'TRIP_LOCATION_UPDATED' ? isTripLocation(data.location)
    : data.eventType === 'TRIP_LOCATION_ENDED' && data.location === null
}
