import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('./session', () => ({ api: mocks, isDemoMode: false }))
import { locationListPath, locationPatch, locationsApi } from './locations'
import type { LocationRecord } from './locations'

const record: LocationRecord = {
  locationId: 'loc_001', name: 'City of Dreams Lobby', address: 'Cotai', coordinates: { latitude: 22.149, longitude: 113.566 },
  region: 'COTAI', categoryCode: 'LC-004', contactPerson: 'Concierge desk', contactPhone: '+853 8868 8800',
  operatingHours: '24 hours', geofence: { isEnabled: true, radiusMeters: 100 }, isActive: true, notes: null, sortOrder: 2, version: 3,
}

describe('Portal locations API contract', () => {
  beforeEach(() => { mocks.request.mockReset(); mocks.requestEnvelope.mockReset(); mocks.request.mockResolvedValue(record); mocks.requestEnvelope.mockResolvedValue({ data: [record] }) })

  it('uses the documented list, detail and category routes', async () => {
    expect(locationListPath({ page: 2, pageSize: 10, region: 'COTAI', isActive: true, sort: '-name', search: 'Lobby' })).toBe('/api/platform/staff/v1/locations?page=2&pageSize=10&search=Lobby&region=COTAI&isActive=true&sort=-name')
    await locationsApi.list({ page: 1, pageSize: 10 })
    await locationsApi.detail('loc_001')
    await locationsApi.categories()
    expect(mocks.requestEnvelope).toHaveBeenCalledWith('/api/platform/staff/v1/locations?page=1&pageSize=10', {})
    expect(mocks.request).toHaveBeenCalledWith('/api/platform/staff/v1/locations/loc_001')
    expect(mocks.request).toHaveBeenCalledWith('/api/platform/staff/v1/location-categories')
  })

  it('sends only changed PATCH fields with the expected version', async () => {
    const changes = locationPatch(record, { ...record, name: 'New lobby', geofence: { isEnabled: false, radiusMeters: 100 } })
    expect(changes).toEqual({ name: 'New lobby', geofence: { isEnabled: false } })
    expect(locationPatch(record, { ...record, notes: '', name: 'New lobby' })).toEqual({ name: 'New lobby' })
    await locationsApi.update(record, changes)
    const [path, init] = mocks.request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/platform/staff/v1/locations/loc_001')
    expect(init.method).toBe('PATCH')
    expect(new Headers(init.headers).get('X-Expected-Version')).toBe('3')
    expect(JSON.parse(String(init.body))).toEqual(changes)
  })

  it('creates and reorders locations with the required request contracts', async () => {
    await locationsApi.create(record)
    await locationsApi.reorder('COTAI', [record])
    const [createPath, createInit] = mocks.request.mock.calls[0] as [string, RequestInit]
    const [orderPath, orderInit] = mocks.request.mock.calls[1] as [string, RequestInit]
    expect(createPath).toBe('/api/platform/staff/v1/locations')
    expect(new Headers(createInit.headers).get('Idempotency-Key')).toBeTruthy()
    expect(orderPath).toBe('/api/platform/staff/v1/location-orderings/COTAI')
    expect(orderInit.method).toBe('PUT')
    expect(JSON.parse(String(orderInit.body))).toEqual({ items: [{ locationId: 'loc_001', expectedVersion: 3 }] })
  })
})
