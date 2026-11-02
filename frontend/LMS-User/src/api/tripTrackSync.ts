import type { TripLocation, TripTrack } from '../../../shared/tripLocations/contracts'
import { ApiRequestError } from './http'
import { mergeTrackPoints } from './tripTrackMerge'

export interface TripTrackState {
  points: TripLocation[]
  isSharing: boolean
  sampled: boolean
  errorCode: number | null
  loaded: boolean
}

export type TripTrackLoader = (tripId: string, afterSeq: number) => Promise<TripTrack>

/** Minimum spacing between two catch-up requests triggered by sequence gaps or failures. */
const CATCH_UP_INTERVAL_MS = 10_000
/** Responses after which the staff member is not allowed to see the track, so no retry is useful. */
const TERMINAL_ERROR_CODES: readonly number[] = [40001, 40300, 40400]

/**
 * Keeps the driven track of one trip: history from the track endpoint merged with live points by `seq`.
 * `contiguousSeq` is the highest sequence below which no point is known to be missing.
 */
export class TripTrackSync {
  private points: TripLocation[] = []
  private contiguousSeq = 0
  private isSharing = false
  private sampled = false
  private ended = false
  private loaded = false
  private errorCode: number | null = null
  private blocked = false
  private disposed = false
  private inFlight = false
  private forceRerun = false
  private lastFetchAt = 0
  private timer: ReturnType<typeof setTimeout> | null = null

  constructor(private readonly tripId: string, private readonly loader: TripTrackLoader,
    private readonly onState: (state: TripTrackState) => void, private readonly now: () => number = Date.now) {}

  start(): void {
    this.publish()
    this.schedule(true)
  }

  /** Re-reads everything after the contiguous sequence, e.g. once the live connection is back. */
  refill(): void {
    this.schedule(true)
  }

  receiveLocation(location: TripLocation): void {
    if (this.disposed || this.blocked || location.seq <= this.maxSeq) return
    const isNextPoint = location.seq === this.maxSeq + 1 && this.contiguousSeq === this.maxSeq
    this.points.push(location)
    if (isNextPoint) this.contiguousSeq = location.seq
    this.isSharing = !this.ended
    this.publish()
    if (!isNextPoint) this.schedule(false)
  }

  markEnded(): void {
    this.ended = true
    this.isSharing = false
    this.publish()
    this.schedule(true)
  }

  dispose(): void {
    this.disposed = true
    this.clearTimer()
  }

  private get maxSeq(): number {
    return this.points.length > 0 ? this.points[this.points.length - 1]!.seq : 0
  }

  private schedule(immediate: boolean): void {
    if (this.disposed || this.blocked) return
    if (this.inFlight) {
      this.forceRerun = this.forceRerun || immediate
      return
    }
    const waitMs = immediate ? 0 : this.lastFetchAt + CATCH_UP_INTERVAL_MS - this.now()
    if (waitMs <= 0) { void this.fetch(); return }
    if (!this.timer) this.timer = setTimeout(() => { this.timer = null; void this.fetch() }, waitMs)
  }

  private async fetch(): Promise<void> {
    this.clearTimer()
    this.inFlight = true
    this.forceRerun = false
    this.lastFetchAt = this.now()
    const afterSeq = this.contiguousSeq
    let succeeded = false
    try {
      const track = await this.loader(this.tripId, afterSeq)
      if (this.disposed) return
      this.applyTrack(track, afterSeq)
      succeeded = true
    } catch (error) {
      if (!this.disposed) this.handleFailure(error)
    } finally {
      this.inFlight = false
    }
    if (!this.disposed) this.continueAfterFetch(succeeded)
  }

  private continueAfterFetch(succeeded: boolean): void {
    if (this.blocked) return
    if (!succeeded) { this.schedule(false); return }
    if (this.forceRerun) this.schedule(true)
    else if (this.contiguousSeq < this.maxSeq) this.schedule(false)
  }

  private applyTrack(track: TripTrack, afterSeq: number): void {
    this.points = mergeTrackPoints(this.points, track.points, track.sampled, afterSeq)
    this.sampled = this.sampled || track.sampled
    this.isSharing = track.isSharing && !this.ended
    this.errorCode = null
    this.loaded = true
    this.contiguousSeq = Math.max(this.contiguousSeq, afterSeq, ...track.points.map((point) => point.seq))
    this.absorbConsecutivePoints()
    this.publish()
  }

  private absorbConsecutivePoints(): void {
    for (const point of this.points) {
      if (point.seq === this.contiguousSeq + 1) this.contiguousSeq = point.seq
    }
  }

  private handleFailure(error: unknown): void {
    const code = error instanceof ApiRequestError ? error.code : null
    console.error(JSON.stringify({ event: 'tripTrackLoadFailed', tripId: this.tripId, code, message: error instanceof Error ? error.message : 'Unknown track error' }))
    if (code === null || !TERMINAL_ERROR_CODES.includes(code)) return
    this.blocked = true
    this.errorCode = code
    this.publish()
  }

  private clearTimer(): void {
    if (this.timer) clearTimeout(this.timer)
    this.timer = null
  }

  private publish(): void {
    this.onState({ points: this.points.slice(), isSharing: this.isSharing, sampled: this.sampled, errorCode: this.errorCode, loaded: this.loaded })
  }
}
