import { apiRequest, buildQuery, createIdempotencyKey } from './http'

export type AvailabilityCode = 'AVAILABLE' | 'BREAK' | 'OFF_DUTY' | 'ON_TRIP'
export type ProgressAction = 'START_EN_ROUTE' | 'ARRIVE_AT_PICKUP' | 'PICK_UP_PASSENGER' | 'START_TRANSIT' | 'ARRIVE_AT_DESTINATION' | 'COMPLETE_TRIP'
export type LanguageCode = 'EN' | 'ZH_HANT' | 'ZH_HANS'
export interface DriverSelf { driverId: string; name: string; propertyId: string; assignedVehicle: string; languages: string[]; status: 'ACTIVE' | 'INACTIVE' | 'SUSPENDED'; availability?: AvailabilityCode; availabilitySource?: string; syncedAt?: string; capabilities: { canChangeAvailability: boolean; canAcceptOffers: boolean }; version: number }
export interface DriverOffer { offerId: string; tripId: string; tripReference: string; status: string; expiresAt: string; createdAt: string; respondedAt?: string; serverTime: string; trip: { pickupAt: string; pickupName: string; dropoffName: string; passengers: number; vehicleClass: string }; version: number }
export interface DriverTripSummary { tripId: string; tripReference: string; serviceType: string; status: string; stage: string; vehicleClass: string; passengers: number; route: { pickupLocationId: string; pickupName: string; dropoffLocationId: string; dropoffName: string; isCrossBorder: boolean }; pickupAt: string; acceptedAt?: string; pickedUpAt?: string; droppedOffAt?: string; completedAt?: string; closedAt?: string; version: number }
export interface DriverTripDetail extends DriverTripSummary { passenger: { name: string; phoneMasked?: string }; specialRequests?: string; assignment: { offerId: string; vehicleLabel: string }; fulfillment: Record<string, string | undefined>; events: Array<{ eventId: string; type: string; status: string; occurredAt: string }>; allowedActions: ProgressAction[]; contact: { canContactPassenger: boolean }; cancellation?: { reasonCode: string; cancelledAt: string } }
export interface InspectionTemplate { phase: 'PRE_TRIP' | 'POST_TRIP'; version: number; items: Array<{ code: string; name: string }> }
export interface DriverPreferences { language: LanguageCode; updatedAt?: string; version: number }
export interface OperationsContacts { propertyId: string; isAvailable: boolean; contacts: Array<{ name: string; channel: string; value: string }> }
export interface DriverLocationPoint { seq: number; latitude: number; longitude: number; accuracyMeters?: number; headingDegrees?: number; speedKmh?: number; recordedAt: string }
export interface DriverLocationUpload { coordinateSystem: 'GCJ02'; points: DriverLocationPoint[] }
export interface DriverLocationUploadResult { acceptedSeq: number }
export interface DriverShift {
  shiftId?: string | null
  businessDate?: string | null
  period?: 'MORNING' | 'AFTERNOON' | 'NIGHT' | null
  startsAt?: string | null
  endsAt?: string | null
  endsNextDay?: boolean
  driverId?: string | null
  driverName?: string | null
  propertyId?: string | null
  vehiclePlate?: string | null
  status?: 'ON_DUTY' | 'REST' | 'LEAVE' | null
  role?: 'PREFERRED' | 'STANDBY' | null
  attendance?: 'SCHEDULED' | 'ON_TIME' | null
  badges?: string[] | null
  version?: number
}

export const driverApi = {
  getSelf: () => apiRequest<DriverSelf>('/api/dispatch/driver/v1/me'),
  listShiftsForDriver: (driverId: string, page = 1) => apiRequest<DriverShift[]>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}/shifts${buildQuery({ page, pageSize: 50, sort: '-businessDate,-shiftId' })}`, { canManageSession: false }),
  updateAvailability: (availability: Exclude<AvailabilityCode, 'ON_TRIP'>, version?: number) => apiRequest<DriverSelf>('/api/dispatch/driver/v1/me/availability', { method: 'PATCH', body: { availability }, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
  listOffers: () => apiRequest<DriverOffer[]>(`/api/dispatch/driver/v1/me/offers${buildQuery({ page: 1, pageSize: 10, sort: 'expiresAt,offerId' })}`),
  respondToOffer: (offerId: string, decision: 'ACCEPT' | 'DECLINE') => apiRequest<DriverOffer>(`/api/dispatch/driver/v1/me/offers/${encodeURIComponent(offerId)}/responses`, { method: 'POST', body: { decision } }),
  listTrips: (view: 'ACTIVE' | 'HISTORY', status?: 'COMPLETED' | 'CANCELLED', page = 1) => apiRequest<DriverTripSummary[]>(`/api/booking/driver/v1/me/trips${buildQuery({ view, status, page, pageSize: 20, sort: view === 'HISTORY' ? '-closedAt,-pickupAt,tripId' : 'pickupAt,tripId' })}`),
  getTrip: (tripId: string) => apiRequest<DriverTripDetail>(`/api/booking/driver/v1/me/trips/${encodeURIComponent(tripId)}`),
  progressTrip: (tripId: string, action: ProgressAction) => apiRequest<DriverTripDetail>(`/api/booking/driver/v1/me/trips/${encodeURIComponent(tripId)}/progress-events`, { method: 'POST', body: { action }, headers: { 'Idempotency-Key': createIdempotencyKey('progress') } }),
  reportLocations: (tripId: string, points: DriverLocationPoint[]) => apiRequest<DriverLocationUploadResult>(`/api/booking/driver/v1/me/trips/${encodeURIComponent(tripId)}/locations`, { method: 'POST', body: { coordinateSystem: 'GCJ02', points } satisfies DriverLocationUpload }),
  getInspectionTemplates: () => apiRequest<InspectionTemplate[]>('/api/dispatch/driver/v1/me/inspection-templates'),
  submitInspection: (phase: InspectionTemplate['phase'], answers: Array<{ itemCode: string; result: 'PASS' | 'FAIL' }>, templateVersion?: number) => apiRequest('/api/dispatch/driver/v1/me/inspections', { method: 'POST', body: { phase, templateVersion, answers }, headers: { 'Idempotency-Key': createIdempotencyKey('inspection') } }),
  reportVehicleIssue: (description: string, inspectionId?: string) => apiRequest('/api/dispatch/driver/v1/me/vehicle-issues', { method: 'POST', body: { description, inspectionId }, headers: { 'Idempotency-Key': createIdempotencyKey('issue') } }),
  getPreferences: () => apiRequest<DriverPreferences>('/api/dispatch/driver/v1/me/preferences'),
  updatePreferences: (language: LanguageCode, version?: number) => apiRequest<DriverPreferences>('/api/dispatch/driver/v1/me/preferences', { method: 'PATCH', body: { language }, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
  getOperationsContacts: () => apiRequest<OperationsContacts>('/api/dispatch/driver/v1/me/operations-contacts'),
  reportIncident: (type: 'VEHICLE_BREAKDOWN' | 'TRAFFIC_ACCIDENT' | 'PASSENGER_ASSISTANCE' | 'OTHER', occurredAt: string, description: string, tripId?: string) => apiRequest('/api/dispatch/driver/v1/me/incidents', { method: 'POST', body: { type, occurredAt, description, tripId }, headers: { 'Idempotency-Key': createIdempotencyKey('incident') } }),
}

export const availabilityToCode = (value: 'Available' | 'On Break' | 'Off Duty'): 'AVAILABLE' | 'BREAK' | 'OFF_DUTY' => value === 'On Break' ? 'BREAK' : value === 'Off Duty' ? 'OFF_DUTY' : 'AVAILABLE'
export const codeToAvailability = (value?: AvailabilityCode): 'Available' | 'On Break' | 'Off Duty' => value === 'BREAK' ? 'On Break' : value === 'OFF_DUTY' ? 'Off Duty' : 'Available'
