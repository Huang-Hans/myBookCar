import { beforeEach, describe, expect, it, vi } from 'vitest'

const { request, requestEnvelope } = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('./session', () => ({ api: { request, requestEnvelope } }))
import { dispatchApi } from './dispatch'

describe('Dispatch staff OpenAPI contract', () => {
  beforeEach(() => { request.mockReset().mockResolvedValue({}); requestEnvelope.mockReset().mockResolvedValue({ data: [] }) })

  it('lists coupons with the documented filters, reads summary/detail and creates with an idempotency key', async () => {
    await dispatchApi.coupons({ page: 2, search: 'WELCOME', type: 'FREE_TRIP', status: 'ACTIVE' })
    const url = new URL(String(requestEnvelope.mock.calls[0]?.[0]), 'https://local.example')
    expect(url.pathname).toBe('/api/dispatch/staff/v1/coupons')
    expect(Object.fromEntries(url.searchParams)).toMatchObject({ page: '2', pageSize: '20', search: 'WELCOME', type: 'FREE_TRIP', status: 'ACTIVE' })
    await dispatchApi.couponSummary()
    await dispatchApi.coupon('coupon/1')
    expect(request.mock.calls.map((call) => call[0])).toEqual(['/api/dispatch/staff/v1/coupons/summary', '/api/dispatch/staff/v1/coupons/coupon%2F1'])
    await dispatchApi.createCoupon({ code: 'WELCOME', name: 'Welcome', memberId: 'm_1', type: 'FREE_TRIP', faceAmount: 1, scope: 'ALL_PROPERTIES', validFrom: '2026-10-01', validTo: '2026-12-31', issuedQuantity: 100 })
    const [path, init] = request.mock.calls[2] as [string, RequestInit]
    expect(path).toBe('/api/dispatch/staff/v1/coupons')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(init.body))).toMatchObject({ memberId: 'm_1', faceAmount: 1 })
  })

  it('uses the documented vehicle list, summary, detail and creation shapes', async () => {
    await dispatchApi.vehicles({ page: 3, propertyId: 'COD', grade: 'LUXURY_MPV', status: 'AVAILABLE' })
    expect(requestEnvelope.mock.calls[0]?.[0]).toBe('/api/dispatch/staff/v1/vehicles?page=3&pageSize=20&propertyId=COD&grade=LUXURY_MPV&status=AVAILABLE')
    await dispatchApi.vehicleSummary('COD')
    await dispatchApi.vehicle('veh/1')
    await dispatchApi.createVehicle({ plate: 'MP-28-61', model: 'Alphard', grade: 'LUXURY_MPV', propertyId: 'COD', seats: 6, mileageKm: 100, status: 'AVAILABLE' })
    expect(request.mock.calls[0]?.[0]).toBe('/api/dispatch/staff/v1/vehicles/summary?propertyId=COD')
    expect(request.mock.calls[1]?.[0]).toBe('/api/dispatch/staff/v1/vehicles/veh%2F1')
    const [path, init] = request.mock.calls[2] as [string, RequestInit]
    expect(path).toBe('/api/dispatch/staff/v1/vehicles')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(init.body))).toMatchObject({ grade: 'LUXURY_MPV', mileageKm: 100 })
  })

  it('loads the shift board and sends create, assign and clear operations to separate routes', async () => {
    await dispatchApi.shiftBoard('2026-10-19', 'COD')
    expect(request.mock.calls[0]?.[0]).toBe('/api/dispatch/staff/v1/shifts/board?Date=2026-10-19&PropertyId=COD')
    await dispatchApi.createShift({ businessDate: '2026-10-19', driverId: 'drv_1', propertyId: 'COD', period: 'MORNING', status: 'ON_DUTY', role: 'PREFERRED' })
    const [createPath, createInit] = request.mock.calls[1] as [string, RequestInit]
    expect(createPath).toBe('/api/dispatch/staff/v1/shifts')
    expect(new Headers(createInit.headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(createInit.body))).toMatchObject({ businessDate: '2026-10-19', period: 'MORNING' })
    await dispatchApi.assignShiftVehicle('shift/1', 'MP-28-61')
    expect(request.mock.calls[2]?.[0]).toBe('/api/dispatch/staff/v1/shifts/shift%2F1/vehicle')
    expect(request.mock.calls[2]?.[1]).toMatchObject({ method: 'PUT', body: JSON.stringify({ vehiclePlate: 'MP-28-61' }) })
    await dispatchApi.clearShiftVehicle('shift/1')
    expect(request.mock.calls[3]).toEqual(['/api/dispatch/staff/v1/shifts/shift%2F1/vehicle', { method: 'DELETE' }])
  })
})
