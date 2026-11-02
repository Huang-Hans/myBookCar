import type { Approval, Booking, BookingStatus, Driver, DriverAvailability, DriverStatus, Guest, Location, Notice } from '../data/demo'
import { api } from './session'
import type { SuccessEnvelope } from './http'
import { isTripTrack, type TripTrack } from '../../../shared/tripLocations/contracts'

export interface DriverView { driverId: string; name: string; licenseNumber: string; licenseExpiryDate: string; phone: string; propertyId: string; assignedVehicle: string; assignedVehicleId?: string | null; status: string; languages: string[]; emergencyContact?: { name?: string; phone?: string } | null; availability?: string | null; restriction?: { reason?: string; effectiveDate?: string; expiryDate?: string } | null; version: number }
export interface GuestSummaryView { guestId: string; name: string; roomNumber?: string | null; memberId?: string | null; tier?: string | null; inHouse: boolean; propertyId?: string | null }
export interface UpdateGuestRequest { name?: string; tier?: 'SILVER' | 'GOLD' | 'PLATINUM' | 'DIAMOND' | 'VIP'; roomNumber?: string | null; propertyId?: string | null }
export interface GuestProfileView extends GuestSummaryView { propertyId?: string | null; phoneMasked?: string | null; blacklistStatus?: { listed: boolean; reason?: string | null } | null; preferences?: { language?: string | null; preferredVehicleClass?: string | null } | null; recentTrips?: TripView[] }
export interface TripAssignmentView { driverId: string; driverName: string; vehicleLabel: string; assignedAt: string }
export interface TripView { productCode?: BookingProductCode | null; channel: TripChannel; assignment?: TripAssignmentView | null; tripId: string; tripReference: string; propertyId: string; guest: { guestId: string; name: string; tierCode?: string | null; roomNumber?: string | null; memberId?: string | null }; route: { pickupLocationId: string; pickupName: string; dropoffLocationId: string; dropoffName: string; isCrossBorder?: boolean }; pickupAt: string; pickupMode?: string | null; serviceType: string; vehicleClass: string; passengers?: number; luggage?: number; specialRequests?: string | null; status: string; stage?: string; points?: { required: number } | null; bookedBy?: { userId?: string; name?: string | null } | null; cancellation?: { reasonCode?: string } | null; version: number }
export type DispatchBoardState = 'PENDING' | 'AWAITING_RESPONSE' | 'DRIVER_ASSIGNED' | 'NEEDS_REDISPATCH' | 'PENDING_CONFIRMATION'
export interface LocationView { locationId: string; name: string; categoryCode: string; address: string; region: string; isActive: boolean; version: number; coordinates?: { latitude: number; longitude: number } | null }
export interface ConsoleNotificationView { notificationId: string; type: string; summary: string; tripReference?: string | null; occurredAt: string; isRead: boolean }
export interface CrossBorderApprovalView { approvalId: string; tripReference: string; guest: { guestId: string; name: string; tierCode: string }; propertyId: string; route: { pickupName?: string; dropoffName?: string }; pickupAt: string; slaDeadlineAt?: string | null; documents: { documentType: string; isVerified: boolean }[]; status: string; version: number; decision?: { comments?: string } | null }
export interface QualificationView { qualificationId: string; guest: { guestId: string; name: string; tierCode: string }; propertyId: string; status: string; justification?: string | null; operatorId?: string | null; operatedAt?: string | null; version: number }
export type BookingProductCode = 'BUSINESS_SEDAN' | 'LIMOUSINE' | 'PARTY' | 'CROSS_BORDER_TRANSFER'
export interface CreateTripRequest { guestId: string; propertyId: string; pickupLocationId: string; dropoffLocationId: string; pickupAt?: string; pickupMode: 'SCHEDULED' | 'ASAP'; serviceType: 'ONE_WAY' | 'AIRPORT_PICKUP' | 'AIRPORT_DROPOFF' | 'INTER_PROPERTY'; productCode?: BookingProductCode; vehicleClass?: 'BUSINESS_SEDAN' | 'LUXURY_SEDAN' | 'LUXURY_MPV' | 'MAYBACH'; passengers: number; luggage: number; specialRequests?: string; confirmationLanguage: 'EN' | 'ZH_HANT' | 'ZH_HANS'; couponId?: string }
export interface ConsoleBookingProduct { vehicleClass: NonNullable<CreateTripRequest['vehicleClass']>; productCode: BookingProductCode; name: string; isBookable: boolean; serviceType: CreateTripRequest['serviceType']; routeScope: 'LOCAL' | 'CROSS_BORDER'; unavailableReason?: string | null }
export interface ConsoleBookingVehicle { vehicleClass: NonNullable<CreateTripRequest['vehicleClass']>; maxPassengers: number; maxLuggage: number }
export interface ConsoleBookingOptions { products: ConsoleBookingProduct[]; vehicles: ConsoleBookingVehicle[]; isAsapAvailable: boolean }
export interface BookingPreviewView { canSubmit: boolean; willWaitlist: boolean; requiresApproval: boolean; blockedReasons: { code: string; field: string; message: string }[]; points: { required: number } }
export interface StaffPreferencesView { station: string; defaultNotificationChannel: string; confirmationLanguage: string; enabledNotificationTypes: string[]; favoriteLocationIds: string[]; recentLocationIds: string[] }
export interface TripTimelineEventView { eventId: string; type: string; occurredAt: string; orderStatus?: string | null; stage?: string | null; actorName?: string | null; reason?: string | null }
export interface TripCancellationPolicyView { tripReference: string; propertyId: string; status: string; policyReference?: string | null }
export interface GuestListQuery { search?: string; tier?: string; page?: number; pageSize?: number }
export type TripChannel = 'CONSOLE' | 'SELF_SERVICE'
export interface TripListQuery { search?: string; propertyId?: string; status?: string; channel?: TripChannel; from?: string; to?: string; page?: number; pageSize?: number; sort?: string }
export interface DispatchOrderListQuery { search?: string; propertyId?: string; channel?: string; dispatchStatus?: string; from?: string; to?: string; page?: number; pageSize?: number }
export type DispatchOrderSummaryQuery = Omit<DispatchOrderListQuery, 'dispatchStatus' | 'page' | 'pageSize'>
export interface DispatchOfferView { offerId: string; round: number; status: string; driverId: string; driverName?: string | null; createdAt: string; expiresAt: string; serverTime: string; version: number }
export interface DispatchCandidateView { driverId: string; name: string; propertyId: string; availability?: string | null; vehicleLabel?: string | null }
export interface DispatchOrderView { tripId: string; tripReference: string; propertyId: string; channel: string; guest: { name: string; roomNumber?: string | null; tierCode?: string | null; memberId?: string | null }; route: { pickupName: string; dropoffName: string; isCrossBorder?: boolean }; pickupAt: string; pickupMode?: string | null; serviceType: string; vehicleClass: string; passengers?: number; luggage?: number; specialRequests?: string | null; orderStatus: string; dispatchStatus: string; currentOffer?: DispatchOfferView | null; assignment?: { driverName?: string | null; vehicleLabel?: string | null } | null; bookedBy?: { userId?: string | null; name?: string | null } | null; createdAt: string; canDispatch: boolean; blockedReason?: string | null; version: number }
export interface DispatchOrderSummaryView { total: number; counts: Record<string, number> }
export interface NotificationListQuery { type?: string; unread?: boolean }
export interface PropertyView { propertyId: string; name: string }
export interface DriverListQuery { page: number; pageSize: number; search?: string; propertyId?: string; status?: string; view?: 'AVAILABLE' | 'ON_TRIP' | 'BLACKLIST'; sort?: string }
export interface ApprovalListQuery { page: number; pageSize: number; search?: string; propertyId?: string; status?: string; from?: string; to?: string }
export interface ApprovalSummaryView { pendingCount: number; approvalChain: string[] }

function listPath(path: string, query: DriverListQuery | ApprovalListQuery | Pick<ApprovalListQuery, 'search' | 'propertyId' | 'status' | 'from' | 'to'>): string {
  const search = new URLSearchParams()
  Object.entries(query).forEach(([key, value]) => { if (value !== undefined && value !== '') search.set(key, String(value)) })
  return `${path}?${search}`
}
// The server omits null fields on the wire; the shared contract expects them as explicit null.
function withExplicitNulls(track: unknown): unknown {
  if (!track || typeof track !== 'object') return track
  const { points } = track as { points?: unknown }
  if (!Array.isArray(points)) return track
  return { ...track, points: points.map((point) => point && typeof point === 'object' ? { headingDegrees: null, speedKmh: null, ...point } : point) }
}
function queryPath(path: string, query: object, paging = false): string {
  const search = new URLSearchParams()
  if (paging) { search.set('page', '1'); search.set('pageSize', '50') }
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === '') return
    if (typeof value === 'string' || typeof value === 'boolean') search.set(key, String(value))
    else if (typeof value === 'number' && (key === 'page' || key === 'pageSize')) search.set(key, String(value))
  })
  const text = search.toString()
  return text ? `${path}?${text}` : path
}

const assignedTripStatuses = new Set(['DRIVER_ASSIGNED', 'DRIVER_EN_ROUTE', 'ARRIVED_AT_PICKUP', 'IN_PROGRESS'])
export function dispatchBoardState(status: string): DispatchBoardState | null {
  if (status === 'CONFIRMED') return 'PENDING'
  if (status === 'PENDING') return 'PENDING_CONFIRMATION'
  if (assignedTripStatuses.has(status)) return 'DRIVER_ASSIGNED'
  return null
}

const propertyNames: Record<string, string> = Object.fromEntries([['COD', 'City of Dreams'], ['SC', 'Studio City'], ['ALT', 'Altira'], ['GH', 'Grand Hyatt']])
const propertyCodes: Record<string, string> = Object.fromEntries(Object.entries(propertyNames).map(([code, name]) => [name, code]))
const availability: Record<string, DriverAvailability> = Object.fromEntries([['AVAILABLE', 'Available'], ['ON_TRIP', 'On Trip'], ['BREAK', 'Break'], ['OFF_DUTY', 'Off Duty']])
const driverStatus: Record<string, DriverStatus> = Object.fromEntries([['ACTIVE', 'Active'], ['INACTIVE', 'Inactive'], ['SUSPENDED', 'Suspended']])
const tripStatus: Record<string, BookingStatus> = Object.fromEntries([['PENDING', 'Pending'], ['CONFIRMED', 'Confirmed'], ['DRIVER_ASSIGNED', 'Driver Assigned'], ['DRIVER_EN_ROUTE', 'Driver En Route'], ['ARRIVED_AT_PICKUP', 'Arrived at Pickup'], ['IN_PROGRESS', 'In Progress'], ['COMPLETED', 'Completed'], ['CANCELLED', 'Cancelled']])
const inTripStages: Record<string, string> = Object.fromEntries([['PASSENGER_ON_BOARD', 'Passenger On Board'], ['IN_TRANSIT', 'In Transit'], ['ARRIVED_AT_DESTINATION', 'Arrived at Destination']])
export function tripStageLabel(code: string): string { return inTripStages[code] ?? tripStatus[code] ?? code }
export const notificationTypes: Record<string, string> = Object.fromEntries([['Booking confirmed', 'BOOKING_CONFIRMED'], ['Driver assigned', 'DRIVER_ASSIGNED'], ['Driver arrived', 'DRIVER_ARRIVED'], ['Trip completed', 'TRIP_COMPLETED'], ['Approval required', 'APPROVAL_REQUIRED'], ['System alerts', 'SYSTEM_ALERT']])
const notificationLabels: Record<string, string> = Object.fromEntries(Object.entries(notificationTypes).map(([label, code]) => [code, label]))

export function mapDriver(value: DriverView): Driver {
  return { driverId: value.driverId, fullName: value.name, licenseNumber: value.licenseNumber, phoneMasked: value.phone, propertyName: propertyNames[value.propertyId] ?? value.propertyId, vehicleName: value.assignedVehicle, vehicleId: value.assignedVehicleId ?? null, availability: availability[value.availability ?? ''] ?? 'Off Duty', status: driverStatus[value.status] ?? 'Inactive', emergencyContact: value.emergencyContact?.name ?? '', emergencyContactPhone: value.emergencyContact?.phone ?? '', languagesSpoken: value.languages.join(', '), assignedProperties: [propertyNames[value.propertyId] ?? value.propertyId], blacklistReason: value.restriction?.reason ?? '', blacklistEffectiveDate: value.restriction?.effectiveDate ?? '', blacklistExpiryDate: value.restriction?.expiryDate ?? '', licenseExpiryDate: value.licenseExpiryDate, version: value.version }
}
export function mapGuest(value: GuestSummaryView): Guest {
  const tier = value.tier?.toLowerCase() ?? ''
  const mappedTier: Guest['tier'] = tier.includes('diamond') ? 'Diamond' : tier.includes('platinum') ? 'Platinum' : tier.includes('gold') ? 'Gold' : tier.includes('vip') ? 'VIP' : 'Silver'
  return { guestId: value.guestId, memberId: value.memberId ?? '', fullName: value.name, roomNumber: value.roomNumber ?? '', tier: mappedTier, isInHouse: value.inHouse, phoneLastFour: '', preferences: [], isBlacklisted: false }
}
export function mapGuestProfile(value: GuestProfileView): Guest { const guest = mapGuest(value); return { ...guest, propertyId: value.propertyId ?? undefined, phoneMasked: value.phoneMasked ?? undefined, phoneLastFour: value.phoneMasked?.slice(-4) ?? '', isBlacklisted: value.blacklistStatus?.listed ?? false, preferences: [value.preferences?.language, value.preferences?.preferredVehicleClass].filter((item): item is string => Boolean(item)) } }
const vehicleClassNames: Record<string, string> = { BUSINESS_SEDAN: 'Standard Sedan', LUXURY_SEDAN: 'Luxury Sedan', LUXURY_MPV: 'Luxury MPV', MAYBACH: 'Maybach' }
export function mapTrip(value: Omit<TripView, 'channel'> & { channel?: TripChannel }): Booking {
  return { productCode: value.productCode ?? undefined, channel: value.channel ?? 'CONSOLE', bookingId: value.tripId, tripReference: value.tripReference, guestId: value.guest.guestId, guestName: value.guest.name, roomNumber: value.guest.roomNumber ?? '', pickupLocation: value.route.pickupName, dropoffLocation: value.route.dropoffName, pickupLocationId: value.route.pickupLocationId, dropoffLocationId: value.route.dropoffLocationId, pickupAt: value.pickupAt, propertyName: propertyNames[value.propertyId] ?? value.propertyId, vehicleClass: vehicleClassNames[value.vehicleClass] ?? value.vehicleClass.replaceAll('_', ' '), status: tripStatus[value.status] ?? 'Pending', stageLabel: tripStageLabel(value.stage ?? value.status), points: value.points?.required ?? 0, createdByStaffId: value.bookedBy?.userId, createdByStaffName: value.bookedBy?.name ?? undefined, cancellationReason: value.cancellation?.reasonCode, driverName: value.assignment?.driverName, vehicleLabel: value.assignment?.vehicleLabel, version: value.version }
}
export function mapLocation(value: LocationView): Location {
  const category: Location['category'] = value.categoryCode.includes('AIRPORT') ? 'Airport' : value.categoryCode.includes('FERRY') ? 'Ferry' : value.categoryCode.includes('BORDER') ? 'Border' : 'Properties'
  return { locationId: value.locationId, name: value.name, category, address: value.address, isFavorite: false }
}
export function mapNotice(value: ConsoleNotificationView): Notice { const category = notificationLabels[value.type] ?? value.type; return { noticeId: value.notificationId, category, title: category, message: value.summary, createdAt: value.occurredAt, isRead: value.isRead } }
export function mapApproval(value: CrossBorderApprovalView): Approval { return { approvalId: value.approvalId, guestName: value.guest.name, roomNumber: '', tier: value.guest.tierCode, reason: value.tripReference, status: value.status === 'APPROVED' ? 'Approved' : value.status === 'REJECTED' ? 'Rejected' : 'Pending Approval', requestedAt: value.pickupAt, version: value.version } }
export function mapQualification(value: QualificationView): Approval { return { approvalId: value.qualificationId, guestName: value.guest.name, guestId: value.guest.guestId, roomNumber: '', tier: value.guest.tierCode, reason: value.justification ?? '', status: value.status === 'APPROVED' ? 'Approved' : value.status === 'REJECTED' ? 'Rejected' : 'Pending Approval', requestedAt: value.operatedAt ?? '', requestedByStaffId: value.operatorId ?? undefined, version: value.version } }

export const lms = {
  drivers: () => api.request<DriverView[]>('/api/dispatch/staff/v1/drivers?page=1&pageSize=50'),
  listDrivers: (query: DriverListQuery, init: RequestInit = {}): Promise<SuccessEnvelope<DriverView[]>> => api.requestEnvelope<DriverView[]>(listPath('/api/dispatch/staff/v1/drivers', query), init),
  driver: (driverId: string) => api.request<DriverView>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}`),
  updateDriverVehicle: (driverId: string, assignedVehicleId: string | null, version: number) =>
    api.request<DriverView>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}`, { method: 'PATCH', headers: new Headers([['X-Expected-Version', String(version)]]), body: JSON.stringify({ assignedVehicleId }) }),
  removeDriver: (driverId: string) => api.request<null>(`/api/dispatch/staff/v1/drivers/${encodeURIComponent(driverId)}`, { method: 'DELETE' }),
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
  guests: () => api.request<GuestSummaryView[]>('/api/booking/staff/v1/guests?page=1&pageSize=50'),
  searchGuests: (query: GuestListQuery = {}) => api.request<GuestSummaryView[]>(queryPath('/api/booking/staff/v1/guests', { search: query.search, tier: query.tier }, true)),
  listGuestPage: (query: GuestListQuery = {}) => api.requestEnvelope<GuestSummaryView[]>(queryPath('/api/booking/staff/v1/guests', { page: 1, pageSize: 20, ...query }, true)),
  guestProfile: (guestId: string) => api.request<GuestProfileView>(`/api/booking/staff/v1/guests/${encodeURIComponent(guestId)}`),
  updateGuest: (guestId: string, changes: UpdateGuestRequest) => api.request<GuestSummaryView>(`/api/booking/staff/v1/guests/${encodeURIComponent(guestId)}`, { method: 'PATCH', body: JSON.stringify(changes) }),
  trips: () => api.request<TripView[]>('/api/booking/staff/v1/trips?page=1&pageSize=20'),
  listTrips: (query: TripListQuery = {}) => api.request<TripView[]>(queryPath('/api/booking/staff/v1/trips', { page: 1, pageSize: 20, ...query }, true)),
  listTripPage: (query: TripListQuery = {}) => api.requestEnvelope<TripView[]>(queryPath('/api/booking/staff/v1/trips', { page: 1, pageSize: 20, ...query }, true)),
  listDispatchOrderPage: (query: DispatchOrderListQuery = {}) => api.requestEnvelope<DispatchOrderView[]>(queryPath('/api/dispatch/staff/v1/dispatch-orders', query, true)),
  dispatchOrderSummaries: (query: DispatchOrderSummaryQuery = {}) => api.request<DispatchOrderSummaryView>(queryPath('/api/dispatch/staff/v1/dispatch-orders/summaries', query)),
  dispatchOrder: (tripId: string) => api.request<DispatchOrderView>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}`),
  listDispatchCandidates: (tripId: string, page = 1) => api.requestEnvelope<DispatchCandidateView[]>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/drivers?page=${page}&pageSize=50`),
  listDispatchOffers: (tripId: string, page = 1) => api.requestEnvelope<DispatchOfferView[]>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/offers?page=${page}&pageSize=20`),
  createDispatchOffer: (tripId: string, driverId: string, version: number) => api.request<DispatchOfferView>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/offers`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()], ['X-Expected-Version', String(version)]]), body: JSON.stringify({ driverId }) }),
  trip: (tripId: string) => api.request<TripView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}`),
  tripTrack: async (tripId: string, afterSeq = 0): Promise<TripTrack> => {
    const suffix = afterSeq > 0 ? `?afterSeq=${afterSeq}` : ''
    const track = withExplicitNulls(await api.request<unknown>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/track${suffix}`))
    if (!isTripTrack(track) || track.tripId !== tripId) throw new Error('The trip track response is invalid.')
    return track
  },
  tripEvents: (tripId: string) => api.request<TripTimelineEventView[]>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/events`),
  cancellationPolicy: (tripId: string) => api.request<TripCancellationPolicyView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/cancellation-policy`),
  tripStatusSummaries: (query: Omit<TripListQuery, 'status'> = {}) => api.request<Record<string, number>>(queryPath('/api/booking/staff/v1/trip-status-summaries', query)),
  bookingOptions: () => api.request<ConsoleBookingOptions>('/api/booking/staff/v1/booking-options'),
  previewBooking: (request: CreateTripRequest) => api.request<BookingPreviewView>('/api/booking/staff/v1/booking-previews', { method: 'POST', body: JSON.stringify({ guestId: request.guestId, propertyId: request.propertyId, pickupLocationId: request.pickupLocationId, dropoffLocationId: request.dropoffLocationId, pickupAt: request.pickupAt, pickupMode: request.pickupMode, serviceType: request.serviceType, productCode: request.productCode, vehicleClass: request.vehicleClass, passengers: request.passengers, luggage: request.luggage }) }),
  createTrip: (request: CreateTripRequest, idempotencyKey: string = crypto.randomUUID()) => api.request<TripView>('/api/booking/staff/v1/trips', { method: 'POST', headers: new Headers([['Idempotency-Key', idempotencyKey]]), body: JSON.stringify(request) }),
  updateTrip: (tripId: string, version: number, changes: { pickupLocationId: string; pickupAt: string }) => api.request<TripView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}`, { method: 'PATCH', headers: new Headers([['X-Expected-Version', String(version)]]), body: JSON.stringify(changes) }),
  cancelTrip: (tripId: string, reasonCode: string) => api.request<TripView>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/cancellation`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ reasonCode }) }),
  cancellationReasons: () => api.request<{ reasonCode: string; name: string }[]>('/api/platform/staff/v1/cancellation-reasons?page=1&pageSize=50'),
  locations: () => api.request<LocationView[]>('/api/platform/staff/v1/locations?page=1&pageSize=50&isActive=true'),
  location: (locationId: string) => api.request<LocationView>(`/api/platform/staff/v1/locations/${encodeURIComponent(locationId)}`),
  notifications: (query: NotificationListQuery = {}) => api.request<ConsoleNotificationView[]>(queryPath('/api/booking/staff/v1/console-notifications', { type: query.type, unread: query.unread ? true : undefined }, true)),
  unreadNotificationCount: () => api.request<{ unreadCount: number }>('/api/booking/staff/v1/console-notifications/unread-count'),
  staffPreferences: () => api.request<StaffPreferencesView>('/api/booking/staff/v1/console-settings'),
  locationFavorites: () => api.request<StaffPreferencesView>('/api/booking/staff/v1/location-favorites'),
  updateConsoleSettings: (station: string, defaultNotificationChannel: string, confirmationLanguage: string) => api.request<StaffPreferencesView>('/api/booking/staff/v1/console-settings', { method: 'PUT', body: JSON.stringify({ station, defaultNotificationChannel, confirmationLanguage }) }),
  updateNotificationPreferences: (enabledNotificationTypes: string[]) => api.request<StaffPreferencesView>('/api/booking/staff/v1/notification-preferences', { method: 'PUT', body: JSON.stringify({ enabledNotificationTypes }) }),
  updateLocationFavorites: (favoriteLocationIds: string[]) => api.request<StaffPreferencesView>('/api/booking/staff/v1/location-favorites', { method: 'PUT', body: JSON.stringify({ favoriteLocationIds }) }),
  markRead: (id: string) => api.request<unknown>(`/api/booking/staff/v1/console-notifications/${encodeURIComponent(id)}/read`, { method: 'POST' }),
  markAllRead: () => api.request<unknown>('/api/booking/staff/v1/console-notifications/read-all', { method: 'POST' }),
  approvals: () => api.request<CrossBorderApprovalView[]>('/api/booking/staff/v1/cross-border-approvals?page=1&pageSize=50'),
  listApprovals: (query: ApprovalListQuery, init: RequestInit = {}): Promise<SuccessEnvelope<CrossBorderApprovalView[]>> => api.requestEnvelope<CrossBorderApprovalView[]>(listPath('/api/booking/staff/v1/cross-border-approvals', query), init),
  approval: (approvalId: string) => api.request<CrossBorderApprovalView>(`/api/booking/staff/v1/cross-border-approvals/${encodeURIComponent(approvalId)}`),
  approvalSummary: (query: Pick<ApprovalListQuery, 'search' | 'propertyId' | 'status' | 'from' | 'to'>) => api.request<ApprovalSummaryView>(listPath('/api/booking/staff/v1/cross-border-approval-summaries', query)),
  blacklistHits: () => api.request<{ approvalId: string; targetId: string; reason?: string | null }[]>('/api/booking/staff/v1/blacklist-hits?page=1&pageSize=50'),
  escalateQualification: (id: string, version: number, justification: string) => api.request<QualificationView>(`/api/booking/staff/v1/qualification-references/${encodeURIComponent(id)}/escalations`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()], ['X-Expected-Version', String(version)]]), body: JSON.stringify({ justification }) }),
  decideApproval: (id: string, version: number, decision: 'APPROVED' | 'REJECTED', comments: string) => api.request<CrossBorderApprovalView>(`/api/booking/staff/v1/cross-border-approvals/${encodeURIComponent(id)}/decisions`, { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()], ['X-Expected-Version', String(version)]]), body: JSON.stringify({ decision, comments }) }),
  bulkDecideApprovals: (items: { approvalId: string; expectedVersion: number }[], decision: 'APPROVED' | 'REJECTED', comments: string) => api.request<{ items: { approvalId: string; status: string; version: number }[] }>('/api/booking/staff/v1/cross-border-approval-decisions', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ items, decision, comments }) }),
}
