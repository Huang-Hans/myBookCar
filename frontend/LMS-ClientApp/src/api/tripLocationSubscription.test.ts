import { describe, expect, it, vi } from 'vitest'
import { TripLocationSubscription, type TripLocationConnection, type TripLocationState } from '../../../shared/tripLocations/subscription'
import type { TripLocationMessage } from '../../../shared/tripLocations/contracts'

const point = (seq: number) => ({ latitude: 22.198745, longitude: 113.543873 + seq / 10000, headingDegrees: 132, speedKmh: 36.4, seq, recordedAt: '2026-10-19T01:12:05Z' })

describe('customer WatchTrip lifecycle', () => {
  it('uses the snapshot, accepts newer pushes, re-watches after reconnect, and unwatches on close', async () => {
    let onMessage: ((message: TripLocationMessage) => void) | undefined
    let onConnected: (() => void) | undefined
    const invoke = vi.fn(async (method: string) => method === 'WatchTrip'
      ? { code: 20000, message: 'success', traceId: 'tr_1', data: { tripId: 'trip_1', isSharing: true, location: point(1) } }
      : { code: 20000, message: 'success', traceId: 'tr_2', data: null })
    const connection: TripLocationConnection = {
      isConnected: true,
      invoke: <T>(method: string): Promise<T> => invoke(method) as Promise<T>,
      reconnect: vi.fn(async () => {}),
      onConnected: (handler) => { onConnected = handler; return () => { onConnected = undefined } },
      subscribeTripLocations: (handler) => { onMessage = handler; return () => { onMessage = undefined } },
    }
    const states: TripLocationState[] = []
    const subscription = new TripLocationSubscription(connection, 'trip_1', (state) => states.push(state))
    await vi.waitFor(() => expect(states[states.length - 1]?.location?.seq).toBe(1))
    onMessage?.({ code: 20000, message: 'success', traceId: 'tr_3', data: { schemaVersion: 1, eventType: 'TRIP_LOCATION_UPDATED', tripId: 'trip_1', location: point(2) } })
    onMessage?.({ code: 20000, message: 'success', traceId: 'tr_4', data: { schemaVersion: 1, eventType: 'TRIP_LOCATION_UPDATED', tripId: 'trip_1', location: point(1) } })
    expect(states[states.length - 1]?.location?.seq).toBe(2)
    onConnected?.()
    await vi.waitFor(() => expect(invoke.mock.calls.filter(([method]) => method === 'WatchTrip')).toHaveLength(2))
    subscription.dispose()
    await vi.waitFor(() => expect(invoke.mock.calls.some(([method]) => method === 'UnwatchTrip')).toBe(true))
  })

  it('hides the driver marker when the watch is rejected', async () => {
    const invoke = vi.fn(async () => ({ code: 40400, message: 'Trip not found', traceId: 'tr_missing', data: null }))
    const connection: TripLocationConnection = {
      isConnected: true,
      invoke: <T>(): Promise<T> => invoke() as Promise<T>,
      reconnect: vi.fn(async () => {}),
      onConnected: () => () => {},
      subscribeTripLocations: () => () => {},
    }
    const states: TripLocationState[] = []
    new TripLocationSubscription(connection, 'trip_missing', (state) => states.push(state))
    await vi.waitFor(() => expect(states[states.length - 1]?.errorCode).toBe(40400))
    expect(states[states.length - 1]?.location).toBeNull()
  })
})
