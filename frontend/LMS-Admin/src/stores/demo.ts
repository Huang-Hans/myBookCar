import { defineStore } from 'pinia'
import { ref } from 'vue'
import { demoApprovals, demoBookings, demoDrivers, demoGuests, demoLocations, demoNotices } from '../data/demo'
import type { Approval, Booking, Driver, Guest, Location, Notice } from '../data/demo'
import { isDemoMode, loadCurrentUser } from '../api/session'
import { lms, mapDriver, mapGuest, mapLocation, mapNotice, mapQualification, mapTrip } from '../api/lms'
import { ApiRequestError } from '../api/http'

export const useDemoStore = defineStore('demo', () => {
  const drivers = ref<Driver[]>(isDemoMode ? structuredClone(demoDrivers) : [])
  const guests = ref<Guest[]>(isDemoMode ? structuredClone(demoGuests) : [])
  const bookings = ref<Booking[]>(isDemoMode ? structuredClone(demoBookings) : [])
  const locations = ref<Location[]>(isDemoMode ? structuredClone(demoLocations) : [])
  const recentLocationIds = ref<string[]>([])
  const approvals = ref<Approval[]>(isDemoMode ? structuredClone(demoApprovals) : [])
  const notices = ref<Notice[]>(isDemoMode ? structuredClone(demoNotices) : [])
  const dataError = ref('')
  const isLoading = ref(false)
  let loaded = false

  async function load(): Promise<void> {
    if (isDemoMode || loaded) return
    isLoading.value = true; dataError.value = ''
    try {
      const [driverRows, guestRows, tripRows, locationRows, approvalRows, noticeRows, identity, preferences, hits] = await Promise.allSettled([
        lms.drivers(), lms.guests(), lms.trips(), lms.locations(), lms.qualifications(), lms.notifications(), loadCurrentUser(), lms.staffPreferences(), lms.blacklistHits(),
      ])
      if (driverRows.status === 'fulfilled') drivers.value = driverRows.value.map(mapDriver)
      if (guestRows.status === 'fulfilled') guests.value = guestRows.value.map(mapGuest)
      if (tripRows.status === 'fulfilled') bookings.value = tripRows.value.map(mapTrip)
      if (locationRows.status === 'fulfilled') locations.value = locationRows.value.map(mapLocation)
      if (approvalRows.status === 'fulfilled') approvals.value = approvalRows.value.map(mapQualification)
      if (noticeRows.status === 'fulfilled') notices.value = noticeRows.value.map(mapNotice)
      if (preferences.status === 'fulfilled') {
        recentLocationIds.value = preferences.value.recentLocationIds
        locations.value.forEach((location) => { location.isFavorite = preferences.value.favoriteLocationIds.includes(location.locationId) })
      }
      if (hits.status === 'fulfilled') blacklistHits.value = hits.value.map((item) => ({ hitId: item.approvalId, guestId: item.targetId, guestName: item.targetId, reason: item.reason ?? 'Blocked booking attempt', occurredAt: '' }))
      const sourceNames = ['drivers', 'guests', 'trips', 'locations', 'qualifications', 'notifications', 'current user', 'staff preferences', 'blacklist hits']
      const results = [driverRows, guestRows, tripRows, locationRows, approvalRows, noticeRows, identity, preferences, hits]
      const failed = results.flatMap((result, index) => {
        if (result.status !== 'rejected') return []
        const reason = result.reason as unknown
        const detail = reason instanceof ApiRequestError ? `HTTP ${reason.status}, traceId: ${reason.traceId}` : 'network error'
        return [`${sourceNames[index] ?? 'unknown source'} (${detail})`]
      })
      if (failed.length) dataError.value = `LMS data unavailable: ${failed.join('; ')}. Retry or contact support with the traceId.`
      loaded = true
    } catch (error) { dataError.value = error instanceof Error ? error.message : 'Unable to load LMS data.' }
    finally { isLoading.value = false }
  }
  async function reload(): Promise<void> { loaded = false; await load() }
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

  return { drivers, guests, bookings, locations, recentLocationIds, approvals, notices, blacklistHits, dataError, isLoading, load, reload, persistDriver, persistDriverStatus, persistFavorite, saveDriver, setDriverStatus, addBooking, updateBooking, addApproval, saveApproval, updateApproval, toggleFavorite, markLocationRecent, markRead, markAllRead, addNotice, setGuestRestriction, recordBlacklistHit }
})
