import { describe, expect, it } from 'vitest'
import { mapApproval, mapDriver, mapGuest, mapLocation, mapNotice, mapTrip } from './lms'

describe('LMS response mappers', () => {
  it('maps driver status, availability and version from the Dispatch schema', () => {
    const driver = mapDriver({ driverId: 'drv_1', name: 'Alex', licenseNumber: 'MAC-1', licenseExpiryDate: '2027-01-01', phone: '+8531234', propertyId: 'COD', assignedVehicle: 'MPV', status: 'ACTIVE', languages: ['English'], availability: 'ON_TRIP', version: 3 })
    expect(driver).toMatchObject({ driverId: 'drv_1', propertyName: 'City of Dreams', availability: 'On Trip', status: 'Active', version: 3 })
  })
  it('maps a booking using the stable trip ID for mutations', () => {
    const booking = mapTrip({ tripId: 'trip_1', tripReference: 'TR-1', propertyId: 'SC', guest: { guestId: 'guest_1', name: 'Guest' }, route: { pickupLocationId: 'loc_1', pickupName: 'Lobby', dropoffLocationId: 'loc_2', dropoffName: 'Airport' }, pickupAt: '2026-10-12T08:00:00Z', serviceType: 'ONE_WAY', vehicleClass: 'LUXURY_MPV', status: 'CONFIRMED', version: 2 })
    expect(booking).toMatchObject({ bookingId: 'trip_1', pickupLocationId: 'loc_1', dropoffLocationId: 'loc_2', status: 'Confirmed', version: 2 })
  })
  it('maps guest, location, approval and notification envelopes without demo identifiers', () => {
    expect(mapGuest({ guestId: 'guest_1', name: 'Guest', tier: 'DIAMOND', inHouse: true })).toMatchObject({ tier: 'Diamond', isInHouse: true })
    expect(mapLocation({ locationId: 'loc_1', name: 'Airport', categoryCode: 'AIRPORT', address: 'Terminal', region: 'AIRPORT', isActive: true, version: 1 }).category).toBe('Airport')
    expect(mapApproval({ approvalId: 'approval_1', tripReference: 'XB-1', guest: { guestId: 'guest_1', name: 'Guest', tierCode: 'VIP' }, propertyId: 'COD', route: {}, pickupAt: '2026-10-12T08:00:00Z', documents: [], status: 'PENDING', version: 5 })).toMatchObject({ status: 'Pending Approval', version: 5 })
    expect(mapNotice({ notificationId: 'notice_1', type: 'DRIVER_ASSIGNED', summary: 'Driver allocated', occurredAt: '2026-10-12T08:00:00Z', isRead: false }).category).toBe('Driver assigned')
  })
})
