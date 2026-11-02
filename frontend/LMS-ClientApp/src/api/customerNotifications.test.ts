import { describe, expect, it } from 'vitest'
import { customerTripNotice, customerTripStatusLabel, type CustomerTripSummary, type TripStatus } from './customer'

const trip = (status: TripStatus): CustomerTripSummary => ({
  tripId: 'trip_001', tripReference: 'TR-001', serviceType: 'LIMOUSINE', status, group: 'ACTIVE', stage: status,
  vehicleClass: 'LUXURY_SEDAN', passengers: 1, route: { pickupLocationId: 'loc_1', pickupName: 'City of Dreams', dropoffLocationId: 'loc_2', dropoffName: 'Airport', isCrossBorder: false },
  pickupAt: '2026-10-15T03:00:00Z', createdAt: '2026-10-13T08:30:00Z', canCancel: true, version: 1,
})

describe('customer trip status presentation', () => {
  it('maps backend states to the agreed status tracker labels', () => {
    expect(customerTripStatusLabel('CONFIRMED')).toBe('Awaiting driver assignment')
    expect(customerTripStatusLabel('DRIVER_ASSIGNED')).toBe('Confirmed')
    expect(customerTripStatusLabel('DRIVER_EN_ROUTE')).toBe('Driver on the Way')
    expect(customerTripStatusLabel('ARRIVED_AT_PICKUP')).toBe('Arrived')
  })

  it('shows preparation after assignment and removes closed trips from the active notice', () => {
    expect(customerTripNotice(trip('DRIVER_ASSIGNED'))).toContain('Your Driver Is Preparing for Pickup')
    expect(customerTripNotice(trip('COMPLETED'))).toBe('')
    expect(customerTripNotice(trip('CANCELLED'))).toBe('')
  })
})
