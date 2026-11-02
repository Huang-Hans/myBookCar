import { clearSession, idempotencyKey, query, request, saveSession, type CurrentUser, type StaffSession } from './http'
import { isTripTrack, type TripTrack } from '../../../shared/tripLocations/contracts'

function pickupTimeUtc(value: string): string {
  const instant = new Date(value)
  if (Number.isNaN(instant.getTime())) throw new Error('Trip pickup time is invalid.')
  return instant.toISOString().replace(/\.000Z$/, 'Z')
}

export interface Guest { guestId: string; name: string; roomNumber?: string | null; memberId?: string | null; tier?: string | null }
export interface Property { propertyId: string; name: string }
export interface Location { locationId: string; name: string; address: string; region: string; isActive: boolean; coordinates?: { latitude: number; longitude: number } | null }
export interface Trip { tripId: string; tripReference: string; propertyId: string; guest: { guestId: string; name: string; tierCode?: string | null; roomNumber?: string | null }; route: { pickupLocationId: string; pickupName: string; dropoffLocationId: string; dropoffName: string }; pickupAt: string; serviceType: string; vehicleClass: string; passengers?: number; luggage?: number; status: string; version: number }
export interface TripEvent { eventId: string; type: string; occurredAt: string; orderStatus?: string | null; actorName?: string | null; reason?: string | null }
export type BookingProductCode = 'BUSINESS_SEDAN' | 'LIMOUSINE' | 'PARTY' | 'CROSS_BORDER_TRANSFER'
export type BookingVehicleClass = 'BUSINESS_SEDAN' | 'LUXURY_SEDAN' | 'LUXURY_MPV' | 'MAYBACH'
export type BookingServiceType = 'ONE_WAY' | 'AIRPORT_PICKUP' | 'AIRPORT_DROPOFF' | 'INTER_PROPERTY'
export interface BookingProduct { productCode: BookingProductCode; vehicleClass: BookingVehicleClass; name: string; isBookable: boolean; serviceType: BookingServiceType; routeScope: 'LOCAL' | 'CROSS_BORDER'; unavailableReason?: string | null }
export interface BookingOptions { products: BookingProduct[]; vehicles: Array<{ vehicleClass: BookingVehicleClass; maxPassengers: number; maxLuggage: number }>; isAsapAvailable: boolean }
export interface BookingInput { guestId: string; propertyId: string; pickupLocationId: string; dropoffLocationId: string; pickupAt: string; pickupMode: 'SCHEDULED'; serviceType: BookingServiceType; productCode: BookingProductCode; vehicleClass: BookingVehicleClass; passengers: number; luggage: number; confirmationLanguage: 'EN' | 'ZH_HANT' }
export interface BookingPreview { canSubmit: boolean; willWaitlist: boolean; requiresApproval: boolean; blockedReasons: Array<{ code: string; field: string; message: string }>; points: { required: number } }
export interface DispatchSummary { tripId: string; tripReference: string; propertyId: string; vehicleClass: string; guest: { name: string }; route: { pickupName: string; dropoffName: string }; pickupAt: string; dispatchStatus: string; orderStatus: string; assignment?: { driverName?: string | null; vehicleLabel?: string | null } | null; currentOffer?: { driverName?: string | null; expiresAt: string } | null; canDispatch: boolean; blockedReason?: string | null; version: number }
export interface DispatchCandidate { driverId: string; name: string; availability?: string | null; vehicleLabel?: string | null }
export interface ShiftView { shiftId: string; businessDate: string; period: string; startsAt: string; endsAt: string; endsNextDay: boolean; driverId: string; driverName: string | null; propertyId: string; vehiclePlate: string | null; vehicleGrade?: string | null; status: 'ON_DUTY' | 'REST' | 'LEAVE'; role: string; attendance: string | null; badges: string[]; version: number }
export interface ShiftPeriodView { period: string; startsAt: string; endsAt: string; endsNextDay: boolean; count: number; shifts: ShiftView[] }
export interface ShiftBoardView { timeZone: string; businessDate: string; propertyId: string | null; periods: ShiftPeriodView[] }
export interface DispatchOrderSummary { total: number; counts: Record<string, number> | null }
export interface DispatchOffer { offerId: string; round: number; status: string; driverId: string; driverName: string | null; createdAt: string; expiresAt: string; respondedAt: string | null; serverTime: string; version: number }
export interface StaffNotice { notificationId: string; type: string; summary: string; tripReference?: string | null; occurredAt: string; isRead: boolean }
export interface StaffPreferences { station: string; defaultNotificationChannel: string; confirmationLanguage: string; enabledNotificationTypes: string[]; favoriteLocationIds: string[]; recentLocationIds: string[] }

export const staffApi = {
  async login(username: string, password: string): Promise<CurrentUser> {
    const session = (await request<StaffSession>('/api/v1/login', { method: 'POST', body: { username, password }, isAnonymous: true })).data
    if (!session.accessToken) throw new Error('No access token returned')
    saveSession(session)
    try { return (await request<CurrentUser>('/api/platform/staff/v1/current-user')).data }
    catch (error) { clearSession(); throw error }
  },
  currentUser: () => request<CurrentUser>('/api/platform/staff/v1/current-user'),
  staffPreferences: () => request<StaffPreferences>('/api/booking/staff/v1/console-settings'),
  updateConsoleSettings: (station: string, defaultNotificationChannel: string, confirmationLanguage: 'EN' | 'ZH_HANT') => request<StaffPreferences>('/api/booking/staff/v1/console-settings', { method: 'PUT', body: { station, defaultNotificationChannel, confirmationLanguage } }),
  properties: (page = 1) => request<Property[]>(`/api/platform/staff/v1/properties${query({ page, pageSize: 50 })}`),
  property: (propertyId: string) => request<Property>(`/api/platform/staff/v1/properties/${encodeURIComponent(propertyId)}`),
  guests: (search: string) => request<Guest[]>(`/api/booking/staff/v1/guests${query({ search, page: 1, pageSize: 20 })}`),
  bookingOptions: () => request<BookingOptions>('/api/booking/staff/v1/booking-options'),
  locations: (page = 1) => request<Location[]>(`/api/platform/staff/v1/locations${query({ page, pageSize: 50, isActive: true })}`),
  location: (locationId: string) => request<Location>(`/api/platform/staff/v1/locations/${encodeURIComponent(locationId)}`),
  trips: (filters: { status?: string; search?: string; page?: number; pageSize?: number } = {}) => request<Trip[]>(`/api/booking/staff/v1/trips${query({ page: 1, pageSize: 20, ...filters })}`),
  trip: (tripId: string) => request<Trip>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}`),
  tripEvents: (tripId: string) => request<TripEvent[]>(`/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/events`),
  async tripTrack(tripId: string, afterSeq?: number): Promise<TripTrack> {
    const path = `/api/booking/staff/v1/trips/${encodeURIComponent(tripId)}/track${query({ afterSeq })}`
    const response = await request<unknown>(path)
    // The upstream serializer omits null fields, while the shared track contract requires explicit nulls.
    const data = response.data
    const rawPoints = data && typeof data === 'object' && 'points' in data ? data.points : null
    const track = Array.isArray(rawPoints)
      ? { ...(data as Record<string, unknown>), points: rawPoints.map((point: unknown) => point && typeof point === 'object'
        ? { headingDegrees: null, speedKmh: null, ...point } : point) }
      : data
    if (!isTripTrack(track) || track.tripId !== tripId) throw new Error('Invalid trip track response')
    return track
  },
  previewBooking: (body: BookingInput) => request<BookingPreview>('/api/booking/staff/v1/booking-previews', { method: 'POST', body: {
    guestId: body.guestId, propertyId: body.propertyId, pickupLocationId: body.pickupLocationId,
    dropoffLocationId: body.dropoffLocationId, pickupAt: body.pickupAt, pickupMode: body.pickupMode,
    serviceType: body.serviceType, productCode: body.productCode, vehicleClass: body.vehicleClass,
    passengers: body.passengers, luggage: body.luggage,
  } }),
  createBooking: (body: BookingInput, key = idempotencyKey('staffTrip')) => request<Trip>('/api/booking/staff/v1/trips', { method: 'POST', body, headers: { 'Idempotency-Key': key } }),
  dispatchOrders: (page = 1) => request<DispatchSummary[]>(`/api/dispatch/staff/v1/dispatch-orders${query({ page, pageSize: 20 })}`),
  dispatchOrderSummaries: () => request<DispatchOrderSummary>('/api/dispatch/staff/v1/dispatch-orders/summaries'),
  dispatchOrder: (tripId: string) => request<DispatchSummary>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}`),
  dispatchCandidates: (tripId: string) => request<DispatchCandidate[]>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/drivers?page=1&pageSize=50`),
  shiftBoard: (pickupAt: string, propertyId?: string) => request<ShiftBoardView>(`/api/dispatch/staff/v1/shifts/board${query({ pickupAt: pickupTimeUtc(pickupAt), propertyId })}`),
  dispatchOffers: (tripId: string) => request<DispatchOffer[]>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/offers?page=1&pageSize=20`),
  offerDriver: (tripId: string, driverId: string, version: number, key = idempotencyKey('dispatch')) => request<unknown>(`/api/dispatch/staff/v1/dispatch-orders/${encodeURIComponent(tripId)}/offers`, { method: 'POST', body: { driverId }, headers: { 'Idempotency-Key': key, 'X-Expected-Version': String(version) } }),
  notices: (unread = false, page = 1) => request<StaffNotice[]>(`/api/booking/staff/v1/console-notifications${query({ page, pageSize: 50, unread: unread || undefined })}`),
  unreadCount: () => request<{ unreadCount: number }>('/api/booking/staff/v1/console-notifications/unread-count'),
  markNoticeRead: (notificationId: string) => request<unknown>(`/api/booking/staff/v1/console-notifications/${encodeURIComponent(notificationId)}/read`, { method: 'POST' }),
  markAllNoticesRead: () => request<unknown>('/api/booking/staff/v1/console-notifications/read-all', { method: 'POST' }),
}
