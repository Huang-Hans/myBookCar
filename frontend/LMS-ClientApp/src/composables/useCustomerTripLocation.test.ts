import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { TripLocationState } from '../../../shared/tripLocations/subscription'
import { useCustomerTripLocation } from './useCustomerTripLocation'

const mocks = vi.hoisted(() => ({ watch: vi.fn(), dispose: vi.fn() }))
vi.mock('@/api/notifications', () => ({
  watchCustomerTripLocation: mocks.watch,
}))

const point = { latitude: 22.198745, longitude: 113.543873, headingDegrees: 132, speedKmh: 36.4, seq: 1, recordedAt: '2026-10-19T01:12:05Z' }

describe('customer trip driver location', () => {
  beforeEach(() => { mocks.watch.mockReset(); mocks.dispose.mockReset(); mocks.watch.mockReturnValue({ dispose: mocks.dispose }) })

  it('watches one trip, updates from live GPS, and removes the position when sharing ends', () => {
    const location = useCustomerTripLocation()
    location.startWatching('trip_1')
    location.startWatching('trip_1')
    expect(mocks.watch).toHaveBeenCalledTimes(1)
    const receive = mocks.watch.mock.calls[0]?.[1] as (state: TripLocationState) => void
    receive({ tripId: 'trip_1', isSharing: true, location: point, errorCode: null, hasEnded: false })
    expect(location.driverLocation.value).toEqual(point)
    expect(location.isSharing.value).toBe(true)
    receive({ tripId: 'trip_1', isSharing: false, location: null, errorCode: null, hasEnded: true })
    expect(location.driverLocation.value).toBeNull()
    location.stopWatching()
    expect(mocks.dispose).toHaveBeenCalledOnce()
  })

  it('disposes the old watch before changing trips and ignores its late updates', () => {
    const location = useCustomerTripLocation()
    location.startWatching('trip_1')
    const oldReceive = mocks.watch.mock.calls[0]?.[1] as (state: TripLocationState) => void
    location.startWatching('trip_2')
    expect(mocks.dispose).toHaveBeenCalledOnce()
    oldReceive({ tripId: 'trip_1', isSharing: true, location: point, errorCode: null, hasEnded: false })
    expect(location.driverLocation.value).toBeNull()
    expect(mocks.watch.mock.calls[1]?.[0]).toBe('trip_2')
  })
})
