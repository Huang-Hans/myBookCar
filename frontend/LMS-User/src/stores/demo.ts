import { defineStore } from 'pinia'
import { ref } from 'vue'
import { demoApprovals, demoBookings, demoDrivers, demoGuests, demoLocations, demoNotices } from '../data/demo'
import type { Approval, Booking, Driver, Guest, Location, Notice } from '../data/demo'
import { isDemoMode } from '../api/session'
import { lms, mapDriver, mapGuest, mapLocation, mapTrip, type PropertyView, type StaffPreferencesView } from '../api/lms'

export const useDemoStore = defineStore('demo', () => {
  const drivers = ref<Driver[]>(isDemoMode ? structuredClone(demoDrivers) : [])
  const guests = ref<Guest[]>(isDemoMode ? structuredClone(demoGuests) : [])
  const bookings = ref<Booking[]>(isDemoMode ? structuredClone(demoBookings) : [])
  const locations = ref<Location[]>(isDemoMode ? structuredClone(demoLocations) : [])
  const recentLocationIds = ref<string[]>([])
  const approvals = ref<Approval[]>(isDemoMode ? structuredClone(demoApprovals) : [])
  const notices = ref<Notice[]>(isDemoMode ? structuredClone(demoNotices) : [])
  const tripStatusCounts = ref<Record<string, number>>({})
  const unreadNotificationCount = ref(0)
  const dataError = ref('')
  const isLoading = ref(false)
  let propertiesRequest: Promise<PropertyView[]> | null = null
  let locationsLoaded = false

  async function loadProperties(): Promise<PropertyView[]> {
    if (isDemoMode) return []
    propertiesRequest ??= lms.properties().catch((error: unknown) => { propertiesRequest = null; throw error })
    return propertiesRequest
  }
  async function loadLocations(): Promise<void> {
    if (isDemoMode || locationsLoaded) return
    locationsLoaded = true
    try { locations.value = (await lms.locations()).map(mapLocation) }
    catch (error) { locationsLoaded = false; dataError.value = error instanceof Error ? error.message : 'Unable to load locations.'; throw error }
  }
  function resetLoads(): void { propertiesRequest = null; locationsLoaded = false }
  async function persistDriver(driver: Driver, previous?: Driver): Promise<void> {
    if (isDemoMode) { saveDriver(driver); return }
    if (driver.version && !previous) throw new Error('The current driver details are required before editing.')
    const result = driver.version && previous ? await lms.updateDriver(driver, previous) : await lms.createDriver(driver)
    saveDriver(mapDriver(result))
  }
  async function persistDriverStatus(driver: Driver, status: Driver['status']): Promise<void> {
    if (isDemoMode) { setDriverStatus(driver.driverId, status); return }
    const result = await lms.updateDriver({ ...driver, status }, driver)
    saveDriver(mapDriver(result))
  }
  const blacklistHits = ref<{ hitId: string; guestId: string; guestName: string; occurredAt: string; reason?: string }[]>([])

  function applyPreferences(value: StaffPreferencesView): void {
    recentLocationIds.value = value.recentLocationIds
    locations.value.forEach((location) => { location.isFavorite = value.favoriteLocationIds.includes(location.locationId) })
  }
  function replaceGuests(rows: Guest[]): void { guests.value = rows }
  function replaceBookings(rows: Booking[]): void { bookings.value = rows }
  function replaceNotices(rows: Notice[]): void { notices.value = rows }
  function setTripStatusCounts(counts: Record<string, number>): void { tripStatusCounts.value = counts }
  function setUnreadCount(count: number): void { unreadNotificationCount.value = count }
  function saveDriver(driver: Driver): void {
    const index = drivers.value.findIndex((item) => item.driverId === driver.driverId)
    if (index < 0) drivers.value.unshift(driver)
    else drivers.value[index] = driver
  }
  function setDriverStatus(driverId: string, status: Driver['status']): void {
    const driver = drivers.value.find((item) => item.driverId === driverId)
    if (driver) driver.status = status
  }
  function addBooking(booking: Booking): void {
    bookings.value.unshift(booking)
    if (isDemoMode) notices.value.unshift({ noticeId: `n_${Date.now()}`, category: 'Booking confirmed', title: 'Booking created', message: `Booking ${booking.bookingId} was created in this local demo.`, createdAt: new Date().toISOString(), isRead: false })
  }
  function updateBooking(bookingId: string, changes: Partial<Booking>): void {
    const booking = bookings.value.find((item) => item.bookingId === bookingId)
    if (booking) Object.assign(booking, changes)
  }
  function addApproval(approval: Approval): void { approvals.value.unshift(approval) }
  function saveApproval(approval: Approval): void { const index = approvals.value.findIndex((item) => item.approvalId === approval.approvalId); if (index < 0) approvals.value.unshift(approval); else approvals.value[index] = approval }
  function updateApproval(approvalId: string, status: Approval['status']): void {
    const approval = approvals.value.find((item) => item.approvalId === approvalId)
    if (approval) approval.status = status
  }
  function toggleFavorite(locationId: string): void {
    const location = locations.value.find((item) => item.locationId === locationId)
    if (location) location.isFavorite = !location.isFavorite
  }
  async function persistFavorite(locationId: string): Promise<void> {
    if (isDemoMode) { toggleFavorite(locationId); return }
    const ids = locations.value.filter((location) => location.isFavorite && location.locationId !== locationId).map((location) => location.locationId)
    if (!locations.value.find((location) => location.locationId === locationId)?.isFavorite) ids.push(locationId)
    const result = await lms.updateLocationFavorites(ids)
    locations.value.forEach((location) => { location.isFavorite = result.favoriteLocationIds.includes(location.locationId) })
  }
  function markLocationRecent(locationId: string): void {
    recentLocationIds.value = [locationId, ...recentLocationIds.value.filter((id) => id !== locationId)].slice(0, 6)
  }
  function markRead(noticeId: string): void {
    const notice = notices.value.find((item) => item.noticeId === noticeId)
    if (notice) notice.isRead = true
  }
  function markAllRead(): void { notices.value.forEach((notice) => { notice.isRead = true }) }
  function addNotice(notice: Notice): void { notices.value.unshift(notice) }
  function setGuestRestriction(guestId: string, isRestricted: boolean): void {
    const guest = guests.value.find((item) => item.guestId === guestId)
    if (guest) guest.isBlacklisted = isRestricted
  }
  function recordBlacklistHit(guestId: string, guestName: string): void {
    blacklistHits.value.unshift({ hitId: `hit_${Date.now()}`, guestId, guestName, occurredAt: new Date().toISOString() })
  }

  return { drivers, guests, bookings, locations, recentLocationIds, approvals, notices, blacklistHits, tripStatusCounts, unreadNotificationCount, dataError, isLoading, loadProperties, loadLocations, resetLoads, persistDriver, persistDriverStatus, persistFavorite, saveDriver, setDriverStatus, addBooking, updateBooking, replaceGuests, replaceBookings, replaceNotices, setTripStatusCounts, setUnreadCount, addApproval, saveApproval, updateApproval, toggleFavorite, markLocationRecent, markRead, markAllRead, addNotice, setGuestRestriction, recordBlacklistHit }
})
