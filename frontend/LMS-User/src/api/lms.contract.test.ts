import { beforeEach, describe, expect, it, vi } from 'vitest'

const { request, requestEnvelope } = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('./session', () => ({ api: { request, requestEnvelope } }))
import { lms } from './lms'

describe('LMS gateway call sites', () => {
  beforeEach(() => { request.mockReset(); request.mockResolvedValue({}); requestEnvelope.mockReset(); requestEnvelope.mockResolvedValue({ data: [], meta: { pagination: { totalItems: 0 } } }) })

  it('sends Portal driver filters and paging to the documented Dispatch endpoint', async () => {
    await lms.listDrivers({ page: 2, pageSize: 20, propertyId: 'COD', status: 'ACTIVE', view: 'AVAILABLE', search: 'Alex', sort: '-name' })
    const [path, init] = requestEnvelope.mock.calls[0] as [string, RequestInit]
    const url = new URL(path, 'https://local.example')
    expect(url.pathname).toBe('/api/dispatch/staff/v1/drivers')
    expect(Object.fromEntries(url.searchParams)).toMatchObject({ page: '2', pageSize: '20', search: 'Alex', propertyId: 'COD', status: 'ACTIVE', view: 'AVAILABLE', sort: '-name' })
    expect(init).toEqual({})
    await lms.driver('drv/1')
    expect(request).toHaveBeenCalledWith('/api/dispatch/staff/v1/drivers/drv%2F1')
  })

  it('uses the approval queue, summary and detail routes without substituting qualification references', async () => {
    const query = { page: 1, pageSize: 10, propertyId: 'COD', status: 'PENDING', from: '2026-10-11T16:00:00Z', to: '2026-10-12T16:00:00Z' }
    await lms.listApprovals(query)
    expect(requestEnvelope.mock.calls[0]?.[0]).toContain('/api/booking/staff/v1/cross-border-approvals?page=1&pageSize=10')
    await lms.approvalSummary(query)
    expect(request.mock.calls[0]?.[0]).toContain('/api/booking/staff/v1/cross-border-approval-summaries?')
    await lms.approval('approval_1')
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/cross-border-approvals/approval_1')
  })

  it('reads guests and bookings from versioned Booking endpoints', async () => {
    await lms.guests(); await lms.trips()
    expect(request).toHaveBeenNthCalledWith(1, '/api/booking/staff/v1/guests?page=1&pageSize=50')
    expect(request).toHaveBeenNthCalledWith(2, '/api/booking/staff/v1/trips?page=1&pageSize=20')
  })

  it('sends console menu filters on the staff routes from the console sheet', async () => {
    await lms.searchGuests({ search: '1808', tier: 'Diamond' })
    expect(request).toHaveBeenNthCalledWith(1, '/api/booking/staff/v1/guests?page=1&pageSize=50&search=1808&tier=Diamond')
    await lms.listGuestPage({ search: 'MC-1', tier: 'Gold', page: 2, pageSize: 20 })
    expect(requestEnvelope.mock.calls[0]?.[0]).toBe('/api/booking/staff/v1/guests?page=2&pageSize=20&search=MC-1&tier=Gold')
    await lms.listTripPage({ from: '2026-10-13T16:00:00.000Z', to: '2026-10-14T16:00:00.000Z', page: 2, pageSize: 50 })
    expect(requestEnvelope.mock.calls[1]?.[0]).toBe('/api/booking/staff/v1/trips?page=2&pageSize=50&from=2026-10-13T16%3A00%3A00.000Z&to=2026-10-14T16%3A00%3A00.000Z')
    await lms.listTrips({ search: 'TR-1', propertyId: 'COD', status: 'CONFIRMED', from: '2026-10-11T16:00:00.000Z', to: '2026-10-12T16:00:00.000Z' })
    expect(request.mock.calls[1]?.[0]).toBe('/api/booking/staff/v1/trips?page=1&pageSize=20&search=TR-1&propertyId=COD&status=CONFIRMED&from=2026-10-11T16%3A00%3A00.000Z&to=2026-10-12T16%3A00%3A00.000Z')
    await lms.trip('trip/1'); await lms.tripEvents('trip/1'); await lms.cancellationPolicy('trip/1')
    expect(request.mock.calls.slice(2, 5).map((call) => call[0])).toEqual([
      '/api/booking/staff/v1/trips/trip%2F1',
      '/api/booking/staff/v1/trips/trip%2F1/events',
      '/api/booking/staff/v1/trips/trip%2F1/cancellation-policy',
    ])
    await lms.tripStatusSummaries({ search: 'TR-1', propertyId: 'COD' })
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/trip-status-summaries?search=TR-1&propertyId=COD')
    await lms.notifications({ type: 'BOOKING_CONFIRMED', unread: true })
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/console-notifications?page=1&pageSize=50&type=BOOKING_CONFIRMED&unread=true')
    await lms.unreadNotificationCount()
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/console-notifications/unread-count')
    await lms.locationFavorites()
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/location-favorites')
    await lms.location('loc/1')
    expect(request).toHaveBeenLastCalledWith('/api/platform/staff/v1/locations/loc%2F1')
  })

  it('sends the create-trip contract with an idempotency key', async () => {
    await lms.createTrip({ guestId: 'guest_1', propertyId: 'COD', pickupLocationId: 'loc_1', dropoffLocationId: 'loc_2', pickupAt: '2026-10-12T08:00:00Z', pickupMode: 'SCHEDULED', serviceType: 'ONE_WAY', vehicleClass: 'LUXURY_SEDAN', passengers: 1, luggage: 0, specialRequests: 'Coupon WELCOME', confirmationLanguage: 'EN', couponId: 'coupon_1' }, 'idem-1')
    const [path, init] = request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/booking/staff/v1/trips')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBe('idem-1')
    expect(JSON.parse(String(init.body))).toMatchObject({ guestId: 'guest_1', pickupLocationId: 'loc_1', specialRequests: 'Coupon WELCOME', couponId: 'coupon_1' })
  })

  it('sends a driver offer to the dispatch order', async () => {
    await lms.createDispatchOffer('trip/1', 'drv_1', 0)
    const [path, init] = request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/dispatch/staff/v1/dispatch-orders/trip%2F1/offers')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy()
    expect(new Headers(init.headers).get('X-Expected-Version')).toBe('0')
    expect(JSON.parse(String(init.body))).toEqual({ driverId: 'drv_1' })
    await lms.listDispatchCandidates('trip/1', 2)
    expect(requestEnvelope).toHaveBeenLastCalledWith('/api/dispatch/staff/v1/dispatch-orders/trip%2F1/drivers?page=2&pageSize=50')
    await lms.listDispatchOffers('trip/1', 2)
    expect(requestEnvelope).toHaveBeenLastCalledWith('/api/dispatch/staff/v1/dispatch-orders/trip%2F1/offers?page=2&pageSize=20')
  })

  it('loads dispatch summaries with the same non-status filters as the queue', async () => {
    await lms.dispatchOrderSummaries({ search: 'TR-1', propertyId: 'COD', channel: 'SELF_SERVICE' })
    expect(request).toHaveBeenCalledWith('/api/dispatch/staff/v1/dispatch-orders/summaries?search=TR-1&propertyId=COD&channel=SELF_SERVICE')
  })

  it('sends a partial driver update and uses the notification read endpoint', async () => {
    const previous = { driverId: 'drv_1', version: 4, fullName: 'Alex', licenseNumber: 'MAC-1', licenseExpiryDate: '2027-01-01', phoneMasked: '+8531234', propertyName: 'City of Dreams', vehicleName: 'MPV', availability: 'Available' as const, status: 'Active' as const, emergencyContact: 'Contact', emergencyContactPhone: '+8535678', languagesSpoken: 'English', assignedProperties: ['City of Dreams'], blacklistReason: '', blacklistEffectiveDate: '', blacklistExpiryDate: '' }
    await lms.updateDriver({ ...previous, status: 'Inactive' }, previous)
    const [path, init] = request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/dispatch/staff/v1/drivers/drv_1')
    expect(init.method).toBe('PATCH')
    expect(new Headers(init.headers).get('X-Expected-Version')).toBe('4')
    expect(JSON.parse(String(init.body))).toEqual({ status: 'INACTIVE' })
    await lms.markRead('notice_1')
    expect(request).toHaveBeenLastCalledWith('/api/booking/staff/v1/console-notifications/notice_1/read', { method: 'POST' })
  })
})
