import { apiRequest, buildQuery, createIdempotencyKey, type ApiEnvelope } from './http'

export type ProductCode = 'BUSINESS_SEDAN' | 'LIMOUSINE' | 'PARTY' | 'CROSS_BORDER_TRANSFER'
export type VehicleClass = 'BUSINESS_SEDAN' | 'LUXURY_SEDAN' | 'LUXURY_MPV' | 'MAYBACH'
export type TripGroup = 'ACTIVE' | 'COMPLETED' | 'CANCELLED'
export type TripStatus = 'PENDING' | 'CONFIRMED' | 'DRIVER_ASSIGNED' | 'DRIVER_EN_ROUTE' | 'ARRIVED_AT_PICKUP' | 'IN_PROGRESS' | 'COMPLETED' | 'CANCELLED'
export type LanguageCode = 'EN' | 'ZH_HANT' | 'ZH_HANS'
export type FavoriteUsage = 'PICKUP' | 'DROPOFF'

export interface TripRoute { pickupLocationId: string; pickupName: string; dropoffLocationId: string; dropoffName: string; isCrossBorder: boolean }
export interface CustomerTripSummary { tripId: string; tripReference: string; productCode?: ProductCode; serviceType: string; status: TripStatus; group: TripGroup; stage: string; vehicleClass: VehicleClass; passengers: number; route: TripRoute; pickupAt: string; createdAt: string; canCancel: boolean; version: number }
export interface CustomerTrip { summary: CustomerTripSummary; specialRequests?: string; assignmentStatus: 'UNASSIGNED' | 'ASSIGNED'; driver?: { name: string; vehicleLabel: string; canContact: boolean }; timeline: Array<{ type: string; status: TripStatus; occurredAt: string }>; points: { required: number; state: string }; cancellation?: { reasonCode: string; cancelledAt: string }; rebookInput?: { productCode?: ProductCode; pickupLocationId: string; dropoffLocationId: string; vehicleClass: VehicleClass; passengers: number; specialRequests?: string } }
export interface CustomerDriverContact { phoneNumber: string }
export interface CustomerSummary { guestId: string; name: string; tierCode: string; memberId?: string; propertyId?: string; preferredLanguage?: string; canBook: boolean; bookingBlockReason?: string; currentTrip?: CustomerTripSummary }
export interface ServiceOfferings { cities: Array<{ cityCode: 'MACAU' | 'HONG_KONG'; name: string; regions: string[] }>; products: Array<{ productCode: ProductCode; name: string; description: string; routeScope: 'LOCAL' | 'CROSS_BORDER'; isBookable: boolean; unavailableReason?: string }>; rules: { minimumLeadTimeHours: number; maximumAdvanceDays: number; maximumActiveRequestsPerGuestPerDay: number }; isAsapAvailable: boolean }
export interface CustomerLocation { locationId: string; name: string; address: string; cityCode: 'MACAU' | 'HONG_KONG'; region: string; categoryCode: string; operatingHours: string; coordinates?: { latitude: number; longitude: number } }
export interface RideOptions { route: TripRoute & { pickupCityCode: string; dropoffCityCode: string }; vehicles: Array<{ vehicleClass: VehicleClass; maxPassengers: number; maxLuggage: number; pointsRequired: number; isEligible: boolean; ineligibleReason?: string }> }
export interface CreateTripRequest { productCode: ProductCode; pickupLocationId: string; dropoffLocationId: string; pickupMode: 'ASAP' | 'SCHEDULED'; pickupAt?: string; vehicleClass?: VehicleClass; passengers: number; luggage?: number; specialRequests?: string; confirmationLanguage?: string }
export interface CustomerAccount { guestId: string; name: string; nameSource: string; email?: string | null; phoneMasked?: string; isPhoneChangeAvailable: boolean; memberId: string; tierCode: string; pointsBalance?: number; membershipSource: string; updatedAt?: string; version: number }
export interface CustomerPreferences { language: LanguageCode; isPushNotificationsEnabled: boolean; delivery: { isPushDeliveryAvailable: boolean }; updatedAt?: string; version: number }
export interface LocationFavorites { usage: FavoriteUsage; items: Array<{ locationId: string; name: string; region: string; isAvailable?: boolean }>; updatedAt?: string; version: number }
export interface TripReview { tripId: string; tripReference: string; rating: number; comments?: string | null; createdAt: string; updatedAt: string; version: number }
export interface ReviewableTrip { tripId: string; tripReference: string; productCode?: ProductCode; pickupName: string; dropoffName: string; pickupAt: string; completedAt?: string; canReview: boolean; review?: TripReview }
export interface CancellationPolicy { tripId: string; tripReference: string; status: TripStatus; canCancel: boolean; blockedReason?: string; policyReference?: string; reasonCode: string }

export const customerApi = {
  getSelf: () => apiRequest<CustomerSummary>('/api/booking/customer/v1/me'),
  getServiceOfferings: () => apiRequest<ServiceOfferings>('/api/booking/customer/v1/me/service-offerings'),
  listLocations: (values: { search?: string; region?: string; page?: number; pageSize?: number } = {}) => apiRequest<CustomerLocation[]>(`/api/booking/customer/v1/me/locations${buildQuery({ ...values, sort: 'name' })}`),
  getLocation: (locationId: string) => apiRequest<CustomerLocation>(`/api/booking/customer/v1/me/locations/${encodeURIComponent(locationId)}`),
  getRideOptions: (values: { productCode: ProductCode; pickupLocationId: string; dropoffLocationId: string; passengers: number }) => apiRequest<RideOptions>(`/api/booking/customer/v1/me/ride-options${buildQuery(values)}`),
  listTrips: (group: TripGroup, page = 1) => apiRequest<CustomerTripSummary[]>(`/api/booking/customer/v1/me/trips${buildQuery({ group, page, pageSize: 20, sort: '-createdAt,tripId' })}`),
  createTrip: (body: CreateTripRequest, idempotencyKey = createIdempotencyKey('trip')) => apiRequest<CustomerTrip>('/api/booking/customer/v1/me/trips', { method: 'POST', body, headers: { 'Idempotency-Key': idempotencyKey } }),
  getTrip: (tripId: string) => apiRequest<CustomerTrip>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}`),
  getDriverContact: (tripId: string) => apiRequest<CustomerDriverContact>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/driver-contacts`),
  getCancellationPolicy: (tripId: string) => apiRequest<CancellationPolicy>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/cancellation-policy`),
  cancelTrip: (tripId: string, comments?: string) => apiRequest<CustomerTrip>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/cancellations`, { method: 'POST', body: comments ? { comments } : {}, headers: { 'Idempotency-Key': createIdempotencyKey('cancel') } }),
  getProfile: () => apiRequest<CustomerAccount>('/api/booking/customer/v1/me/profile'),
  updateProfile: (body: { name?: string; email?: string | null }, version?: number) => apiRequest<CustomerAccount>('/api/booking/customer/v1/me/profile', { method: 'PATCH', body, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
  getPreferences: () => apiRequest<CustomerPreferences>('/api/booking/customer/v1/me/preferences'),
  updatePreferences: (body: { language?: LanguageCode; isPushNotificationsEnabled?: boolean }, version?: number) => apiRequest<CustomerPreferences>('/api/booking/customer/v1/me/preferences', { method: 'PATCH', body, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
  getFavorites: (usage: FavoriteUsage) => apiRequest<LocationFavorites>(`/api/booking/customer/v1/me/location-favorites/${usage}`),
  replaceFavorites: (usage: FavoriteUsage, locationIds: string[], version?: number) => apiRequest<LocationFavorites>(`/api/booking/customer/v1/me/location-favorites/${usage}`, { method: 'PUT', body: { items: locationIds.map((locationId) => ({ locationId })) }, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
  listReviewableTrips: (page = 1) => apiRequest<ReviewableTrip[]>(`/api/booking/customer/v1/me/reviewable-trips${buildQuery({ page, pageSize: 20, sort: '-completedAt,tripId' })}`),
  getReview: (tripId: string) => apiRequest<TripReview>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/review`),
  saveReview: (tripId: string, rating: number, comments: string | null, version?: number): Promise<ApiEnvelope<TripReview>> => apiRequest<TripReview>(`/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/review`, { method: 'PUT', body: { rating, comments }, headers: version === undefined ? {} : { 'X-Expected-Version': String(version) } }),
}

export function serviceIdToProductCode(serviceId: string): ProductCode {
  if (serviceId === 'business-sedan') return 'BUSINESS_SEDAN'
  if (serviceId === 'party') return 'PARTY'
  if (serviceId === 'cross-border') return 'CROSS_BORDER_TRANSFER'
  return 'LIMOUSINE'
}

export function productCodeToServiceId(productCode?: ProductCode): 'business-sedan' | 'limousine' | 'party' | 'cross-border' {
  if (productCode === 'BUSINESS_SEDAN') return 'business-sedan'
  if (productCode === 'PARTY') return 'party'
  if (productCode === 'CROSS_BORDER_TRANSFER') return 'cross-border'
  return 'limousine'
}

export function customerTripStatusLabel(status: TripStatus): string {
  const labels: Record<TripStatus, string> = {
    PENDING: 'Pending confirmation',
    CONFIRMED: 'Awaiting driver assignment',
    DRIVER_ASSIGNED: 'Confirmed',
    DRIVER_EN_ROUTE: 'Driver on the Way',
    ARRIVED_AT_PICKUP: 'Arrived',
    IN_PROGRESS: 'In Progress',
    COMPLETED: 'Completed',
    CANCELLED: 'Cancelled',
  }
  return labels[status]
}

export function customerTripNotice(trip?: CustomerTripSummary): string {
  if (!trip || trip.status === 'COMPLETED' || trip.status === 'CANCELLED') return ''
  const detail = trip.status === 'DRIVER_ASSIGNED' ? 'Your Driver Is Preparing for Pickup' : customerTripStatusLabel(trip.status)
  return `${trip.route.pickupName} · ${detail}`
}
