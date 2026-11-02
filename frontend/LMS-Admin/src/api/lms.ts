import type { Approval, Booking, BookingStatus, Driver, DriverAvailability, DriverStatus, Guest, Location, Notice } from '../data/demo'
import { api } from './session'
import type { SuccessEnvelope } from './http'
import type { VehicleOption } from '../../../shared/vehicles/contracts'

export interface DriverView { driverId: string; name: string; licenseNumber: string; licenseExpiryDate: string; phone: string; propertyId: string; assignedVehicle: string; assignedVehicleId?: string | null; status: string; languages: string[]; emergencyContact?: { name?: string; phone?: string } | null; availability?: string | null; restriction?: { reason?: string; effectiveDate?: string; expiryDate?: string } | null; version: number }
export interface GuestSummaryView { guestId: string; name: string; roomNumber?: string | null; memberId?: string | null; tier?: string | null; inHouse: boolean }
export interface GuestProfileView extends GuestSummaryView { propertyId?: string | null; phoneMasked?: string | null; blacklistStatus?: { listed: boolean; reason?: string | null } | null; preferences?: { language?: string | null; preferredVehicleClass?: string | null } | null }
export interface TripView { tripId: string; tripReference: string; propertyId: string; guest: { guestId: string; name: string; tierCode?: string | null; roomNumber?: string | null }; route: { pickupLocationId: string; pickupName: string; dropoffLocationId: string; dropoffName: string }; pickupAt: string; serviceType: string; vehicleClass: string; status: string; points?: { required: number } | null; bookedBy?: { userId?: string } | null; cancellation?: { reasonCode?: string } | null; version: number }
export interface LocationView { locationId: string; name: string; categoryCode: string; address: string; region: string; isActive: boolean; version: number }
export interface ConsoleNotificationView { notificationId: string; type: string; summary: string; tripReference?: string | null; occurredAt: string; isRead: boolean }
export interface CrossBorderApprovalView { approvalId: string; tripReference: string; guest: { guestId: string; name: string; tierCode: string }; propertyId: string; route: { pickupName?: string; dropoffName?: string }; pickupAt: string; slaDeadlineAt?: string | null; documents: { documentType: string; isVerified: boolean }[]; status: string; version: number; decision?: { comments?: string } | null }
export interface QualificationView { qualificationId: string; guest: { guestId: string; name: string; tierCode: string }; propertyId: string; status: string; justification?: string | null; operatorId?: string | null; operatedAt?: string | null; version: number }
export interface CreateTripRequest { guestId: string; propertyId: string; pickupLocationId: string; dropoffLocationId: string; pickupAt: string; pickupMode: 'SCHEDULED'; serviceType: 'ONE_WAY' | 'AIRPORT_PICKUP' | 'AIRPORT_DROPOFF' | 'INTER_PROPERTY'; vehicleClass: 'BUSINESS_SEDAN' | 'LUXURY_SEDAN' | 'LUXURY_MPV' | 'MAYBACH'; passengers: number; luggage: number; confirmationLanguage: 'EN' | 'ZH_HANT' }
export interface StaffPreferencesView { station: string; defaultNotificationChannel: string; confirmationLanguage: string; enabledNotificationTypes: string[]; favoriteLocationIds: string[]; recentLocationIds: string[] }
export interface PropertyView { propertyId: string; name: string }
export interface DriverListQuery { page: number; pageSize: number; search?: string; propertyId?: string; status?: string; view?: 'AVAILABLE' | 'ON_TRIP' | 'BLACKLIST'; sort?: string }
export interface ApprovalListQuery { page: number; pageSize: number; search?: string; propertyId?: string; status?: string; from?: string; to?: string }
export interface ApprovalSummaryView { pendingCount: number; approvalChain: string[] }

function listPath(path: string, query: DriverListQuery | ApprovalListQuery | Pick<ApprovalListQuery, 'search' | 'propertyId' | 'status' | 'from' | 'to'>): string {
  const search = new URLSearchParams()
  Object.entries(query).forEach(([key, value]) => { if (value !== undefined && value !== '') search.set(key, String(value)) })
  return `${path}?${search}`
}

const propertyNames: Record<string, string> = Object.fromEntries([['COD', 'City of Dreams'], ['SC', 'Studio City'], ['ALT', 'Altira'], ['GH', 'Grand Hyatt']])
const propertyCodes: Record<string, string> = Object.fromEntries(Object.entries(propertyNames).map(([code, name]) => [name, code]))
const availability: Record<string, DriverAvailability> = Object.fromEntries([['AVAILABLE', 'Available'], ['ON_TRIP', 'On Trip'], ['BREAK', 'Break'], ['OFF_DUTY', 'Off Duty']])
const driverStatus: Record<string, DriverStatus> = Object.fromEntries([['ACTIVE', 'Active'], ['INACTIVE', 'Inactive'], ['SUSPENDED', 'Suspended']])
const tripStatus: Record<string, BookingStatus> = Object.fromEntries([['PENDING', 'Pending'], ['CONFIRMED', 'Confirmed'], ['DRIVER_ASSIGNED', 'Driver Assigned'], ['DRIVER_EN_ROUTE', 'Driver En Route'], ['ARRIVED_AT_PICKUP', 'Arrived at Pickup'], ['IN_PROGRESS', 'In Progress'], ['COMPLETED', 'Completed'], ['CANCELLED', 'Cancelled']])
export const notificationTypes: Record<string, string> = Object.fromEntries([['Booking confirmed', 'BOOKING_CONFIRMED'], ['Driver assigned', 'DRIVER_ASSIGNED'], ['Driver arrived', 'DRIVER_ARRIVED'], ['Trip completed', 'TRIP_COMPLETED'], ['Approval required', 'APPROVAL_REQUIRED'], ['System alerts', 'SYSTEM_ALERT']])
const notificationLabels: Record<string, string> = Object.fromEntries(Object.entries(notificationTypes).map(([label, code]) => [code, label]))

export function mapDriver(value: DriverView): Driver {
  return { driverId: value.driverId, fullName: value.name, licenseNumber: value.licenseNumber, phoneMasked: value.phone, propertyName: propertyNames[value.propertyId] ?? value.propertyId, vehicleName: value.assignedVehicle, vehicleId: value.assignedVehicleId ?? null, availability: availability[value.availability ?? ''] ?? 'Off Duty', status: driverStatus[value.status] ?? 'Inactive', emergencyContact: value.emergencyContact?.name ?? '', emergencyContactPhone: value.emergencyContact?.phone ?? '', languagesSpoken: value.languages.join(', '), assignedProperties: [propertyNames[value.propertyId] ?? value.propertyId], blacklistReason: value.restriction?.reason ?? '', blacklistEffectiveDate: value.restriction?.effectiveDate ?? '', blacklistExpiryDate: value.restriction?.expiryDate ?? '', licenseExpiryDate: value.licenseExpiryDate, version: value.version }
}
export function mapGuest(value: GuestSummaryView): Guest {
  const tier = value.tier?.toLowerCase() ?? ''
  const mappedTier: Guest['tier'] = tier.includes('diamond') ? 'Diamond' : tier.includes('platinum') ? 'Platinum' : tier.includes('gold') ? 'Gold' : tier.includes('vip') ? 'VIP' : 'Silver'
  return { guestId: value.guestId, fullName: value.name, roomNumber: value.roomNumber ?? '', tier: mappedTier, isInHouse: value.inHouse, phoneLastFour: '', preferences: [], isBlacklisted: false }
}
export function mapGuestProfile(value: GuestProfileView): Guest { const guest = mapGuest(value); return { ...guest, propertyId: value.propertyId ?? undefined, phoneMasked: value.phoneMasked ?? undefined, phoneLastFour: value.phoneMasked?.slice(-4) ?? '', isBlacklisted: value.blacklistStatus?.listed ?? false, preferences: [value.preferences?.language, value.preferences?.preferredVehicleClass].filter((item): item is string => Boolean(item)) } }
const vehicleClassNames: Record<string, string> = { BUSINESS_SEDAN: 'Standard Sedan', LUXURY_SEDAN: 'Luxury Sedan', LUXURY_MPV: 'Luxury MPV', MAYBACH: 'Maybach' }
export function mapTrip(value: TripView): Booking {
  return { bookingId: value.tripId, guestId: value.guest.guestId, guestName: value.guest.name, roomNumber: value.guest.roomNumber ?? '', pickupLocation: value.route.pickupName, dropoffLocation: value.route.dropoffName, pickupLocationId: value.route.pickupLocationId, dropoffLocationId: value.route.dropoffLocationId, pickupAt: value.pickupAt, propertyName: propertyNames[value.propertyId] ?? value.propertyId, vehicleClass: vehicleClassNames[value.vehicleClass] ?? value.vehicleClass.replaceAll('_', ' '), status: tripStatus[value.status] ?? 'Pending', points: value.points?.required ?? 0, createdByStaffId: value.bookedBy?.userId, cancellationReason: value.cancellation?.reasonCode, version: value.version }
}
export function mapLocation(value: LocationView): Location {
  const category: Location['category'] = value.categoryCode.includes('AIRPORT') ? 'Airport' : value.categoryCode.includes('FERRY') ? 'Ferry' : value.categoryCode.includes('BORDER') ? 'Border' : 'Properties'
  return { locationId: value.locationId, name: value.name, category, address: value.address, isFavorite: false }
}
export function mapNotice(value: ConsoleNotificationView): Notice { const category = notificationLabels[value.type] ?? value.type; return { noticeId: value.notificationId, category, title: category, message: value.summary, createdAt: value.occurredAt, isRead: value.isRead } }
export function mapApproval(value: CrossBorderApprovalView): Approval { return { approvalId: value.approvalId, guestName: value.guest.name, roomNumber: '', tier: value.guest.tierCode, reason: value.tripReference, status: value.status === 'APPROVED' ? 'Approved' : value.status === 'REJECTED' ? 'Rejected' : 'Pending Approval', requestedAt: value.pickupAt, version: value.version } }
export function mapQualification(value: QualificationView): Approval { return { approvalId: value.qualificationId, guestName: value.guest.name, guestId: value.guest.guestId, roomNumber: '', tier: value.guest.tierCode, reason: value.justification ?? '', status: value.status === 'APPROVED' ? 'Approved' : 'Pending Approval', requestedAt: value.operatedAt ?? '', requestedByStaffId: value.operatorId ?? undefined, version: value.version } }

export const lms = {
  drivers: () => api.request<DriverView[]>('/api/dispatch/staff/v1/drivers?page=1&pageSize=50'),
  listDrivers: (query: DriverListQuery, init: RequestInit = {}): Promise<SuccessEnvelope<DriverView[]>> => api.requestEnvelope<DriverView[]>(listPath('/api/dispatch/staff/v1/drivers', query), init),
  driver: (driverId: string) => api.request<DriverView>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}`),
  vehicleOptions: (propertyId: string, page: number) => api.requestEnvelope<VehicleOption[]>(listPath('/api/dispatch/staff/v1/vehicles', { propertyId, page, pageSize: 50 })),
  properties: async () => {
    const values = await api.request<PropertyView[]>('/api/platform/staff/v1/properties?page=1&pageSize=50&status=ACTIVE')
    values.forEach((value) => { propertyNames[value.propertyId] = value.name; propertyCodes[value.name] = value.propertyId })
    return values
  },
  createDriver: (driver: Driver) => api.request<DriverView>('/api/dispatch/staff/v1/drivers', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ name: driver.fullName, licenseNumber: driver.licenseNumber, licenseExpiryDate: driver.licenseExpiryDate, phone: driver.phoneMasked, propertyId: propertyCodes[driver.propertyName] ?? driver.propertyName, assignedVehicleId: driver.vehicleId || null, status: driver.status.toUpperCase(), languages: driver.languagesSpoken.split(',').map((part) => part.trim()).filter(Boolean), emergencyContact: { name: driver.emergencyContact, phone: driver.emergencyContactPhone } }) }),
  updateDriver: (driver: Driver, previous: Driver) => {
    const fields: [keyof Driver, string, unknown][] = [
      ['fullName', 'name', driver.fullName], ['licenseNumber', 'licenseNumber', driver.licenseNumber],
      ['licenseExpiryDate', 'licenseExpiryDate', driver.licenseExpiryDate], ['phoneMasked', 'phone', driver.phoneMasked],
      ['propertyName', 'propertyId', propertyCodes[driver.propertyName] ?? driver.propertyName],
      ['vehicleId', 'assignedVehicleId', driver.vehicleId || null], ['status', 'status', driver.status.toUpperCase()],
      ['languagesSpoken', 'languages', driver.languagesSpoken.split(',').map((part) => part.trim()).filter(Boolean)],
      ['emergencyContact', 'emergencyContact', { name: driver.emergencyContact, phone: driver.emergencyContactPhone }],
    ]
    const changes = Object.fromEntries(fields.filter(([key]) => key === 'emergencyContact' ? driver.emergencyContact !== previous.emergencyContact || driver.emergencyContactPhone !== previous.emergencyContactPhone : driver[key] !== previous[key]).map(([, key, value]) => [key, value]))
    return api.request<DriverView>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driver.driverId)}`, { method: 'PATCH', headers: new Headers([['X-Expected-Version', String(driver.version)]]), body: JSON.stringify(changes) })
  },
  guests: () => api.request<GuestSummaryView[]>('/api/booking/staff/v1/guests'),
  guestProfile: (guestId: string) => api.request<GuestProfileView>(`/api/booking/staff/v1/guests/${encodeURIComponent(guestId)}`),
  trips: () => api.request<TripView[]>('/api/booking/staff/v1/trips'),
  createTrip: (request: CreateTripRequest) => api.request<TripView>('/api/booking/staff/v1/trips', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify(request) }),
  updateTrip: (tripId: string, version: number, changes: { pickupLocationId: string; pickupAt: string }) => api.request<TripView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}`, { method: 'PATCH', headers: new Headers([['X-Expected-Version', String(version)]]), body: JSON.stringify(changes) }),
  cancelTrip: (tripId: string, reasonCode: string) => api.request<TripView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/cancellation`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ reasonCode }) }),
  cancellationReasons: () => api.request<{ reasonCode: string; name: string }[]>('/api/platform/staff/v1/cancellation-reasons?page=1&pageSize=50'),
  locations: () => api.request<LocationView[]>('/api/platform/staff/v1/locations?page=1&pageSize=50&isActive=true'),
  notifications: () => api.request<ConsoleNotificationView[]>('/api/booking/staff/v1/console-notifications'),
  staffPreferences: () => api.request<StaffPreferencesView>('/api/booking/staff/v1/console-settings'),
  updateConsoleSettings: (station: string, defaultNotificationChannel: string, confirmationLanguage: string) => api.request<StaffPreferencesView>('/api/booking/staff/v1/console-settings', { method: 'PUT', body: JSON.stringify({ station, defaultNotificationChannel, confirmationLanguage }) }),
  updateNotificationPreferences: (enabledNotificationTypes: string[]) => api.request<StaffPreferencesView>('/api/booking/staff/v1/notification-preferences', { method: 'PUT', body: JSON.stringify({ enabledNotificationTypes }) }),
  updateLocationFavorites: (favoriteLocationIds: string[]) => api.request<StaffPreferencesView>('/api/booking/staff/v1/location-favorites', { method: 'PUT', body: JSON.stringify({ favoriteLocationIds }) }),
  markRead: (id: string) => api.request<unknown>(`/api/booking/staff/v1/console-notifications/${encodeURIComponent(id)}/read`, { method: 'POST' }),
  markAllRead: () => api.request<unknown>('/api/booking/staff/v1/console-notifications/read-all', { method: 'POST' }),
  approvals: () => api.request<CrossBorderApprovalView[]>('/api/booking/staff/v1/cross-border-approvals?page=1&pageSize=50'),
  listApprovals: (query: ApprovalListQuery, init: RequestInit = {}): Promise<SuccessEnvelope<CrossBorderApprovalView[]>> => api.requestEnvelope<CrossBorderApprovalView[]>(listPath('/api/booking/staff/v1/cross-border-approvals', query), init),
  approval: (approvalId: string) => api.request<CrossBorderApprovalView>(`/api/booking/staff/v1/cross-border-approvals/${encodeURIComponent(approvalId)}`),
  approvalSummary: (query: Pick<ApprovalListQuery, 'search' | 'propertyId' | 'status' | 'from' | 'to'>) => api.request<ApprovalSummaryView>(listPath('/api/booking/staff/v1/cross-border-approval-summaries', query)),
  qualifications: () => api.request<QualificationView[]>('/api/booking/staff/v1/qualification-references?page=1&pageSize=50'),
  blacklistHits: () => api.request<{ approvalId: string; targetId: string; reason?: string | null }[]>('/api/booking/staff/v1/blacklist-hits'),
  escalateQualification: (id: string, version: number, justification: string) => api.request<QualificationView>(`/api/booking/staff/v1/qualification-references/${encodeURIComponent(id)}/escalations`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()], ['X-Expected-Version', String(version)]]), body: JSON.stringify({ justification }) }),
  decideApproval: (id: string, version: number, decision: 'APPROVED' | 'REJECTED', comments: string) => api.request<CrossBorderApprovalView>(`/api/booking/staff/v1/cross-border-approvals/${encodeURIComponent(id)}/decisions`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()], ['X-Expected-Version', String(version)]]), body: JSON.stringify({ decision, comments }) }),
  bulkDecideApprovals: (items: { approvalId: string; expectedVersion: number }[], decision: 'APPROVED' | 'REJECTED', comments: string) => api.request<{ items: { approvalId: string; status: string; version: number }[] }>('/api/booking/staff/v1/cross-border-approval-decisions', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ items, decision, comments }) }),
}
