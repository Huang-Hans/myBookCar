import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useDemoStore } from './demo'

describe('demo state', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('saves a driver and changes status without mutating other records', () => {
    const store = useDemoStore()
    const originalCount = store.drivers.length
    store.saveDriver({ driverId: 'DR-test', fullName: 'Driver Test', licenseNumber: 'MAC-test', phoneMasked: '—', propertyName: 'Studio City', vehicleName: 'Demo Vehicle', availability: 'Off Duty', status: 'Active', emergencyContact: '', languagesSpoken: 'English', assignedProperties: ['Studio City'], blacklistReason: '', blacklistEffectiveDate: '', blacklistExpiryDate: '' })
    expect(store.drivers).toHaveLength(originalCount + 1)
    store.setDriverStatus('DR-test', 'Inactive')
    expect(store.drivers.find((driver) => driver.driverId === 'DR-test')?.status).toBe('Inactive')
    expect(store.drivers.find((driver) => driver.driverId === 'DR-1021')?.status).toBe('Active')
  })

  it('creates a booking and an unread local notification', () => {
    const store = useDemoStore()
    const originalCount = store.bookings.length
    store.addBooking({ bookingId: 'TR-test', guestId: 'g_001', guestName: 'Guest A', roomNumber: '1808', pickupLocation: 'Macau International Airport', dropoffLocation: 'City of Dreams', pickupAt: '2026-10-09T06:30:00Z', propertyName: 'Nuwa', vehicleClass: 'Luxury Sedan', status: 'Pending', points: 0 })
    expect(store.bookings).toHaveLength(originalCount + 1)
    expect(store.notices[0]?.isRead).toBe(false)
    store.updateBooking('TR-test', { status: 'Cancelled' })
    expect(store.bookings[0]?.status).toBe('Cancelled')
  })

  it('marks notifications read and toggles location favorites', () => {
    const store = useDemoStore()
    const locationId = store.locations[0]?.locationId ?? ''
    const originalFavorite = store.locations[0]?.isFavorite
    store.toggleFavorite(locationId)
    expect(store.locations[0]?.isFavorite).toBe(!originalFavorite)
    store.markAllRead()
    expect(store.notices.every((notice) => notice.isRead)).toBe(true)
  })

  it('keeps a unique, bounded list of recently selected locations', () => {
    const store = useDemoStore()
    for (const location of store.locations) store.markLocationRecent(location.locationId)
    const firstLocationId = store.locations[0]?.locationId ?? ''
    store.markLocationRecent(firstLocationId)
    expect(store.recentLocationIds[0]).toBe(firstLocationId)
    expect(store.recentLocationIds).toHaveLength(store.locations.length)
    expect(new Set(store.recentLocationIds).size).toBe(store.recentLocationIds.length)
  })
})
