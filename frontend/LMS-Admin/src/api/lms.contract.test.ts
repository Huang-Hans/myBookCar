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
    expect(request).toHaveBeenNthCalledWith(1, '/api/booking/staff/v1/guests')
    expect(request).toHaveBeenNthCalledWith(2, '/api/booking/staff/v1/trips')
  })

  it('sends the create-trip contract with an idempotency key', async () => {
    await lms.createTrip({ guestId: 'guest_1', propertyId: 'COD', pickupLocationId: 'loc_1', dropoffLocationId: 'loc_2', pickupAt: '2026-10-12T08:00:00Z', pickupMode: 'SCHEDULED', serviceType: 'ONE_WAY', vehicleClass: 'LUXURY_SEDAN', passengers: 1, luggage: 0, confirmationLanguage: 'EN' })
    const [path, init] = request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/booking/staff/v1/trips')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(init.body))).toMatchObject({ guestId: 'guest_1', pickupLocationId: 'loc_1' })
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
