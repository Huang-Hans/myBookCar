import { beforeEach, describe, expect, it, vi } from 'vitest'
import { staffApi, type BookingInput } from './staff'
import { request } from './http'

vi.mock('./http', () => ({ request: vi.fn(), saveSession: vi.fn(), clearSession: vi.fn(), idempotencyKey: () => 'fixed-key', query: (values: Record<string, unknown>) => `?${new URLSearchParams(Object.entries(values).filter(([, value]) => value !== undefined).map(([key, value]) => [key, String(value)])).toString()}` }))
const mockedRequest = vi.mocked(request)
const booking: BookingInput = { guestId: 'g_1', propertyId: 'p_1', pickupLocationId: 'l_1', dropoffLocationId: 'l_2', pickupAt: '2026-10-16T06:30:00Z', pickupMode: 'SCHEDULED', serviceType: 'ONE_WAY', productCode: 'LIMOUSINE', vehicleClass: 'LUXURY_SEDAN', passengers: 2, luggage: 1, confirmationLanguage: 'ZH_HANT' }

describe('staff API contracts', () => {
  beforeEach(() => mockedRequest.mockReset())
  it('loads vehicle/service products from the booking-options API', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: { products: [], vehicles: [], isAsapAvailable: false }, traceId: 'tr_booking_options' })
    await staffApi.bookingOptions()
    expect(mockedRequest).toHaveBeenCalledWith('/api/booking/staff/v1/booking-options')
  })
  it('uses the booking preview and idempotent creation endpoints', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: { canSubmit: true }, traceId: 'tr_1' })
    await staffApi.previewBooking(booking)
    expect(mockedRequest).toHaveBeenCalledWith('/api/booking/staff/v1/booking-previews', { method: 'POST', body: {
      guestId: booking.guestId, propertyId: booking.propertyId, pickupLocationId: booking.pickupLocationId,
      dropoffLocationId: booking.dropoffLocationId, pickupAt: booking.pickupAt, pickupMode: booking.pickupMode,
      serviceType: booking.serviceType, productCode: booking.productCode, vehicleClass: booking.vehicleClass,
      passengers: booking.passengers, luggage: booking.luggage,
    } })
    await staffApi.createBooking(booking)
    expect(mockedRequest).toHaveBeenLastCalledWith('/api/booking/staff/v1/trips', { method: 'POST', body: booking, headers: { 'Idempotency-Key': 'fixed-key' } })
  })
  it('passes optimistic version and idempotency key for dispatch', async () => {
    mockedRequest.mockResolvedValue({ code: 20100, message: 'success', data: null, traceId: 'tr_2' })
    await staffApi.offerDriver('t_1', 'd_1', 4)
    expect(mockedRequest).toHaveBeenCalledWith('/api/dispatch/staff/v1/dispatch-orders/t_1/offers', { method: 'POST', body: { driverId: 'd_1' }, headers: { 'Idempotency-Key': 'fixed-key', 'X-Expected-Version': '4' } })
  })
  it('reads dispatch totals, detail, shift board and offer history from Dispatch API', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: [], traceId: 'tr_dispatch' })
    await staffApi.dispatchOrderSummaries()
    await staffApi.dispatchOrder('t/1')
    await staffApi.shiftBoard('2026-10-22T08:00:00Z', 'COD')
    await staffApi.dispatchOffers('t/1')
    expect(mockedRequest.mock.calls.map((call) => call[0])).toEqual([
      '/api/dispatch/staff/v1/dispatch-orders/summaries',
      '/api/dispatch/staff/v1/dispatch-orders/t%2F1',
      '/api/dispatch/staff/v1/shifts/board?pickupAt=2026-10-22T08%3A00%3A00Z&propertyId=COD',
      '/api/dispatch/staff/v1/dispatch-orders/t%2F1/offers?page=1&pageSize=20',
    ])
  })
  it('normalizes a non-UTC pickup timestamp to the UTC Z format required by the shift board', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: { periods: [] }, traceId: 'tr_shift_board' })
    await staffApi.shiftBoard('2026-10-22T08:00:00+08:00', 'COD')
    expect(mockedRequest).toHaveBeenCalledWith('/api/dispatch/staff/v1/shifts/board?pickupAt=2026-10-22T00%3A00%3A00Z&propertyId=COD')
    expect(() => staffApi.shiftBoard('invalid', 'COD')).toThrow('Trip pickup time is invalid.')
  })
  it('uses the console notification contract', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: [], traceId: 'tr_3' })
    await staffApi.notices(true)
    expect(mockedRequest).toHaveBeenCalledWith('/api/booking/staff/v1/console-notifications?page=1&pageSize=50&unread=true')
  })
  it('loads and fully replaces console settings', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: { station: 'Front Desk' }, traceId: 'tr_settings' })
    await staffApi.staffPreferences()
    expect(mockedRequest).toHaveBeenCalledWith('/api/booking/staff/v1/console-settings')
    await staffApi.updateConsoleSettings('Concierge', 'SMS', 'EN')
    expect(mockedRequest).toHaveBeenLastCalledWith('/api/booking/staff/v1/console-settings', { method: 'PUT', body: { station: 'Concierge', defaultNotificationChannel: 'SMS', confirmationLanguage: 'EN' } })
  })
  it('loads the staff trip track and normalizes nullable telemetry', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', traceId: 'tr_track', data: {
      tripId: 'trip_1', isSharing: true, sampled: false,
      points: [{ seq: 1, latitude: 22.2, longitude: 113.5, recordedAt: '2026-10-20T01:00:00Z' }],
    } })
    const track = await staffApi.tripTrack('trip_1', 5)
    expect(mockedRequest).toHaveBeenCalledWith('/api/booking/staff/v1/trips/trip_1/track?afterSeq=5')
    expect(track.points[0]).toMatchObject({ headingDegrees: null, speedKmh: null })
    expect(track.isSharing).toBe(true)
  })
  it('uses the staff location detail endpoint and propagates track failures', async () => {
    mockedRequest.mockResolvedValueOnce({ code: 20000, message: 'success', traceId: 'tr_location', data: {
      locationId: 'loc_1', name: 'Altira Macau', address: 'Macau', region: 'Macau', isActive: true,
      coordinates: { latitude: 22.2, longitude: 113.5 },
    } })
    const location = await staffApi.location('loc/1')
    expect(mockedRequest).toHaveBeenCalledWith('/api/platform/staff/v1/locations/loc%2F1')
    expect(location.data.coordinates?.latitude).toBe(22.2)
    mockedRequest.mockRejectedValueOnce(new Error('Forbidden'))
    await expect(staffApi.tripTrack('trip_1')).rejects.toThrow('Forbidden')
  })

  it('loads property details by ID for trip booking summaries', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', traceId: 'tr_property', data: { propertyId: 'p_1', name: 'Altira Macau' } })
    const property = await staffApi.property('p/1')
    expect(mockedRequest).toHaveBeenCalledWith('/api/platform/staff/v1/properties/p%2F1')
    expect(property.data.name).toBe('Altira Macau')
  })

  it('requests selectable catalogs with allowed page sizes and active locations', async () => {
    mockedRequest.mockResolvedValue({ code: 20000, message: 'success', data: [], traceId: 'tr_catalog' })
    await staffApi.properties(2)
    await staffApi.locations(2)
    expect(mockedRequest.mock.calls.map(([path]) => path)).toEqual([
      '/api/platform/staff/v1/properties?page=2&pageSize=50',
      '/api/platform/staff/v1/locations?page=2&pageSize=50&isActive=true',
    ])
  })
})
