<script setup lang="ts">
import { useLocale } from '../i18n'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Bell, CalendarDays, X } from 'lucide-vue-next'
import DataPager from '../components/DataPager.vue'
import ShellLayout from '../components/ShellLayout.vue'
import ConsoleCouponsPage from './console/ConsoleCouponsPage.vue'
import ConsoleDispatchPage from './console/ConsoleDispatchPage.vue'
import ConsoleDriversPage from './console/ConsoleDriversPage.vue'
import ConsoleMembersPage from './console/ConsoleMembersPage.vue'
import ConsoleGuestBookingForm from './console/ConsoleGuestBookingForm.vue'
import ConsoleReportsPage from './console/ConsoleReportsPage.vue'
import ConsoleRosterPage from './console/ConsoleRosterPage.vue'
import ConsolePropertiesPage from './console/ConsolePropertiesPage.vue'
import ConsoleTripTrackDialog from './console/ConsoleTripTrackDialog.vue'
import ConsoleVehiclesPage from './console/ConsoleVehiclesPage.vue'
import { useDemoStore } from '../stores/demo'
import type { Approval, Booking, BookingStatus, Notice } from '../data/demo'
import { isDemoMode, currentUser, loadCurrentUser } from '../api/session'
import { lms, mapNotice, mapQualification, mapTrip, notificationTypes, type TripChannel, type TripTimelineEventView } from '../api/lms'
import { apiErrorMessage } from '../api/http'
import { bookingNotifications, bookingNotificationUnreadCount, markBookingNotificationRead, refreshBookingNotificationSnapshot } from '../api/notifications'
const { dateLocale, t } = useLocale()

const route = useRoute()
const router = useRouter()
const store = useDemoStore()
const section = computed(() => String(route.params.section ?? 'booking'))
const cancellationReasons = ref<{ reasonCode: string; name: string }[]>([])
const properties = ref<{ propertyId: string; name: string }[]>([])
const tripEvents = ref<TripTimelineEventView[]>([])
const cancellationPolicy = ref('')
let tripGeneration = 0
let reasonsRequested = false
function errorMessage(error: unknown): string { return apiErrorMessage(error) }
const demoStaffId = 'FD-SL-003'
function bookingSourceLabel(booking: Booking): string {
  return booking.channel === 'SELF_SERVICE' ? 'Customer Web' : 'Front Desk'
}
const titles: Record<string, string> = { booking: 'Quick Booking', tracking: 'Trip Status Tracking', history: 'Booking History', approvals: 'Approval Requests', manage: 'Booking List', notifications: 'Notifications', coupons: 'Coupon Management', vehicles: 'Vehicle Management', drivers: 'Driver Management', roster: 'Driver Roster', members: 'Member Information', properties: 'Property Management' }
const title = computed(() => titles[section.value] ?? 'Quick Booking')
const subtitle = computed(() => {
  if (section.value === 'manage') return 'View customer bookings and arrange transfers on behalf of guests.'
  if (section.value === 'properties') return 'Maintain property details, contacts and service settings.'
  if (section.value === 'members') return 'Dispatch a member directly, with a coupon, or by membership tier.'
  if (section.value === 'coupons') return 'Issue complimentary trips, fare credits and member offers.'
  if (section.value === 'vehicles') return 'Track fleet class, assignment and workshop status.'
  if (section.value === 'drivers') return 'Keep driver contact, licence and vehicle assignment in one list.'
  if (section.value === 'roster') return 'Schedule a driver first, then assign a vehicle to that shift.'
  return 'Seamless transfers. Exceptional guest experiences.'
})
const today = computed(() => new Intl.DateTimeFormat(dateLocale.value, { weekday: 'long', day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Macau' }).format(new Date()))
const operatorName = computed(() => isDemoMode ? t('Front Desk Agent') : currentUser.value?.name || '—')
const operatorDetail = computed(() => {
  if (isDemoMode) return t('Service Desk')
  const user = currentUser.value
  if (!user) return ''
  const property = user.hasAllPropertyAccess ? '' : user.propertyId ?? ''
  return [user.roleCode, property].filter(Boolean).join(' · ')
})
const tripPage = ref(1)
const tripPageSize = ref(20)
const tripTotalItems = ref(0)
const bookingTab = ref<TripChannel>('CONSOLE')
const isCreatingBooking = ref(false)
const isLoadingTrips = ref(false)
const tripLoadError = ref('')
const canManageBookings = computed(() => isDemoMode || Boolean(currentUser.value?.permissions.includes('bookings:write')))
const statusFilter = ref('All')
const propertyFilter = ref('')
const bookingSearch = ref('')
const bookingError = ref('')
const approvalTab = ref('All')
const notificationTab = ref('All')
const notificationCategories = ['Booking confirmed', 'Driver assigned', 'Driver arrived', 'Trip completed', 'Approval required', 'System alerts'] as const
const notificationPreferenceKey = 'lmsConsoleNoticeCategories'
function savedNotificationCategories(): string[] {
  if (!isDemoMode) return [...notificationCategories]
  try {
    const saved: unknown = JSON.parse(window.localStorage.getItem(notificationPreferenceKey) ?? 'null')
    return Array.isArray(saved) ? notificationCategories.filter((category) => saved.includes(category)) : [...notificationCategories]
  } catch { return [...notificationCategories] }
}
const enabledNotificationCategories = ref<string[]>(savedNotificationCategories())
function loadNotificationPreferences(): void {
  if (isDemoMode || section.value !== 'notifications') return
  void lms.staffPreferences().then((value) => { enabledNotificationCategories.value = notificationCategories.filter((category) => value.enabledNotificationTypes.includes(notificationTypes[category] ?? '')) }).catch((error) => { store.dataError = error instanceof Error ? error.message : 'Unable to load notification preferences.' })
}
const selectedBooking = ref<Booking | null>(null)
const bookingDialog = ref<'details' | 'modify' | 'cancel' | null>(null)
const cancellationReason = ref('')
const editingPickupLocal = ref('')
const editingPickupLocation = ref('')
const approvalDialog = ref(false)
const selectedApproval = ref<Approval | null>(null)
const approvalGuestId = ref('')
const approvalReason = ref('')
const approvalError = ref('')
const historyFrom = ref('')
const historyTo = ref('')

const statuses: BookingStatus[] = ['Pending', 'Confirmed', 'Driver Assigned', 'Driver En Route', 'Arrived at Pickup', 'In Progress', 'Completed', 'Cancelled']
const statusCodes = new Map<BookingStatus, string>([
  ['Pending', 'PENDING'],
  ['Confirmed', 'CONFIRMED'],
  ['Driver Assigned', 'DRIVER_ASSIGNED'],
  ['Driver En Route', 'DRIVER_EN_ROUTE'],
  ['Arrived at Pickup', 'ARRIVED_AT_PICKUP'],
  ['In Progress', 'IN_PROGRESS'],
  ['Completed', 'COMPLETED'],
  ['Cancelled', 'CANCELLED'],
])
function statusCount(status: BookingStatus): number {
  if (!isDemoMode && Object.keys(store.tripStatusCounts).length > 0) return store.tripStatusCounts[statusCodes.get(status) ?? ''] ?? 0
  return store.bookings.filter((booking) => booking.status === status).length
}
const filteredBookings = computed(() => {
  if (!isDemoMode && (isLoadingTrips.value || tripLoadError.value)) return []
  if (!isDemoMode) return statusFilter.value === 'Active' ? store.bookings.filter((booking) => !['Completed', 'Cancelled'].includes(booking.status)) : store.bookings
  return store.bookings.filter((booking) => {
    const matchesText = `${booking.tripReference ?? ''} ${booking.bookingId} ${booking.guestName} ${booking.roomNumber} ${booking.pickupLocation}`.toLowerCase().includes(bookingSearch.value.toLowerCase().trim())
    const matchesStatus = statusFilter.value === 'All' || statusFilter.value === 'Active' && !['Completed', 'Cancelled'].includes(booking.status) || booking.status === statusFilter.value
    const matchesProperty = !propertyFilter.value || booking.propertyName === propertyFilter.value
    const pickupDate = macauDate(booking.pickupAt)
    const matchesDate = section.value !== 'history' || (!historyFrom.value || pickupDate >= historyFrom.value) && (!historyTo.value || pickupDate <= historyTo.value)
    const matchesChannel = section.value !== 'manage' || (booking.channel ?? 'CONSOLE') === bookingTab.value
    return matchesText && matchesStatus && matchesProperty && matchesDate && matchesChannel
  })
})
const filteredApprovals = computed(() => store.approvals.filter((approval) => approvalTab.value === 'All' || approval.status === approvalTab.value))
const liveBookingNotices = computed<Notice[]>(() => bookingNotifications.value.map((item) => ({
  noticeId: item.notification.notificationId ?? item.eventId,
  category: 'System alerts',
  title: 'New customer booking',
  message: item.notification.summary,
  createdAt: item.notification.occurredAt,
  isRead: item.isRead,
})))
const notificationNotices = computed(() => {
  if (isDemoMode) return store.notices
  const notices = new Map(store.notices.map((notice) => [notice.noticeId, notice]))
  liveBookingNotices.value.forEach((notice) => { notices.set(notice.noticeId, notice) })
  return [...notices.values()].sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt))
})
const filteredNotices = computed(() => notificationNotices.value.filter((notice) => enabledNotificationCategories.value.includes(notice.category) && (notificationTab.value === 'All' || notificationTab.value === 'Unread' && !notice.isRead || notice.category === notificationTab.value)))
const unreadCount = computed(() => isDemoMode ? store.notices.filter((notice) => !notice.isRead).length : Math.max(store.unreadNotificationCount, bookingNotificationUnreadCount.value))

function bookingLabel(booking: Booking): string { return booking.tripReference || booking.bookingId }
function bookingStatusLabel(booking: Pick<Booking, 'status' | 'stageLabel'>): string {
  if (section.value !== 'manage' || bookingTab.value !== 'CONSOLE') return booking.stageLabel ?? booking.status
  if (booking.status === 'Confirmed') return 'Awaiting driver assignment'
  if (booking.status === 'Driver Assigned') return 'Driver accepted'
  return booking.stageLabel ?? booking.status
}
function displayTime(value: string): string { return value && !Number.isNaN(Date.parse(value)) ? new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Macau' }).format(new Date(value)) : '—' }
function displayDate(value: string): string { return value && !Number.isNaN(Date.parse(value)) ? new Intl.DateTimeFormat(dateLocale.value, { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Macau' }).format(new Date(value)) : '—' }
function macauDate(value: string): string {
  const parts = new Intl.DateTimeFormat('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Macau' }).formatToParts(new Date(value))
  const part = (type: string): string => parts.find((item) => item.type === type)?.value ?? ''
  return `${part('year')}-${part('month')}-${part('day')}`
}
function macauBound(date: string, endExclusive: boolean): string | undefined {
  if (!date) return undefined
  const instant = new Date(`${date}T00:00:00+08:00`)
  if (Number.isNaN(instant.getTime())) return undefined
  if (endExclusive) instant.setTime(instant.getTime() + 86_400_000)
  return instant.toISOString()
}
async function refreshTrips(): Promise<void> {
  if (isDemoMode || !['tracking', 'history', 'manage'].includes(section.value)) return
  window.clearTimeout(tripTimer)
  const generation = ++tripGeneration
  isLoadingTrips.value = true; tripLoadError.value = ''
  try { properties.value = await store.loadProperties() } catch { /* Property names stay as returned identifiers. */ }
  if (generation !== tripGeneration) return
  const status = statuses.includes(statusFilter.value as BookingStatus) ? statusCodes.get(statusFilter.value as BookingStatus) : undefined
  const propertyId = properties.value.find((property) => property.name === propertyFilter.value)?.propertyId
  const from = section.value === 'history' ? macauBound(historyFrom.value, false) : undefined
  const to = section.value === 'history' ? macauBound(historyTo.value, true) : undefined
  const searchText = bookingSearch.value.trim() || undefined
  try {
    const channel = section.value === 'manage' ? bookingTab.value : undefined
    const envelope = await lms.listTripPage({ search: searchText, propertyId, status, channel, from, to, page: tripPage.value, pageSize: tripPageSize.value, sort: channel ? '-createdAt' : undefined })
    const counts = section.value === 'tracking' ? await lms.tripStatusSummaries({ search: searchText, propertyId, from, to }) : undefined
    if (generation !== tripGeneration) return
    store.replaceBookings((envelope.data ?? []).map(mapTrip))
    tripTotalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data?.length ?? 0
    if (counts) store.setTripStatusCounts(counts)
  } catch (error) { if (generation === tripGeneration) tripLoadError.value = errorMessage(error) }
  finally { if (generation === tripGeneration) isLoadingTrips.value = false }
}
async function refreshNotices(): Promise<void> {
  if (isDemoMode || section.value !== 'notifications') return
  const type = notificationTypes[notificationTab.value]
  const unread = notificationTab.value === 'Unread' ? true : undefined
  try { store.replaceNotices((await lms.notifications({ type, unread })).map(mapNotice)) }
  catch (error) { store.dataError = errorMessage(error) }
}
async function refreshUnread(): Promise<void> {
  if (isDemoMode) return
  try { store.setUnreadCount((await lms.unreadNotificationCount()).unreadCount) }
  catch (error) { store.dataError = errorMessage(error) }
}
async function focusSearch(): Promise<void> { await router.push('/console/members'); await nextTick(); document.querySelector<HTMLInputElement>('.main-content input')?.focus() }
function handleGuestBookingCreated(booking: Booking): void {
  store.addBooking(booking)
  if (tripPage.value !== 1) tripPage.value = 1
  else void refreshTrips()
  void refreshUnread()
}
async function openBooking(booking: Booking, mode: 'details' | 'modify' | 'cancel'): Promise<void> {
  selectedBooking.value = booking; bookingDialog.value = mode; editingPickupLocation.value = booking.pickupLocation
  cancellationReason.value = ''; tripEvents.value = []; cancellationPolicy.value = ''
  const date = new Date(booking.pickupAt); editingPickupLocal.value = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}T${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
  if (isDemoMode) return
  try {
    const detail = mapTrip(await lms.trip(booking.bookingId))
    store.updateBooking(booking.bookingId, detail)
    selectedBooking.value = { ...booking, ...detail }
    if (mode === 'details') tripEvents.value = await lms.tripEvents(booking.bookingId)
    if (mode === 'cancel') cancellationPolicy.value = (await lms.cancellationPolicy(booking.bookingId)).policyReference ?? ''
  } catch (error) { bookingError.value = errorMessage(error) }
}
function applyTrackedBooking(booking: Booking): void {
  store.updateBooking(booking.bookingId, booking)
  selectedBooking.value = booking
}
async function saveBooking(): Promise<void> {
  if (!selectedBooking.value || !editingPickupLocal.value || !editingPickupLocation.value) return
  if (['Completed', 'Cancelled'].includes(selectedBooking.value.status)) return
  if (Number.isNaN(new Date(editingPickupLocal.value).getTime())) return
  if (!isDemoMode) {
    const location = store.locations.find((item) => item.name === editingPickupLocation.value)
    if (!location || !selectedBooking.value.version) { bookingError.value = 'Select a known pickup location.'; return }
    try { store.updateBooking(selectedBooking.value.bookingId, mapTrip(await lms.updateTrip(selectedBooking.value.bookingId, selectedBooking.value.version, { pickupLocationId: location.locationId, pickupAt: new Date(editingPickupLocal.value).toISOString() }))); void refreshTrips() }
    catch (error) { bookingError.value = errorMessage(error); return }
  } else store.updateBooking(selectedBooking.value.bookingId, { pickupAt: new Date(editingPickupLocal.value).toISOString(), pickupLocation: editingPickupLocation.value })
  bookingDialog.value = null
}
async function cancelBooking(): Promise<void> {
  if (!selectedBooking.value || !cancellationReason.value) return
  if (['Completed', 'Cancelled'].includes(selectedBooking.value.status)) return
  if (!isDemoMode) {
    try { store.updateBooking(selectedBooking.value.bookingId, mapTrip(await lms.cancelTrip(selectedBooking.value.bookingId, cancellationReason.value))); void refreshTrips() }
    catch (error) { bookingError.value = errorMessage(error); return }
  } else store.updateBooking(selectedBooking.value.bookingId, { status: 'Cancelled', cancellationReason: cancellationReason.value })
  bookingDialog.value = null
}
async function submitApproval(): Promise<void> {
  if (!isDemoMode) {
    const qualification = store.approvals.find((item) => item.guestId === approvalGuestId.value && item.status === 'Pending Approval')
    if (!qualification || !approvalReason.value.trim()) { approvalError.value = 'Select a guest with a pending qualification reference and provide a reason.'; return }
    try { store.saveApproval(mapQualification(await lms.escalateQualification(qualification.approvalId, qualification.version ?? 0, approvalReason.value.trim()))); approvalDialog.value = false; approvalReason.value = ''; approvalGuestId.value = ''; approvalError.value = '' }
    catch (error) { approvalError.value = errorMessage(error) }
    return
  }
  const guest = store.guests.find((item) => item.guestId === approvalGuestId.value)
  if (!guest || !approvalReason.value.trim()) { approvalError.value = 'Select a guest and provide a reason.'; return }
  const approval: Approval = { approvalId: `AP-${Date.now()}`, guestName: guest.fullName, roomNumber: guest.roomNumber, tier: guest.tier, reason: approvalReason.value, status: 'Pending Approval', requestedAt: new Date().toISOString(), requestedByStaffId: demoStaffId }
  store.addApproval(approval)
  store.addNotice({ noticeId: `n_${Date.now()}`, category: 'Approval required', title: 'Approval request pending', message: `Request ${approval.approvalId} was created in this local demo.`, createdAt: new Date().toISOString(), isRead: false })
  approvalDialog.value = false; approvalReason.value = ''; approvalGuestId.value = ''; approvalError.value = ''
}
async function toggleNotificationCategory(category: string): Promise<void> {
  const next = enabledNotificationCategories.value.includes(category)
    ? enabledNotificationCategories.value.filter((item) => item !== category)
    : [...enabledNotificationCategories.value, category]
  if (!isDemoMode) {
    try { const saved = await lms.updateNotificationPreferences(next.map((item) => notificationTypes[item] ?? item)); enabledNotificationCategories.value = notificationCategories.filter((item) => saved.enabledNotificationTypes.includes(notificationTypes[item] ?? '')) }
    catch (error) { store.dataError = errorMessage(error) }
    return
  }
  enabledNotificationCategories.value = next
  try { window.localStorage.setItem(notificationPreferenceKey, JSON.stringify(next)) } catch { /* Storage may be unavailable. */ }
}
function ensureCancellationReasons(): void {
  if (isDemoMode || reasonsRequested || section.value !== 'manage') return
  reasonsRequested = true
  void lms.cancellationReasons().then((rows) => { cancellationReasons.value = rows }).catch(() => { reasonsRequested = false })
}
function ensurePageData(): void {
  if (isDemoMode) return
  if (section.value === 'manage') ensureCancellationReasons()
  if (section.value === 'notifications') {
    loadNotificationPreferences()
    void refreshUnread()
    // 实时下单通知以 SignalR 快照为准（契约第 4 节）：进入消息页面时刷新一次快照，之后由通知事件增量更新。
    void refreshBookingNotificationSnapshot().catch((error) => { store.dataError = errorMessage(error) })
  }
}
function retryPage(): void {
  store.dataError = ''
  store.resetLoads()
  reasonsRequested = false
  ensurePageData()
  if (['tracking', 'history', 'manage'].includes(section.value)) void refreshTrips()
  if (section.value === 'notifications') void refreshNotices()
}
onMounted(() => {
  if (!isDemoMode && !currentUser.value) void loadCurrentUser().catch(() => undefined)
  ensurePageData()
})
watch(section, () => { statusFilter.value = 'All'; bookingSearch.value = ''; ensurePageData() })
let tripTimer = 0
function scheduleTrips(): void {
  window.clearTimeout(tripTimer); tripGeneration++
  if (isDemoMode || !['tracking', 'history', 'manage'].includes(section.value)) { isLoadingTrips.value = false; return }
  isLoadingTrips.value = true; tripLoadError.value = ''; tripTotalItems.value = 0
  tripTimer = window.setTimeout(() => { void refreshTrips() }, 300)
}
watch([section, bookingTab, bookingSearch, statusFilter, propertyFilter, historyFrom, historyTo], () => {
  if (tripPage.value !== 1) tripPage.value = 1
  scheduleTrips()
}, { immediate: true })
watch([tripPage, tripPageSize], scheduleTrips)
watch([section, notificationTab], () => { void refreshNotices() }, { immediate: true })
async function markRead(id: string): Promise<void> {
  try {
    if (!isDemoMode) {
      const liveItem = bookingNotifications.value.find((item) => (item.notification.notificationId ?? item.eventId) === id)
      if (liveItem) await markBookingNotificationRead(liveItem)
      else await lms.markRead(id)
    }
    store.markRead(id)
    await refreshUnread()
  } catch (error) { store.dataError = errorMessage(error) }
}
async function markAllRead(): Promise<void> {
  try {
    if (!isDemoMode) {
      await lms.markAllRead()
      await refreshBookingNotificationSnapshot()
    }
    store.markAllRead(); store.setUnreadCount(0); await refreshUnread(); await refreshNotices()
  } catch (error) { store.dataError = errorMessage(error) }
}
onBeforeUnmount(() => { tripGeneration++; window.clearTimeout(tripTimer) })

</script>

<template>
  <ShellLayout mode="console" @focus-search="focusSearch">
    <p v-if="store.dataError" class="form-error" role="alert">{{ store.dataError }} <button class="text-button" @click="retryPage()">{{ t('Retry') }}</button></p>
    <div v-if="section !== 'reports' && section !== 'dispatch' && section !== 'roster'" class="console-heading"><div><h1>{{ t(title) }}</h1><p>{{ t(subtitle) }}</p></div><div class="console-context"><div><CalendarDays :size="14" /> {{ today }}</div><span>♙ &nbsp; {{ t('Booked by:') }} &nbsp;<strong>{{ operatorName }}</strong><template v-if="operatorDetail"> · {{ operatorDetail }}</template></span></div></div>

    <div v-if="section === 'manage'" class="tabs booking-source-tabs" role="tablist" :aria-label="t('Booking source')">
      <button id="staff-bookings-tab" type="button" role="tab" aria-controls="booking-list-panel" :aria-selected="bookingTab === 'CONSOLE'" :class="{ active: bookingTab === 'CONSOLE' }" :disabled="isCreatingBooking" @click="bookingTab = 'CONSOLE'">{{ t('Guest bookings') }}</button>
      <button id="customer-bookings-tab" type="button" role="tab" aria-controls="booking-list-panel" :aria-selected="bookingTab === 'SELF_SERVICE'" :class="{ active: bookingTab === 'SELF_SERVICE' }" :disabled="isCreatingBooking" @click="bookingTab = 'SELF_SERVICE'">{{ t('Customer bookings') }}</button>
    </div>
    <div v-if="section === 'manage'" v-show="bookingTab === 'CONSOLE'">
      <ConsoleGuestBookingForm @created="handleGuestBookingCreated" @busy-change="isCreatingBooking = $event" />
    </div>

    <template v-if="section === 'tracking' || section === 'manage'">
      <section id="booking-list-panel" class="panel" :role="section === 'manage' ? 'tabpanel' : undefined" :aria-labelledby="section === 'manage' ? (bookingTab === 'CONSOLE' ? 'staff-bookings-tab' : 'customer-bookings-tab') : undefined">
        <div class="panel-heading">
          <h2>{{ t(section === 'tracking' ? 'Trip Status Tracking' : bookingTab === 'CONSOLE' ? 'Recent guest bookings' : 'Customer bookings') }}</h2>
          <small>{{ t(section === 'tracking' ? (isDemoMode ? 'Session updates · Backend live feed not connected' : 'Live trip list from LMS') : 'Property policy checked before every change') }}</small>
        </div>
        <div class="console-filter-row booking-list-filters">
          <input v-model="bookingSearch" :aria-label="t('Search booking ID, guest, room or destination')" :placeholder="t('Search booking ID, guest, room or destination')" />
          <select v-model="statusFilter" :aria-label="t('Status')">
            <option value="All">{{ t('All') }}</option><option v-if="section === 'tracking'" value="Active">{{ t('Active') }}</option>
            <option v-for="status in statuses" :key="status" :value="status">{{ t(bookingStatusLabel({ status })) }}</option>
          </select>
          <select v-if="section === 'manage'" v-model="propertyFilter" :aria-label="t('Property')">
            <option value="">{{ t('All properties') }}</option><option v-for="property in properties" :key="property.propertyId" :value="property.name">{{ property.name }}</option>
          </select>
        </div>
        <div v-if="section === 'tracking'" class="status-counter-row"><button v-for="status in statuses" :key="status" @click="statusFilter = status">{{ t(bookingStatusLabel({ status })) }} <b>{{ statusCount(status) }}</b></button></div>
        <p v-if="tripLoadError" class="form-error booking-list-message" role="alert">{{ tripLoadError }} <button class="text-button" @click="refreshTrips">{{ t('Retry') }}</button></p>
        <p v-if="isLoadingTrips" class="muted booking-list-message" role="status">{{ t('Loading') }}</p>
        <div class="table-scroll" :aria-busy="isLoadingTrips">
          <table class="data-table console-table" :aria-label="t(title)">
            <thead><tr><th scope="col">{{ t('BOOKING / GUEST') }}</th><th scope="col">{{ t('ROUTE / TIME') }}</th><th scope="col">{{ t('PROPERTY / VEHICLE') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col">{{ t('ACTIONS') }}</th></tr></thead>
            <tbody>
              <tr v-for="booking in filteredBookings" :key="booking.bookingId">
                <td><strong>{{ booking.guestName }}</strong><small>{{ bookingLabel(booking) }} · {{ t('Room') }} {{ booking.roomNumber || '—' }}</small></td>
                <td>{{ booking.pickupLocation }}<small>{{ displayDate(booking.pickupAt) }} {{ displayTime(booking.pickupAt) }} · {{ booking.dropoffLocation }}</small></td>
                <td>{{ booking.propertyName }}<small>{{ t(booking.vehicleClass) }}</small></td>
                <td><span class="status-tag" :class="booking.status.toLowerCase().replaceAll(' ', '-')">{{ t(bookingStatusLabel(booking)) }}</span></td>
                <td class="table-actions"><button @click="openBooking(booking, 'details')">{{ t(section === 'tracking' ? 'Track' : 'Details') }}</button><template v-if="section === 'manage' && canManageBookings"><button :disabled="['Completed', 'Cancelled'].includes(booking.status)" @click="openBooking(booking, 'modify')">{{ t('Modify') }}</button><button :disabled="['Completed', 'Cancelled'].includes(booking.status)" @click="openBooking(booking, 'cancel')">{{ t('Cancel') }}</button></template></td>
              </tr>
              <tr v-if="!isLoadingTrips && !tripLoadError && filteredBookings.length === 0"><td colspan="5" class="empty-cell">{{ t('No matching bookings') }}</td></tr>
            </tbody>
          </table>
        </div>
        <div class="panel-note">{{ isDemoMode ? filteredBookings.length : tripTotalItems }} {{ t('bookings · All times in Macau (GMT+8)') }}</div>
        <DataPager v-if="!isDemoMode && !isLoadingTrips && !tripLoadError" v-model:page="tripPage" v-model:page-size="tripPageSize" :total-items="tripTotalItems" />
      </section>
    </template>

    <template v-else-if="section === 'history'"><section class="panel"><div class="panel-heading"><h2>{{ t('Booking History') }} <span class="count-badge">{{ isDemoMode ? filteredBookings.length : tripTotalItems }}</span></h2><small>{{ t('Guest & staff linked records') }}</small></div><div class="history-filters"><label class="field-label">{{ t('GUEST / ROOM / BOOKING ID') }}<input v-model="bookingSearch" :placeholder="t('Search history')" /></label><label class="field-label">{{ t('STATUS') }}<select v-model="statusFilter"><option value="All">{{ t('All') }}</option><option v-for="status in statuses" :key="status" :value="status">{{ t(bookingStatusLabel({ status })) }}</option></select></label><label class="field-label">{{ t('PROPERTY') }}<select v-model="propertyFilter"><option value="">{{ t('All properties') }}</option><option v-if="isDemoMode">Nuwa</option><option v-if="isDemoMode">Morpheus</option><option v-for="property in properties" :key="property.propertyId" :value="property.name">{{ property.name }}</option></select></label><label class="field-label">{{ t('From') }}<input v-model="historyFrom" type="date" /></label><label class="field-label">{{ t('To') }}<input v-model="historyTo" type="date" /></label><button class="secondary-button" @click="bookingSearch = ''; statusFilter = 'All'; propertyFilter = ''; historyFrom = ''; historyTo = ''">{{ t('RESET') }}</button></div><div class="table-scroll"><table class="data-table console-table" :aria-label="t(title)"><thead><tr><th scope="col">{{ t('DATE / ID') }}</th><th scope="col">{{ t('GUEST') }}</th><th scope="col">{{ t('ROUTE / VEHICLE') }}</th><th scope="col">{{ t('PROPERTY') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col">{{ t('REDEMPTION') }}</th><th scope="col">{{ t('BOOKED BY') }}</th><th scope="col"></th></tr></thead><tbody><tr v-for="booking in filteredBookings" :key="booking.bookingId"><td>{{ displayDate(booking.pickupAt) }} · {{ displayTime(booking.pickupAt) }}<small>{{ bookingLabel(booking) }}</small></td><td>{{ booking.guestName }}<small>{{ t('Room') }} {{ booking.roomNumber }}</small></td><td>{{ booking.pickupLocation }}<small>{{ t(booking.vehicleClass) }}</small></td><td>{{ booking.propertyName }}</td><td><span class="status-tag" :class="booking.status.toLowerCase().replaceAll(' ', '-')">{{ t(bookingStatusLabel(booking)) }}</span></td><td>{{ t(booking.status === 'Completed' ? 'Redeemed' : 'Points Reserved') }}<small>{{ booking.points.toLocaleString() }} {{ t('pts') }}</small></td><td>{{ booking.createdByStaffName ?? booking.createdByStaffId ?? demoStaffId }}<small>{{ t(bookingSourceLabel(booking)) }}</small></td><td class="table-actions"><button @click="openBooking(booking, 'details')">{{ t('VIEW →') }}</button></td></tr></tbody></table></div><div class="panel-note">{{ t('Showing') }} {{ filteredBookings.length }} {{ t('of') }} {{ isDemoMode ? store.bookings.length : tripTotalItems }} {{ t('records · Macau (GMT+8)') }}</div><DataPager v-if="!isDemoMode" v-model:page="tripPage" v-model:page-size="tripPageSize" :total-items="tripTotalItems" /></section>    </template>

    <template v-else-if="section === 'approvals'"><section class="panel"><div class="panel-heading"><h2>{{ t('Approval Requests') }}</h2><button class="primary-button" @click="approvalDialog = true">{{ t('NEW REQUEST') }}</button></div><div class="tabs console-tabs"><button v-for="tab in ['All', 'Pending Approval', 'Approved', 'Rejected']" :key="tab" :class="{ active: approvalTab === tab }" @click="approvalTab = tab">{{ t(tab) }}</button></div><div class="table-scroll"><table class="data-table console-table" :aria-label="t(title)"><thead><tr><th scope="col">{{ t('REQUEST / TIME') }}</th><th scope="col">{{ t('GUEST / TIER') }}</th><th scope="col">{{ t('QUALIFICATION') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col">{{ t('REQUESTED BY') }}</th><th scope="col"></th></tr></thead><tbody><tr v-for="approval in filteredApprovals" :key="approval.approvalId"><td>{{ approval.approvalId }}<small>{{ displayDate(approval.requestedAt) }}, {{ displayTime(approval.requestedAt) }}</small></td><td>{{ approval.guestName }}<small>{{ t('Room') }} {{ approval.roomNumber }} · {{ t(approval.tier) }}</small></td><td>{{ t(approval.reason) }}</td><td><span class="status-tag pending">{{ t(approval.status) }}</span></td><td>{{ approval.requestedByStaffId ?? demoStaffId }}<small>{{ t('Front Desk') }}</small></td><td class="table-actions"><button @click="selectedApproval = approval">{{ t('VIEW →') }}</button></td></tr><tr v-if="filteredApprovals.length === 0"><td colspan="6" class="empty-cell">{{ t('No approval requests') }}</td></tr></tbody></table></div><div class="panel-note">{{ filteredApprovals.length }} {{ t('requests · Decisions generate console notifications') }}</div></section></template>

    <ConsoleDispatchPage v-else-if="section === 'dispatch'" />

    <ConsoleCouponsPage v-else-if="section === 'coupons'" />

    <ConsoleVehiclesPage v-else-if="section === 'vehicles'" />

    <ConsoleDriversPage v-else-if="section === 'drivers'" />

    <ConsoleRosterPage v-else-if="section === 'roster'" />
    <ConsolePropertiesPage v-else-if="section === 'properties'" />

    <ConsoleMembersPage v-else-if="section === 'members'" />

    <ConsoleReportsPage v-else-if="section === 'reports'" />

    <template v-else-if="section === 'notifications'"><section class="panel"><div class="panel-heading"><h2>{{ t('Notification Center') }}</h2><div><span class="count-badge">{{ unreadCount }} {{ t('unread') }}</span><button class="text-button" @click="markAllRead()">{{ t('Mark all as read') }}</button></div></div><div class="tabs console-tabs"><button v-for="tab in ['All', 'Unread', 'Booking confirmed', 'Driver assigned', 'Driver arrived', 'Trip completed', 'Approval required', 'System alerts']" :key="tab" :class="{ active: notificationTab === tab }" @click="notificationTab = tab">{{ t(tab) }}</button></div><div class="notice-list"><article v-for="notice in filteredNotices" :key="notice.noticeId" class="notice-item" :class="{ unread: !notice.isRead }"><span class="notice-icon"><Bell :size="18" /></span><div><small>{{ t(notice.category) }}</small><h3>{{ t(notice.title) }}</h3><p>{{ t(notice.message) }}</p><span class="muted">{{ displayDate(notice.createdAt) }} · {{ displayTime(notice.createdAt) }}</span></div><button v-if="!notice.isRead" class="text-button" @click="markRead(notice.noticeId)">{{ t('Mark read') }}</button></article><p v-if="filteredNotices.length === 0" class="empty-cell">{{ t('No notifications') }}</p></div></section><section class="panel preference-panel"><div class="panel-heading"><h2>{{ t('Notification Preferences') }}</h2></div><p>{{ t('Choose which events appear in your notification center.') }}</p><div class="preference-grid"><label v-for="category in notificationCategories" :key="category"><input type="checkbox" :checked="enabledNotificationCategories.includes(category)" @change="toggleNotificationCategory(category)" /> {{ t(category) }}</label></div><small class="muted">{{ t(isDemoMode ? 'These preferences filter local demo notices only. Real-time delivery requires an event service.' : 'Notification preferences are saved to your LMS account.') }}</small></section></template>

    <ConsoleTripTrackDialog v-if="section === 'tracking' && bookingDialog === 'details' && selectedBooking" :booking="selectedBooking" :events="tripEvents" @close="bookingDialog = null" @refresh="applyTrackedBooking" />
    <div v-if="bookingDialog && selectedBooking && !(section === 'tracking' && bookingDialog === 'details')" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Booking details')"><header class="dialog-header"><h2>{{ t(bookingDialog === 'modify' ? 'Modify Booking' : bookingDialog === 'cancel' ? 'Cancel Booking' : 'Booking Details') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="bookingDialog = null"><X :size="18" /></button></header><div class="dialog-body"><p><strong>{{ bookingLabel(selectedBooking) }}</strong> · {{ selectedBooking.guestName }}</p><p>{{ selectedBooking.pickupLocation }} → {{ selectedBooking.dropoffLocation }}</p><p>{{ t('Status:') }} {{ t(bookingStatusLabel(selectedBooking)) }}</p><p>{{ t('Service type') }}: {{ t(selectedBooking.serviceType ?? 'Property transfer') }} · {{ t('Booked by') }}: {{ selectedBooking.createdByStaffName ?? selectedBooking.createdByStaffId ?? demoStaffId }}</p><p v-if="selectedBooking.cancellationReason">{{ t('Cancellation reason') }}: {{ t(selectedBooking.cancellationReason) }}</p><template v-if="bookingDialog === 'modify'"><label class="field-label">{{ t('Pickup location') }}<select v-model="editingPickupLocation"><option v-for="location in store.locations" :key="location.locationId" :value="location.name">{{ location.name }}</option></select></label><label class="field-label">{{ t('Pickup time') }}<input v-model="editingPickupLocal" type="datetime-local" /></label></template><template v-if="bookingDialog === 'cancel'"><label class="field-label">{{ t('Cancellation reason') }}<select v-model="cancellationReason"><option value="">{{ t('Select a reason') }}</option><option v-for="reason in (isDemoMode ? ['Customer no-show', 'Flight delayed', 'Changed plans', 'Driver unavailable', 'Vehicle breakdown'].map((name) => ({ reasonCode: name, name })) : cancellationReasons)" :key="reason.reasonCode" :value="reason.reasonCode">{{ t(reason.name) }}</option></select></label><p v-if="cancellationPolicy" class="muted">{{ t('Cancellation policy') }}: {{ cancellationPolicy }}</p><p class="form-error">{{ t(isDemoMode ? 'This changes only local demo state; property cancellation policies require the backend.' : 'The cancellation policy is enforced by LMS.') }}</p></template><ul v-if="bookingDialog === 'details' && tripEvents.length" class="notice-list"><li v-for="event in tripEvents" :key="event.eventId"><strong>{{ t(event.type) }}</strong> · {{ displayDate(event.occurredAt) }} {{ displayTime(event.occurredAt) }}<small v-if="event.actorName">{{ event.actorName }}</small></li></ul><p v-else-if="bookingDialog === 'details'" class="muted">{{ t(isDemoMode ? 'Live tracking is not connected.' : 'No timeline events recorded.') }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="bookingDialog = null">{{ t('CLOSE') }}</button><button v-if="bookingDialog === 'modify'" class="primary-button" @click="saveBooking">{{ t('SAVE CHANGES') }}</button><button v-if="bookingDialog === 'cancel'" class="danger-button" :disabled="!cancellationReason" @click="cancelBooking">{{ t('CANCEL BOOKING') }}</button></footer></section></div>
    <div v-if="selectedApproval" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Approval request details')"><header class="dialog-header"><h2>{{ t('Approval request details') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="selectedApproval = null"><X :size="18" /></button></header><div class="dialog-body"><p><strong>{{ selectedApproval.approvalId }}</strong> · {{ selectedApproval.guestName }}</p><p>{{ t('Status') }}: {{ t(selectedApproval.status) }}</p><p>{{ t('Justification') }}: {{ t(selectedApproval.reason) }}</p><p>{{ t('Requested by') }}: {{ selectedApproval.requestedByStaffId ?? demoStaffId }}</p><p class="muted">{{ t('Manager decisions and live notifications require the production backend.') }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="selectedApproval = null">{{ t('CLOSE') }}</button></footer></section></div>
    <div v-if="approvalDialog" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('New approval request')"><header class="dialog-header"><h2>{{ t('New Approval Request') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="approvalDialog = false"><X :size="18" /></button></header><div class="dialog-body"><label class="field-label">{{ t('Guest') }}<select v-model="approvalGuestId"><option value="">{{ t('Select a guest') }}</option><option v-for="guest in store.guests" :key="guest.guestId" :value="guest.guestId">{{ guest.fullName }} · {{ t('Room') }} {{ guest.roomNumber }}</option></select></label><label class="field-label">{{ t('Justification') }}<textarea v-model="approvalReason" rows="4" :placeholder="t('Explain the entitlement exception')" /></label><p v-if="approvalError" class="form-error">{{ t(approvalError) }}</p><p class="muted">{{ t(isDemoMode ? 'Request is saved in local demo state. Production decisions require manager authorization.' : 'The qualification request is submitted to LMS for manager review.') }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="approvalDialog = false">{{ t('CANCEL') }}</button><button class="primary-button" @click="submitApproval">{{ t('SUBMIT REQUEST') }}</button></footer></section></div>
  </ShellLayout>
</template>

<style scoped>
.booking-source-tabs{gap:64px;padding:0;margin-bottom:14px}.booking-source-tabs button{height:44px;font-size:14px}.booking-list-message{padding:0 14px}.booking-list-filters{flex-wrap:wrap}.booking-list-filters input{flex:1;min-width:200px}@media(max-width:600px){.booking-list-filters select{flex:1;min-width:120px}.booking-source-tabs{gap:32px}}
</style>
