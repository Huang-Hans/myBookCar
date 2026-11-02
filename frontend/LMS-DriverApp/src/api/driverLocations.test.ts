import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiRequestError } from './http'
import { canReportTripLocation, captureDriverLocation, DriverLocationReporter, LOCATION_REPORT_INTERVAL_MS, type LocationSample } from './driverLocations'
import type { DriverLocationPoint, DriverLocationUploadResult } from './driver'
import { wgs84ToGcj02 } from '../data/amap'

const location: LocationSample = {
  latitude: 22.198745,
  longitude: 113.543873,
  accuracyMeters: 8.5,
  headingDegrees: 132,
  speedKmh: 36.4,
  recordedAt: '2026-10-19T01:12:05.120Z',
}

function createReporter(upload = vi.fn(async (_tripId: string, points: DriverLocationPoint[]): Promise<DriverLocationUploadResult> => ({ acceptedSeq: points[points.length - 1]?.seq ?? 0 }))) {
  const sequence = new Map<string, number>()
  const capture = vi.fn(async () => location)
  const reporter = new DriverLocationReporter({
    capture,
    upload,
    readSequence: (tripId) => sequence.get(tripId) ?? 0,
    writeSequence: (tripId, seq) => { sequence.set(tripId, seq) },
  })
  return { reporter, capture, upload, sequence }
}

beforeEach(() => { vi.useFakeTimers(); vi.setSystemTime(new Date('2026-10-19T01:12:05Z')) })
afterEach(() => { vi.useRealTimers(); vi.restoreAllMocks() })

describe('driver location reporter', () => {
  it('only permits an active trip from departure through arrival', () => {
    expect(LOCATION_REPORT_INTERVAL_MS).toBe(3_000)
    expect(canReportTripLocation(null)).toBe(false)
    expect(canReportTripLocation({ tripId: 'trip_1', status: 'ACCEPTED', stage: 'ASSIGNED' })).toBe(false)
    expect(canReportTripLocation({ tripId: 'trip_1', status: 'IN_PROGRESS', stage: 'DRIVER_EN_ROUTE' })).toBe(true)
    expect(canReportTripLocation({ tripId: 'trip_1', status: 'IN_PROGRESS', stage: 'ARRIVED_AT_DESTINATION' })).toBe(true)
    expect(canReportTripLocation({ tripId: 'trip_1', status: 'COMPLETED', stage: 'COMPLETED' })).toBe(false)
    expect(canReportTripLocation({ tripId: 'trip_1', status: 'CANCELLED', stage: 'IN_TRANSIT' })).toBe(false)
  })

  it('uploads immediately and then every three seconds with a persistent increasing seq', async () => {
    const { reporter, capture, upload, sequence } = createReporter()
    reporter.start('trip_1')
    await vi.advanceTimersByTimeAsync(0)
    expect(upload).toHaveBeenCalledWith('trip_1', [{ ...location, seq: 1 }])

    await vi.advanceTimersByTimeAsync(LOCATION_REPORT_INTERVAL_MS - 1)
    expect(capture).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(1)
    expect(upload).toHaveBeenLastCalledWith('trip_1', [{ ...location, seq: 2 }])
    expect(sequence.get('trip_1')).toBe(2)

    reporter.pause()
    await vi.advanceTimersByTimeAsync(LOCATION_REPORT_INTERVAL_MS)
    expect(capture).toHaveBeenCalledTimes(2)
    reporter.start('trip_1')
    await vi.advanceTimersByTimeAsync(0)
    expect(upload).toHaveBeenLastCalledWith('trip_1', [{ ...location, seq: 3 }])
    reporter.endTrip()
  })

  it('retries unsent points together with the next fix after a network failure', async () => {
    const upload = vi.fn()
      .mockRejectedValueOnce(new ApiRequestError('Network unavailable', 0, 50000, 'tr_offline'))
      .mockResolvedValueOnce({ acceptedSeq: 2 })
    const { reporter } = createReporter(upload)
    vi.spyOn(console, 'error').mockImplementation(() => {})
    reporter.start('trip_2')
    await vi.advanceTimersByTimeAsync(0)
    expect(upload).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(LOCATION_REPORT_INTERVAL_MS)
    expect(upload).toHaveBeenLastCalledWith('trip_2', [{ ...location, seq: 1 }, { ...location, seq: 2 }])
    reporter.endTrip()
  })

  it('continues the stored trip sequence after a reporter is recreated', async () => {
    const sequence = new Map<string, number>()
    const upload = vi.fn(async (_tripId: string, points: DriverLocationPoint[]) => ({ acceptedSeq: points[points.length - 1]?.seq ?? 0 }))
    const dependencies = {
      capture: async () => location,
      upload,
      readSequence: (tripId: string) => sequence.get(tripId) ?? 0,
      writeSequence: (tripId: string, seq: number) => { sequence.set(tripId, seq) },
    }
    const first = new DriverLocationReporter(dependencies)
    first.start('trip_4')
    await vi.advanceTimersByTimeAsync(0)
    first.endTrip()

    const restarted = new DriverLocationReporter(dependencies)
    restarted.start('trip_4')
    await vi.advanceTimersByTimeAsync(0)
    expect(upload).toHaveBeenLastCalledWith('trip_4', [{ ...location, seq: 2 }])
    restarted.endTrip()
  })

  it('stops reporting on a non-shareable trip and does not restart that trip', async () => {
    const upload = vi.fn().mockRejectedValue(new ApiRequestError('Trip is not sharing', 409, 40900, 'tr_conflict'))
    const { reporter, capture } = createReporter(upload)
    reporter.start('trip_3')
    await vi.advanceTimersByTimeAsync(0)
    reporter.start('trip_3')
    await vi.advanceTimersByTimeAsync(LOCATION_REPORT_INTERVAL_MS)
    expect(capture).toHaveBeenCalledTimes(1)
    expect(upload).toHaveBeenCalledTimes(1)
    reporter.endTrip()
  })

  it('reads a native GCJ-02 fix without converting it twice', async () => {
    vi.stubGlobal('uni', { getLocation: ({ success }: { success: (value: object) => void }) => success({ latitude: 22.2, longitude: 113.5, accuracy: 8, heading: 90, speed: 10 }) })
    const sample = await captureDriverLocation()
    expect(sample).toMatchObject({ latitude: 22.2, longitude: 113.5, accuracyMeters: 8, headingDegrees: 90, speedKmh: 36 })
    expect(sample?.recordedAt).toMatch(/Z$/)
    vi.unstubAllGlobals()
  })

  it('converts browser WGS-84 GPS fixes to GCJ-02', async () => {
    vi.stubEnv('UNI_PLATFORM', 'h5')
    vi.resetModules()
    const { captureDriverLocation: captureBrowserLocation } = await import('./driverLocations')
    vi.stubGlobal('navigator', { geolocation: { getCurrentPosition: (success: (position: object) => void) => success({
      coords: { latitude: 22.198745, longitude: 113.543873, accuracy: 8.5, heading: 132, speed: 10 },
      timestamp: Date.parse('2026-10-19T01:12:05.120Z'),
    }) } })
    const sample = await captureBrowserLocation()
    const [longitude, latitude] = wgs84ToGcj02([113.543873, 22.198745])
    expect(sample).toMatchObject({ latitude, longitude, accuracyMeters: 8.5, headingDegrees: 132, speedKmh: 36, recordedAt: '2026-10-19T01:12:05.120Z' })
    vi.unstubAllEnvs()
    vi.unstubAllGlobals()
  })
})
