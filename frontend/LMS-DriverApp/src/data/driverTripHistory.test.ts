import { describe, expect, it } from 'vitest'
import type { DriverTripDetail, DriverTripSummary } from '@/api/driver'
import { mapDriverTripDetail, mapDriverTripSummary } from './driverTripHistory'

const summary: DriverTripSummary = {
  tripId: 'trip_123', tripReference: 'TR-20261016-2', serviceType: 'ONE_WAY', status: 'COMPLETED',
  stage: 'COMPLETED', vehicleClass: 'LUXURY_SEDAN', passengers: 2, version: 3,
  route: { pickupLocationId: 'loc_a', pickupName: 'Macau Ferry Terminal', dropoffLocationId: 'loc_b', dropoffName: 'Studio City', isCrossBorder: false },
  pickupAt: '2026-10-16T14:00:00Z', acceptedAt: '2026-10-16T13:30:00Z',
  pickedUpAt: '2026-10-16T14:02:00Z', completedAt: '2026-10-16T14:35:00Z', closedAt: '2026-10-16T14:35:00Z',
}

describe('driver trip history mapping', () => {
  it('renders list cards entirely from the driver trip summary', () => {
    const card = mapDriverTripSummary(summary)
    expect(card).toMatchObject({
      id: 'trip_123', displayId: 'TR-20261016-2', service: 'ONE WAY', status: 'Completed',
      pickup: 'Macau Ferry Terminal', dropoff: 'Studio City',
    })
    expect(card.accepted).not.toBe('—')
    expect(card.started).not.toBe('—')
    expect(card.ended).not.toBe('—')
  })

  it('does not invent actual times for a cancelled trip', () => {
    const card = mapDriverTripSummary({ ...summary, status: 'CANCELLED', acceptedAt: undefined, pickedUpAt: undefined, completedAt: undefined })
    expect(card.status).toBe('Cancelled')
    expect(card.accepted).toBe('—')
    expect(card.started).toBe('—')
    expect(card.ended).toBe('—')
  })

  it('loads passenger details only from the detail response', () => {
    const detail: DriverTripDetail = {
      ...summary,
      passenger: { name: 'Wei Chen', phoneMasked: '+853 6***0088' },
      specialRequests: 'Two suitcases',
      assignment: { offerId: 'offer_1', vehicleLabel: 'Mercedes S-Class' },
      fulfillment: { acceptedAt: summary.acceptedAt, pickedUpAt: summary.pickedUpAt, completedAt: summary.completedAt },
      events: [], allowedActions: [], contact: { canContactPassenger: false },
    }
    const order = mapDriverTripDetail(detail)
    expect(order).toMatchObject({ customerName: 'Wei Chen', customerInitials: 'WC', customerPhone: '+853 6***0088', passengers: 2, specialInstructions: 'Two suitcases' })
    expect(order.pickupAddress).toBe('')
    expect(order.dropoffAddress).toBe('')
  })
})
