import { afterEach, describe, expect, it, vi } from 'vitest'
import type { TripLocation, TripTrack } from '../../../shared/tripLocations/contracts'
import { ApiRequestError } from './http'
import { TripTrackPoller, type TripTrackState } from './tripTrackPoller'
import { mergeTrackPoints } from './tripTrackMerge'
import { splitTrackSegments } from './tripTrackSegments'

const point = (seq: number, latitude = 22.2): TripLocation => ({
  seq, latitude, longitude: 113.5, headingDegrees: null, speedKmh: null,
  recordedAt: `2026-10-20T01:00:${String(seq).padStart(2, '0')}Z`,
})
const track = (points: TripLocation[], isSharing = true): TripTrack => ({ tripId: 'trip_1', points, isSharing, sampled: false })

afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks() })

describe('trip track polling', () => {
  it('loads history then merges incremental updates and stops on page hide', async () => {
    vi.useFakeTimers()
    const load = vi.fn().mockResolvedValueOnce(track([point(1), point(2)]))
      .mockResolvedValueOnce(track([point(3)]))
    const states: TripTrackState[] = []
    const poller = new TripTrackPoller('trip_1', load, (state) => states.push(state), () => false)
    poller.start()
    await vi.waitFor(() => expect(states[states.length - 1]?.points).toHaveLength(2))
    await vi.advanceTimersByTimeAsync(3_000)
    expect(load.mock.calls.map((call) => call[1])).toEqual([undefined, 2])
    expect(states[states.length - 1]?.points.map((item) => item.seq)).toEqual([1, 2, 3])
    poller.stop()
    await vi.advanceTimersByTimeAsync(6_000)
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('preserves the displayed route after a transient failure', async () => {
    vi.useFakeTimers()
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const load = vi.fn().mockResolvedValueOnce(track([point(1)]))
      .mockRejectedValueOnce(new ApiRequestError('Unavailable', 503, 50300, 'tr_1'))
      .mockResolvedValueOnce(track([point(2)], false))
    const states: TripTrackState[] = []
    const poller = new TripTrackPoller('trip_1', load, (state) => states.push(state), () => true)
    poller.start()
    await vi.waitFor(() => expect(states[states.length - 1]?.points).toHaveLength(1))
    await vi.advanceTimersByTimeAsync(3_000)
    expect(states[states.length - 1]?.points).toHaveLength(1)
    await vi.advanceTimersByTimeAsync(3_000)
    expect(states[states.length - 1]?.points.map((item) => item.seq)).toEqual([1, 2])
    expect(states[states.length - 1]?.isSharing).toBe(false)
    poller.stop()
  })

  it('hides an unauthorized track and does not retry', async () => {
    vi.useFakeTimers()
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
    const load = vi.fn().mockRejectedValue(new ApiRequestError('Forbidden', 403, 40300, 'tr_2'))
    const states: TripTrackState[] = []
    const poller = new TripTrackPoller('trip_1', load, (state) => states.push(state), () => false)
    poller.start()
    await vi.waitFor(() => expect(states[states.length - 1]?.isHidden).toBe(true))
    await vi.advanceTimersByTimeAsync(6_000)
    expect(load).toHaveBeenCalledTimes(1)
  })
})

describe('track drawing', () => {
  it('deduplicates sequence numbers and replaces sampled coverage', () => {
    expect(mergeTrackPoints([point(1), point(2), point(3)], [point(2), point(4)], false, 1)
      .map((item) => item.seq)).toEqual([1, 2, 3, 4])
    expect(mergeTrackPoints([point(1), point(2), point(3), point(4)], [point(2), point(4)], true, 1)
      .map((item) => item.seq)).toEqual([1, 2, 4])
  })
  it('does not connect distant GPS fixes with a fictitious line', () => {
    const segments = splitTrackSegments([point(1), point(2, 23.2)])
    expect(segments.map((segment) => segment.length)).toEqual([1, 1])
  })
})
