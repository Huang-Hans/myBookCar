import { ApiRequestError } from './http'
import { driverApi, type DriverLocationPoint, type DriverLocationUploadResult, type DriverTripDetail } from './driver'
import { wgs84ToGcj02 } from '../data/amap'
import { tripStageIndex } from '../data/driverState'

export const LOCATION_REPORT_INTERVAL_MS = 3_000
const MAX_POINTS_PER_UPLOAD = 50
const LOCATION_TIMEOUT_MS = 15_000
const sequenceKey = (tripId: string): string => `lmsDriverLocationSeq:${tripId}`

export type LocationSample = Omit<DriverLocationPoint, 'seq'>

export function canReportTripLocation(trip: Pick<DriverTripDetail, 'tripId' | 'stage' | 'status'> | null): boolean {
  const stage = trip ? tripStageIndex(trip.stage) : -1
  return Boolean(trip?.tripId) && stage >= 0 && stage < 5
    && trip?.status !== 'COMPLETED' && trip?.status !== 'CANCELLED'
}

interface LocationReporterDependencies {
  capture(): Promise<LocationSample | null>
  upload(tripId: string, points: DriverLocationPoint[]): Promise<DriverLocationUploadResult>
  readSequence(tripId: string): number
  writeSequence(tripId: string, seq: number): void
}

const isValidCoordinate = (latitude: number, longitude: number): boolean =>
  Number.isFinite(latitude) && latitude >= -90 && latitude <= 90
  && Number.isFinite(longitude) && longitude >= -180 && longitude <= 180

function optionalNonNegative(value: unknown): number | undefined {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0 ? value : undefined
}

function sampleFromCoordinates(coordinates: {
  latitude: number
  longitude: number
  accuracy?: number | null
  heading?: number | null
  speed?: number | null
}, recordedAt: number, isWgs84: boolean): LocationSample | null {
  if (!isValidCoordinate(coordinates.latitude, coordinates.longitude)) return null
  const [longitude, latitude] = isWgs84
    ? wgs84ToGcj02([coordinates.longitude, coordinates.latitude])
    : [coordinates.longitude, coordinates.latitude]
  const accuracyMeters = optionalNonNegative(coordinates.accuracy)
  const heading = optionalNonNegative(coordinates.heading)
  const speed = optionalNonNegative(coordinates.speed)
  return {
    latitude,
    longitude,
    ...(accuracyMeters === undefined ? {} : { accuracyMeters }),
    ...(heading === undefined || heading > 360 ? {} : { headingDegrees: heading }),
    ...(speed === undefined ? {} : { speedKmh: speed * 3.6 }),
    recordedAt: new Date(Number.isFinite(recordedAt) ? recordedAt : Date.now()).toISOString(),
  }
}

// The uni build replaces this platform constant. Browser GPS is WGS-84; native gcj02 fixes are already converted.
declare const process: { env: { UNI_PLATFORM?: string } }
const isH5Build = process.env.UNI_PLATFORM === 'h5'

export function captureDriverLocation(): Promise<LocationSample | null> {
  return new Promise((resolve) => {
    let isSettled = false
    const finish = (sample: LocationSample | null): void => {
      if (isSettled) return
      isSettled = true
      clearTimeout(timeout)
      resolve(sample)
    }
    const timeout = setTimeout(() => finish(null), LOCATION_TIMEOUT_MS)
    if (isH5Build && typeof navigator !== 'undefined' && navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => finish(sampleFromCoordinates(position.coords, position.timestamp, true)),
        () => finish(null),
        { enableHighAccuracy: true, timeout: LOCATION_TIMEOUT_MS, maximumAge: 0 },
      )
      return
    }
    uni.getLocation({
      type: 'gcj02',
      isHighAccuracy: true,
      success: (position) => finish(sampleFromCoordinates(position, Date.now(), isH5Build)),
      fail: () => finish(null),
    })
  })
}

function readSequence(tripId: string): number {
  try {
    const value: unknown = uni.getStorageSync(sequenceKey(tripId))
    const seq = Number(value)
    return Number.isSafeInteger(seq) && seq > 0 ? seq : 0
  } catch { return 0 }
}

function writeSequence(tripId: string, seq: number): void {
  try { uni.setStorageSync(sequenceKey(tripId), seq) }
  catch { /* The current process still keeps a monotonic sequence. */ }
}

const defaultDependencies: LocationReporterDependencies = {
  capture: captureDriverLocation,
  upload: async (tripId, points) => (await driverApi.reportLocations(tripId, points)).data,
  readSequence,
  writeSequence,
}

export class DriverLocationReporter {
  private tripId: string | null = null
  private lastSeq = 0
  private queue: DriverLocationPoint[] = []
  private timer: ReturnType<typeof setInterval> | null = null
  private isTicking = false
  private generation = 0
  private blockedTripId: string | null = null

  constructor(private readonly dependencies: LocationReporterDependencies = defaultDependencies) {}

  start(tripId: string): void {
    if (!tripId || this.blockedTripId === tripId) return
    if (this.tripId !== tripId) {
      this.pause()
      this.tripId = tripId
      this.blockedTripId = null
      this.lastSeq = this.dependencies.readSequence(tripId)
      this.queue = []
    }
    if (this.timer) return
    this.generation += 1
    this.timer = setInterval(() => { void this.tick() }, LOCATION_REPORT_INTERVAL_MS)
    void this.tick()
  }

  pause(): void {
    if (this.timer) clearInterval(this.timer)
    this.timer = null
    this.generation += 1
    this.isTicking = false
  }

  endTrip(): void {
    this.pause()
    this.tripId = null
    this.queue = []
  }

  private async tick(): Promise<void> {
    if (!this.tripId || !this.timer || this.isTicking) return
    const tripId = this.tripId
    const generation = this.generation
    this.isTicking = true
    try {
      let sample: LocationSample | null = null
      try { sample = await this.dependencies.capture() }
      catch { /* An unavailable GPS fix must not prevent retrying queued points. */ }
      if (generation !== this.generation || tripId !== this.tripId) return
      if (sample && isValidCoordinate(sample.latitude, sample.longitude)) {
        const seq = this.lastSeq + 1
        this.lastSeq = seq
        this.dependencies.writeSequence(tripId, seq)
        this.queue.push({ ...sample, seq })
      }
      while (this.queue.length && generation === this.generation && tripId === this.tripId) {
        const batch = this.queue.slice(0, MAX_POINTS_PER_UPLOAD)
        try {
          const result = await this.dependencies.upload(tripId, batch)
          if (generation !== this.generation || tripId !== this.tripId) return
          const acceptedSeq = result.acceptedSeq
          if (!Number.isSafeInteger(acceptedSeq) || acceptedSeq < 0) throw new Error('Invalid location acknowledgement')
          this.lastSeq = Math.max(this.lastSeq, acceptedSeq)
          this.dependencies.writeSequence(tripId, this.lastSeq)
          this.queue = this.queue.filter((point) => point.seq > acceptedSeq)
          if (this.queue[0]?.seq === batch[0]?.seq) break
        } catch (reason) {
          if (generation !== this.generation || tripId !== this.tripId) return
          const error = reason instanceof ApiRequestError ? reason : null
          if (error?.code === 40400 || error?.code === 40900) {
            this.blockedTripId = tripId
            this.endTrip()
            return
          }
          if (error?.code === 40100) { this.pause(); return }
          if (error?.code === 40001 || error?.code === 40000) this.queue.splice(0, batch.length)
          console.error(JSON.stringify({ event: 'driverLocationUploadFailed', code: error?.code ?? 50000, traceId: error?.traceId ?? 'unavailable' }))
          if (!error || (error.code !== 40001 && error.code !== 40000)) break
        }
      }
    } finally {
      if (generation === this.generation) this.isTicking = false
    }
  }
}

export const driverLocationReporter = new DriverLocationReporter()
// Shared with the native reporter so a trip handed over to the APK shell keeps one sequence.
export { readSequence as readTripLocationSequence, writeSequence as writeTripLocationSequence }
