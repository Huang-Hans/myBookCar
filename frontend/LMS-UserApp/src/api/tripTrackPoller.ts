import type { TripLocation, TripTrack } from '../../../shared/tripLocations/contracts'
import { ApiRequestError } from './http'
import { mergeTrackPoints } from './tripTrackMerge'

export interface TripTrackState {
  points: TripLocation[]
  isSharing: boolean
  sampled: boolean
  isLoaded: boolean
  isHidden: boolean
}

export class TripTrackPoller {
  private state: TripTrackState = { points: [], isSharing: false, sampled: false, isLoaded: false, isHidden: false }
  private timer: ReturnType<typeof setTimeout> | null = null
  private generation = 0
  private isRunning = false

  constructor(
    private readonly tripId: string,
    private readonly load: (tripId: string, afterSeq?: number) => Promise<TripTrack>,
    private readonly publish: (state: TripTrackState) => void,
    private readonly isTerminal: () => boolean,
  ) {}

  start(): void {
    if (this.isRunning) return
    this.isRunning = true
    this.publishState()
    void this.fetch(++this.generation)
  }

  stop(): void {
    this.isRunning = false
    this.generation++
    if (this.timer) clearTimeout(this.timer)
    this.timer = null
  }

  private async fetch(generation: number): Promise<void> {
    const afterSeq = this.state.points[this.state.points.length - 1]?.seq
    try {
      const track = await this.load(this.tripId, afterSeq)
      if (generation !== this.generation) return
      this.state = {
        points: mergeTrackPoints(this.state.points, track.points, track.sampled, afterSeq ?? 0),
        isSharing: track.isSharing,
        sampled: this.state.sampled || track.sampled,
        isLoaded: true,
        isHidden: false,
      }
      this.publishState()
    } catch (error) {
      if (generation !== this.generation) return
      const code = error instanceof ApiRequestError ? error.code : null
      console.error(JSON.stringify({ event: 'tripTrackLoadFailed', tripId: this.tripId, code,
        traceId: error instanceof ApiRequestError ? error.traceId : 'unavailable' }))
      if (code === 40300 || code === 40400 || code === 40001) {
        this.state = { points: [], isSharing: false, sampled: false, isLoaded: true, isHidden: true }
        this.publishState()
        this.stop()
        return
      }
    }
    if (!this.isRunning || this.isTerminal() && this.state.isLoaded && !this.state.isSharing) return
    this.timer = setTimeout(() => { this.timer = null; void this.fetch(this.generation) }, 3_000)
  }

  private publishState(): void {
    this.publish({ ...this.state, points: [...this.state.points] })
  }
}
