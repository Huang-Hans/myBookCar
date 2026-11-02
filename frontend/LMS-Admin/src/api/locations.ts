import type { SuccessEnvelope } from './http'
import { api } from './session'

export interface LocationCoordinates { latitude: number; longitude: number }
export interface LocationGeofence { isEnabled: boolean; radiusMeters: number }
export interface LocationRecord {
  locationId: string
  name: string
  address: string
  coordinates: LocationCoordinates
  region: string
  categoryCode: string
  contactPerson: string
  contactPhone: string
  operatingHours: string
  geofence: LocationGeofence
  isActive: boolean
  notes?: string | null
  sortOrder: number
  version: number
}
export interface LocationCategory { categoryCode: string; name: string; description?: string | null; isActive: boolean }
export type LocationDraft = Omit<LocationRecord, 'locationId' | 'sortOrder' | 'version'>
export type LocationPatch = Partial<Omit<LocationDraft, 'coordinates' | 'geofence'>> & {
  coordinates?: Partial<LocationCoordinates>
  geofence?: Partial<LocationGeofence>
}

export interface LocationQuery {
  page: number
  pageSize: number
  search?: string
  region?: string
  isActive?: boolean
  sort?: string
}

const path = '/api/platform/staff/v1/locations'
export const locationRegions = ['MACAU_PENINSULA', 'COTAI', 'HONG_KONG', 'AIRPORT'] as const
export const locationRegionLabels: Readonly<Record<string, string>> = Object.fromEntries([
  ['MACAU_PENINSULA', 'Macau Peninsula'],
  ['COTAI', 'Cotai'],
  ['HONG_KONG', 'Hong Kong'],
  ['AIRPORT', 'Airport'],
])

export function locationListPath(query: LocationQuery): string {
  const parameters = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize) })
  if (query.search?.trim()) parameters.set('search', query.search.trim())
  if (query.region) parameters.set('region', query.region)
  if (query.isActive !== undefined) parameters.set('isActive', String(query.isActive))
  if (query.sort) parameters.set('sort', query.sort)
  return `${path}?${parameters}`
}

export function locationPatch(previous: LocationRecord, next: LocationDraft): LocationPatch {
  const changes: LocationPatch = {}
  for (const key of ['name', 'address', 'region', 'categoryCode', 'contactPerson', 'contactPhone', 'operatingHours', 'isActive'] as const) {
    if (previous[key] !== next[key]) Object.assign(changes, { [key]: next[key] ?? null })
  }
  if ((previous.notes ?? '') !== (next.notes ?? '')) changes.notes = next.notes?.trim() ? next.notes : null
  if (previous.coordinates.latitude !== next.coordinates.latitude || previous.coordinates.longitude !== next.coordinates.longitude) {
    changes.coordinates = {}
    if (previous.coordinates.latitude !== next.coordinates.latitude) changes.coordinates.latitude = next.coordinates.latitude
    if (previous.coordinates.longitude !== next.coordinates.longitude) changes.coordinates.longitude = next.coordinates.longitude
  }
  if (previous.geofence.isEnabled !== next.geofence.isEnabled || previous.geofence.radiusMeters !== next.geofence.radiusMeters) {
    changes.geofence = {}
    if (previous.geofence.isEnabled !== next.geofence.isEnabled) changes.geofence.isEnabled = next.geofence.isEnabled
    if (previous.geofence.radiusMeters !== next.geofence.radiusMeters) changes.geofence.radiusMeters = next.geofence.radiusMeters
  }
  return changes
}

export const locationsApi = {
  list: (query: LocationQuery, init: RequestInit = {}): Promise<SuccessEnvelope<LocationRecord[]>> => api.requestEnvelope<LocationRecord[]>(locationListPath(query), init),
  detail: (locationId: string) => api.request<LocationRecord>(`${path}/${encodeURIComponent(locationId)}`),
  categories: () => api.request<LocationCategory[]>('/api/platform/staff/v1/location-categories'),
  create: (draft: LocationDraft) => api.request<LocationRecord>(path, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify(draft) }),
  update: (previous: LocationRecord, changes: LocationPatch) => api.request<LocationRecord>(`${path}/${encodeURIComponent(previous.locationId)}`, { method: 'PATCH', headers: new Headers([['X-Expected-Version', String(previous.version)]]), body: JSON.stringify(changes) }),
  reorder: (region: string, items: LocationRecord[]) => api.request(`/api/platform/staff/v1/location-orderings/${encodeURIComponent(region)}`, { method: 'PUT', body: JSON.stringify({ items: items.map(({ locationId, version }) => ({ locationId, expectedVersion: version })) }) }),
}
