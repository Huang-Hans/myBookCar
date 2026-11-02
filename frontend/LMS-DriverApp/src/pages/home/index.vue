<script setup lang="ts">
import { computed, ref } from 'vue'
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AmapRouteMap from '@/components/AmapRouteMap.vue'
import { activeTripStages, canReceiveRequest, createInspectionAnswers, inspectionItemsFor, isInspectionComplete, nextTripStage, progressActionLabel, stageTimesFor, tripStageIndex } from '@/data/driverState'
import type { DriverOrder } from '@/data/driverOrders'
import { mapDriverTripDetail, mapDriverTripSummary, type DriverHistoryCard } from '@/data/driverTripHistory'
import { ApiRequestError, clearDriverSession, logoutDriver } from '@/api/http'
import { onDriverNativeEvent, type DriverNativeEvent } from '@/api/nativeBridge'
import { recordNativeTripLocationStop, stopNativeTripLocation, synchronizeNativeTripLocation } from '@/api/nativeTripLocation'
import { canReportTripLocation, driverLocationReporter } from '@/api/driverLocations'
import { codeToAvailability, driverApi, type DriverOffer, type DriverPreferences, type DriverSelf, type DriverTripDetail, type InspectionTemplate, type OperationsContacts, type ProgressAction } from '@/api/driver'
import { fetchUpcomingDriverShifts, macauBusinessDate, type DriverShiftDay } from '@/api/driverShifts'
import { onDriverNotificationsConnected, startDriverNotificationHub, stopDriverNotificationHub, subscribeDriverNotifications } from '@/api/notifications'
import type { NotificationMessageV1 } from '@/api/notificationContract'
import type { AmapRouteMetrics } from '@/data/amap'
import { localeTag, setLocale, t } from '@/i18n/locale'
import { mapConfig } from '../../../../map.config'

type Tab = 'home' | 'history' | 'inspection' | 'settings'
type Availability = 'Available' | 'On Break' | 'Off Duty'
type Answer = 'Pass' | 'Fail' | null
type TripStatus = 'none' | 'active' | 'completed'

const tab = ref<Tab>('home')
let isHomeLoaded = false
let isPageVisible = false
const activeView = ref<'home' | 'map' | 'details' | 'order-details'>('home')
const selectedHistoryOrder = ref<DriverOrder | null>(null)
const driverSelf = ref<DriverSelf | null>(null)
const currentOffer = ref<DriverOffer | null>(null)
const activeTrip = ref<DriverTripDetail | null>(null)
const amapWebKey = mapConfig.webKey
const amapSecurityJsCode = mapConfig.securityJsCode
const remoteHistory = ref<DriverHistoryCard[]>([])
const isHistoryLoading = ref(false)
const isHistoryDetailLoading = ref(false)
const hasHistoryLoaded = ref(false)
const hasHistoryError = ref(false)
const historyPage = ref(0)
const historyTotalPages = ref(0)
let historyRequestVersion = 0
const inspectionTemplates = ref<InspectionTemplate[]>([])
const driverPreferences = ref<DriverPreferences | null>(null)
const operationsContacts = ref<OperationsContacts | null>(null)
const isSosOpen = ref(false)
const isRequestOpen = ref(false)
const isVehicleIssueOpen = ref(false)
const offerRemainingSeconds = ref(0)
let offerServerOffsetMs = 0
const slideProgress = ref(0)
let dragStartX: number | null = null
const isLanguageOpen = ref(false)
const availability = ref<Availability | 'On Trip' | null>(null)
const shiftDays = ref<DriverShiftDay[]>([])
const isShiftLoading = ref(false)
const shiftError = ref('')
let shiftRequestVersion = 0
const tripStatus = ref<TripStatus>(readStored('lmsDriverTripStatus', 'none') as TripStatus)
const activeStageIndex = ref(Number(readStored('lmsDriverActiveStage', '-1')))
const activeSlideProgress = ref(0)
const mapMetrics = ref<AmapRouteMetrics>({ coveredKm: 0, distanceToPickupKm: 0, speedKmh: 0, etaMinutes: 0, hasCurrentPosition: false })
const activeStageTimes = computed(() => stageTimesFor(activeTrip.value?.events ?? []))
function formatStageTime(value: string): string {
  return new Date(value).toLocaleString(localeTag(), { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}
let activeDragStartX: number | null = null
const activeSlideThreshold = 75
const hasIncomingRequest = ref(readStored('lmsDriverIncomingRequest', 'true') === 'true')
const historyFilter = ref<'All Trips' | 'Completed' | 'Cancelled'>('All Trips')
const historyFilterIndex = computed(() => (['All Trips', 'Completed', 'Cancelled'] as const).indexOf(historyFilter.value))
const inspectionType = ref<'Pre-Trip' | 'Post-Trip'>('Pre-Trip')
const currentInspectionTemplate = computed(() => inspectionTemplates.value.find((item) => item.phase === (inspectionType.value === 'Pre-Trip' ? 'PRE_TRIP' : 'POST_TRIP')))
const inspectionItems = computed(() => currentInspectionTemplate.value?.items.map((item) => item.name) ?? inspectionItemsFor(inspectionType.value))
const answers = ref<Record<string, Answer>>(createInspectionAnswers())
const language = ref<'English' | '繁體中文' | '简体中文'>(readStored('lmsDriverLanguage', 'English') as 'English' | '繁體中文' | '简体中文')
const incidentType = ref('')
const incidentDescription = ref('')
const vehicleIssueDescription = ref('')
const incidentTypes = ['Vehicle Breakdown', 'Traffic Accident', 'Passenger Assistance', 'Other Incident']
const title = computed(() => ({ home: '', history: 'Trip History', inspection: 'Vehicle Inspection', settings: 'Settings' })[tab.value])
const activeProgressLabel = computed(() => {
  const actionLabel = progressActionLabel(activeTrip.value?.allowedActions[0])
  if (actionLabel) return actionLabel
  return activeTripStages[Math.max(0, activeStageIndex.value)] ?? 'Update Trip Status'
})
const passengerInitials = computed(() => {
  const parts = activeTrip.value?.passenger.name.trim().split(/\s+/).filter(Boolean) ?? []
  return parts.slice(0, 2).map((part) => part.charAt(0).toUpperCase()).join('') || '—'
})
const visibleHistory = computed(() => remoteHistory.value)

const unsubscribeNotifications = subscribeDriverNotifications(handleDriverNotification)
const unsubscribeReconnect = onDriverNotificationsConnected(recoverActiveTab)
const unsubscribeNativeEvents = onDriverNativeEvent(handleNativeDriverEvent)
onShow(() => {
  isPageVisible = true
  void startDriverNotificationHub()
  synchronizeLocationReporter()
  // Refresh the active trip when returning to the foreground so reporting stops for trips completed elsewhere.
  if (!isHomeLoaded) { isHomeLoaded = true; void loadHomeTab() }
  else if (tab.value === 'home') void loadHomeTab()
  else if (tab.value === 'history' && activeView.value === 'home') void loadHistory()
})
onHide(() => { isPageVisible = false; driverLocationReporter.pause() })
const offerCountdownTimer = setInterval(updateOfferCountdown, 1_000)
onUnload(() => { driverLocationReporter.endTrip(); clearInterval(offerCountdownTimer); unsubscribeNotifications(); unsubscribeReconnect(); unsubscribeNativeEvents() })
function synchronizeLocationReporter(): void {
  const trip = activeTrip.value
  const sharingTripId = tripStatus.value === 'active' && trip && canReportTripLocation(trip) ? trip.tripId : null
  // In the APK shell a native foreground service reports, including in the background and with the screen off.
  if (synchronizeNativeTripLocation(sharingTripId)) return
  if (!sharingTripId) {
    driverLocationReporter.endTrip()
    return
  }
  if (isPageVisible) driverLocationReporter.start(sharingTripId)
  else driverLocationReporter.pause()
}
function handleNativeDriverEvent(event: DriverNativeEvent): void {
  if (event.type === 'LOCATION_PERMISSION_CHANGED') { synchronizeLocationReporter(); return }
  recordNativeTripLocationStop(event.tripId, event.lastSeq, event.reason === 'TRIP_ENDED')
  if (event.reason === 'SESSION_EXPIRED') { clearDriverSession(); uni.reLaunch({ url: '/pages/login/index' }); return }
  if (event.reason === 'TRIP_ENDED' && isPageVisible) void loadHomeTab()
}
async function handleDriverNotification(message: NotificationMessageV1): Promise<void> {
  if (!['DRIVER_OFFER_CREATED', 'DRIVER_OFFER_CLOSED', 'DRIVER_OFFER_ACCEPTED'].includes(message.data.eventType)) return
  if (message.data.notification.targetType !== 'DRIVER_OFFER') return
  if (message.data.eventType === 'DRIVER_OFFER_CLOSED' && currentOffer.value?.offerId === message.data.notification.targetId) closeRequest()
  // Orders and offers are driven by SignalR events; the offer payload itself comes from the documented HTTP query,
  // fetched only while the Home tab is visible.
  if (tab.value === 'home') await loadHomeTab()
  if (message.data.eventType === 'DRIVER_OFFER_ACCEPTED') uni.showToast({ title: t('Trip accepted'), icon: 'none' })
}
async function loadHomeTab(): Promise<void> {
  try {
    const [selfResponse, offerResponse, activeResponse] = await Promise.all([
      driverApi.getSelf(), driverApi.listOffers(), driverApi.listTrips('ACTIVE'),
    ])
    driverSelf.value = selfResponse.data
    availability.value = selfResponse.data.availability
      ? selfResponse.data.availability === 'ON_TRIP' ? 'On Trip' : codeToAvailability(selfResponse.data.availability)
      : null
    void loadDriverShifts()
    currentOffer.value = offerResponse.data.find((item) => item.status === 'PENDING') ?? null
    hasIncomingRequest.value = currentOffer.value !== null
    synchronizeOfferCountdown()
    const activeSummary = activeResponse.data[0]
    if (activeSummary) {
      activeTrip.value = (await driverApi.getTrip(activeSummary.tripId)).data
      tripStatus.value = 'active'
      activeStageIndex.value = tripStageIndex(activeTrip.value.stage)
    } else { activeTrip.value = null; tripStatus.value = 'none' }
    synchronizeLocationReporter()
  } catch (reason) { showApiError(reason, 'Unable to load driver data.') }
}
async function loadDriverShifts(): Promise<void> {
  const driverId = driverSelf.value?.driverId
  if (!driverId) return
  const requestVersion = ++shiftRequestVersion
  isShiftLoading.value = true
  shiftError.value = ''
  try {
    const days = await fetchUpcomingDriverShifts(driverId, macauBusinessDate(new Date()))
    if (requestVersion === shiftRequestVersion) shiftDays.value = days
  } catch (reason) {
    if (requestVersion !== shiftRequestVersion) return
    shiftDays.value = []
    const error = reason instanceof ApiRequestError ? reason : null
    shiftError.value = error ? `[${error.statusCode}] ${error.message} · Trace ID: ${error.traceId}` : t('Unable to load shifts.')
  } finally {
    if (requestVersion === shiftRequestVersion) isShiftLoading.value = false
  }
}
function formatShiftDate(businessDate: string): string {
  return new Intl.DateTimeFormat(localeTag(), { weekday: 'short', month: 'short', day: 'numeric', timeZone: 'Asia/Macau' }).format(new Date(`${businessDate}T12:00:00Z`))
}
function shiftStatusLabel(status: string | null | undefined): string {
  return status === 'ON_DUTY' ? 'On Duty' : status === 'REST' ? 'Rest Day' : status === 'LEAVE' ? 'On Leave' : 'Unknown Status'
}
function shiftPeriodLabel(period: string | null | undefined): string {
  return period === 'MORNING' ? 'Morning' : period === 'AFTERNOON' ? 'Afternoon' : period === 'NIGHT' ? 'Night' : 'Shift'
}
async function loadInspectionTab(): Promise<void> {
  try { inspectionTemplates.value = (await driverApi.getInspectionTemplates()).data }
  catch (reason) { showApiError(reason, 'Unable to load inspection templates.') }
}
async function loadSettingsTab(): Promise<void> {
  try {
    const data = (await driverApi.getPreferences()).data
    driverPreferences.value = data
    language.value = data.language === 'ZH_HANT' ? '繁體中文' : data.language === 'ZH_HANS' ? '简体中文' : 'English'
    setLocale(data.language)
  } catch (reason) { showApiError(reason, 'Unable to load preferences.') }
}
async function loadOperationsContacts(): Promise<void> {
  try { operationsContacts.value = (await driverApi.getOperationsContacts()).data }
  catch (reason) { showApiError(reason, 'Unable to load operations contacts.') }
}
// Hub reconnect recovery restores the data of the tab currently on screen (contract section 6).
function recoverActiveTab(): void {
  if (tab.value === 'home') void loadHomeTab()
  else if (tab.value === 'history') void loadHistory()
}
function synchronizeOfferCountdown(): void {
  if (!currentOffer.value) { offerRemainingSeconds.value = 0; return }
  const serverTime = Date.parse(currentOffer.value.serverTime)
  offerServerOffsetMs = Number.isFinite(serverTime) ? serverTime - Date.now() : 0
  updateOfferCountdown()
}
function updateOfferCountdown(): void {
  if (!currentOffer.value) { offerRemainingSeconds.value = 0; return }
  const expiresAt = Date.parse(currentOffer.value.expiresAt)
  const remaining = Number.isFinite(expiresAt) ? Math.max(0, Math.ceil((expiresAt - (Date.now() + offerServerOffsetMs)) / 1_000)) : 0
  const didExpire = offerRemainingSeconds.value > 0 && remaining === 0
  offerRemainingSeconds.value = remaining
  if (didExpire) { closeRequest(); void loadHomeTab() }
}
async function loadHistory(page = 1): Promise<void> {
  if (page > 1 && (isHistoryLoading.value || page > historyTotalPages.value)) return
  const requestVersion = page === 1 ? ++historyRequestVersion : historyRequestVersion
  if (page === 1) {
    remoteHistory.value = []
    historyPage.value = 0
    historyTotalPages.value = 0
    hasHistoryLoaded.value = false
    hasHistoryError.value = false
  }
  isHistoryLoading.value = true
  try {
    const status = historyFilter.value === 'Completed' ? 'COMPLETED' : historyFilter.value === 'Cancelled' ? 'CANCELLED' : undefined
    const response = await driverApi.listTrips('HISTORY', status, page)
    if (requestVersion !== historyRequestVersion) return
    const cards = response.data.map(mapDriverTripSummary)
    remoteHistory.value = page === 1 ? cards : [...remoteHistory.value, ...cards]
    historyPage.value = page
    historyTotalPages.value = response.meta?.pagination?.totalPages ?? page
    hasHistoryLoaded.value = true
    hasHistoryError.value = false
  } catch (reason) {
    if (requestVersion !== historyRequestVersion) return
    hasHistoryError.value = true
    hasHistoryLoaded.value = true
    showApiError(reason, 'Unable to load trip history.')
  } finally {
    if (requestVersion === historyRequestVersion) isHistoryLoading.value = false
  }
}
function selectHistoryFilter(value: typeof historyFilter.value): void {
  if (historyFilter.value === value) return
  historyFilter.value = value
  void loadHistory()
}
function loadMoreHistory(): void {
  if (tab.value === 'history' && activeView.value === 'home' && !isHistoryLoading.value && historyPage.value < historyTotalPages.value) void loadHistory(historyPage.value + 1)
}
function showApiError(reason: unknown, fallback: string): void {
  const error = reason instanceof ApiRequestError ? reason : null
  uni.showModal({ title: t('Request Failed'), content: error ? `${error.message}\nTrace ID: ${error.traceId}` : t(fallback), showCancel: false })
}

function readStored(key: string, fallback: string): string {
  try { return uni.getStorageSync(key) || fallback } catch { return fallback }
}
async function acceptTrip(): Promise<void> {
  if (!currentOffer.value) return
  try {
    const response = await driverApi.respondToOffer(currentOffer.value.offerId, 'ACCEPT')
    if (response.code === 20200 || response.data.status === 'ACCEPTING') {
      closeRequest()
      uni.showToast({ title: t('Acceptance is processing'), icon: 'none' })
      await loadHomeTab()
      return
    }
    if (response.code !== 20000 || response.data.status !== 'ACCEPTED') {
      uni.showToast({ title: t('Acceptance has not completed yet'), icon: 'none' })
      await loadHomeTab()
      return
    }
  } catch (reason) { showApiError(reason, 'Unable to accept request.'); return }
  tripStatus.value = 'active'
  uni.setStorageSync('lmsDriverTripStatus', 'active')
  hasIncomingRequest.value = false
  uni.setStorageSync('lmsDriverIncomingRequest', 'false')
  isRequestOpen.value = false
  slideProgress.value = 0
  activeStageIndex.value = -1
  uni.setStorageSync('lmsDriverActiveStage', '-1')
  await loadHomeTab()
}
async function declineTrip(): Promise<void> {
  if (!currentOffer.value) return
  try { await driverApi.respondToOffer(currentOffer.value.offerId, 'DECLINE') } catch (reason) { showApiError(reason, 'Unable to decline request.'); return }
  hasIncomingRequest.value = false
  uni.setStorageSync('lmsDriverIncomingRequest', 'false')
  isRequestOpen.value = false
  slideProgress.value = 0
  uni.showToast({ title: t('Request returned to dispatch'), icon: 'none' })
}
function completeTrip(): void {
  tripStatus.value = 'completed'
  driverLocationReporter.endTrip()
  uni.setStorageSync('lmsDriverTripStatus', 'completed')
  activeSlideProgress.value = 0
}
function advanceTrip(): void {
  if (activeTrip.value?.allowedActions[0]) { void progressActiveTrip(activeTrip.value.allowedActions[0]); return }
  const next = nextTripStage(activeStageIndex.value)
  if (next === null) completeTrip()
  else {
    activeStageIndex.value = next
    uni.setStorageSync('lmsDriverActiveStage', String(next))
    activeSlideProgress.value = 0
  }
}
async function progressActiveTrip(action: ProgressAction): Promise<void> {
  if (!activeTrip.value) return
  try {
    activeTrip.value = (await driverApi.progressTrip(activeTrip.value.tripId, action)).data
    activeStageIndex.value = tripStageIndex(activeTrip.value.stage)
    if (activeTrip.value.status === 'COMPLETED') tripStatus.value = 'completed'
    synchronizeLocationReporter()
    if (tripStatus.value === 'completed') await loadHistory()
    activeSlideProgress.value = 0
  } catch (reason) { showApiError(reason, 'Unable to update trip progress.') }
}
async function submitInspection(): Promise<void> {
  if (!isInspectionComplete(inspectionItems.value, answers.value)) {
    uni.showToast({ title: t('Check every item before submitting'), icon: 'none' })
    return
  }
  const template = currentInspectionTemplate.value
  if (!template) { uni.showToast({ title: t('Inspection template is unavailable.'), icon: 'none' }); return }
  try {
    await driverApi.submitInspection(template.phase, template.items.map((item) => ({ itemCode: item.code, result: answers.value[item.name] === 'Fail' ? 'FAIL' : 'PASS' })), template.version)
    uni.showToast({ title: t('Inspection submitted'), icon: 'none' })
  } catch (reason) { showApiError(reason, 'Unable to submit inspection.') }
}
async function reportIncident(): Promise<void> {
  if (!incidentType.value || !incidentDescription.value.trim()) {
    uni.showToast({ title: t('Select a type and enter details'), icon: 'none' })
    return
  }
  const typeMap = { 'Vehicle Breakdown': 'VEHICLE_BREAKDOWN', 'Traffic Accident': 'TRAFFIC_ACCIDENT', 'Passenger Assistance': 'PASSENGER_ASSISTANCE', 'Other Incident': 'OTHER' } as const
  try {
    await driverApi.reportIncident(typeMap[incidentType.value as keyof typeof typeMap] ?? 'OTHER', new Date().toISOString(), incidentDescription.value.trim(), activeTrip.value?.tripId)
  } catch (reason) { showApiError(reason, 'Unable to submit report.'); return }
  incidentType.value = ''
  incidentDescription.value = ''
  uni.showToast({ title: t('Incident reported'), icon: 'none' })
}
async function submitVehicleIssue(): Promise<void> {
  const description = vehicleIssueDescription.value.trim()
  if (!description) {
    uni.showToast({ title: t('Describe the issue before saving'), icon: 'none' })
    return
  }
  try {
    await driverApi.reportVehicleIssue(description)
  } catch (reason) { showApiError(reason, 'Unable to save vehicle issue.'); return }
  vehicleIssueDescription.value = ''
  isVehicleIssueOpen.value = false
  uni.showToast({ title: t('Vehicle issue reported'), icon: 'none' })
}
async function changeLanguage(value: 'English' | '繁體中文' | '简体中文'): Promise<void> {
  const code = value === '繁體中文' ? 'ZH_HANT' : value === '简体中文' ? 'ZH_HANS' : 'EN'
  try { driverPreferences.value = (await driverApi.updatePreferences(code, driverPreferences.value?.version)).data; language.value = value; setLocale(code); isLanguageOpen.value = false }
  catch (reason) { showApiError(reason, t('Unable to save language.')) }
}
function openSos(): void {
  isSosOpen.value = true
  isRequestOpen.value = false
  isVehicleIssueOpen.value = false
  if (!operationsContacts.value) void loadOperationsContacts()
}
function openVehicleIssue(): void { isVehicleIssueOpen.value = true; isRequestOpen.value = false }
function closeVehicleIssue(): void { isVehicleIssueOpen.value = false; vehicleIssueDescription.value = '' }
function openRequest(): void { slideProgress.value = 0; isRequestOpen.value = true }
function closeRequest(): void { isRequestOpen.value = false; slideProgress.value = 0; dragStartX = null }
function pointerX(event: { touches?: ArrayLike<{ clientX: number }>; clientX?: number }): number | null {
  return event.touches?.[0]?.clientX ?? event.clientX ?? null
}
function startSlide(event: { touches?: ArrayLike<{ clientX: number }>; clientX?: number }): void { dragStartX = pointerX(event) }
function moveSlide(event: { touches?: ArrayLike<{ clientX: number }>; clientX?: number }): void {
  const x = pointerX(event)
  if (dragStartX === null || x === null) return
  const travel = Math.max(180, uni.getSystemInfoSync().windowWidth - 88)
  slideProgress.value = Math.max(0, Math.min(100, (x - dragStartX) / travel * 100))
}
function finishSlide(): void {
  if (dragStartX === null) return
  dragStartX = null
  if (slideProgress.value >= 90) acceptTrip()
  else slideProgress.value = 0
}
function startActiveSlide(event: { touches?: ArrayLike<{ clientX: number }>; clientX?: number }): void {
  activeDragStartX = pointerX(event)
}
function startActivePointerSlide(event: PointerEvent): void {
  activeDragStartX = event.clientX
  const target = event.currentTarget
  if (target instanceof Element && target.hasPointerCapture && !target.hasPointerCapture(event.pointerId)) target.setPointerCapture(event.pointerId)
}
function moveActivePointerSlide(event: PointerEvent): void { moveActiveSlide(event) }
function finishActivePointerSlide(event: PointerEvent): void {
  const target = event.currentTarget
  if (target instanceof Element && target.hasPointerCapture?.(event.pointerId)) target.releasePointerCapture(event.pointerId)
  finishActiveSlide()
}
function moveActiveSlide(event: { touches?: ArrayLike<{ clientX: number }>; clientX?: number }): void {
  const x = pointerX(event)
  if (activeDragStartX === null || x === null) return
  const travel = Math.max(180, uni.getSystemInfoSync().windowWidth - 120)
  activeSlideProgress.value = Math.max(0, Math.min(100, (x - activeDragStartX) / travel * 100))
}
function finishActiveSlide(): void {
  if (activeDragStartX === null) return
  activeDragStartX = null
  if (activeSlideProgress.value >= activeSlideThreshold) {
    activeSlideProgress.value = 100
    advanceTrip()
  }
  else activeSlideProgress.value = 0
}
function switchTab(value: Tab): void {
  tab.value = value
  isSosOpen.value = false
  activeView.value = 'home'
  selectedHistoryOrder.value = null
  // Each bottom-navigation tap loads its own page data instead of prefetching everything at once.
  if (value === 'home') void loadHomeTab()
  else if (value === 'history') void loadHistory()
  else if (value === 'inspection') void loadInspectionTab()
  else if (value === 'settings') void loadSettingsTab()
}
function openActiveView(value: 'map' | 'details'): void { activeView.value = value }
async function openOrderDetails(order: DriverHistoryCard): Promise<void> {
  if (isHistoryDetailLoading.value) return
  isHistoryDetailLoading.value = true
  try {
    const response = await driverApi.getTrip(order.id)
    if (tab.value !== 'history' || activeView.value !== 'home') return
    selectedHistoryOrder.value = mapDriverTripDetail(response.data)
    activeView.value = 'order-details'
  } catch (reason) { showApiError(reason, 'Unable to load trip details.') }
  finally { isHistoryDetailLoading.value = false }
}
function closeDetailView(): void { activeView.value = 'home'; selectedHistoryOrder.value = null }
function tabIcon(value: Tab): string { return `/static/icons/tab-${value}-${tab.value === value && !isSosOpen.value ? 'active' : 'inactive'}.svg` }
function contactOperations(): void {
  const contacts = operationsContacts.value?.contacts ?? []
  uni.showModal({ title: 'Contact Operations', content: contacts.length ? contacts.map((item) => `${item.name}: ${item.value}`).join('\n') : t('Operations contact information is unavailable. Use local emergency services in an emergency.'), showCancel: false })
}
function contactPassenger(method: 'Call' | 'Message'): void {
  uni.showModal({ title: `${method} Passenger`, content: t('Demo contact number only. Live passenger contact is unavailable.'), showCancel: false })
}
async function signOut(): Promise<void> { driverLocationReporter.endTrip(); stopNativeTripLocation(); try { await stopDriverNotificationHub(); await logoutDriver() } finally { uni.reLaunch({ url: '/pages/login/index' }) } }
</script>

<template>
  <view class="driver-page">
    <view class="top-bar">
      <template v-if="isSosOpen"><button class="back-button" @tap="isSosOpen = false">‹ <text>{{ $t("Back") }}</text></button><text class="top-title sos-top-title">{{ $t("SOS & Incident") }}</text></template>
      <template v-else-if="activeView !== 'home'"><button class="back-button" @tap="closeDetailView">‹ <text>{{ $t("Back") }}</text></button><text class="top-title sos-top-title">{{ $t(activeView === 'order-details' ? 'Order Details' : activeView === 'map' ? 'Active Trip' : 'Trip Details') }}</text></template>
      <image v-else-if="tab === 'home'" class="brand" src="/static/figma/melco-logo.png" mode="aspectFit" />
      <text v-else class="top-title">{{ $t(title) }}</text>
      <button class="sos-button" @tap="openSos"><image src="/static/icons/sos.svg" mode="aspectFit" />SOS</button>
    </view>

    <scroll-view scroll-y class="content" :class="{ 'map-content-scroll': activeView === 'map', 'order-content-scroll': activeView === 'order-details' }" @scrolltolower="loadMoreHistory">
      <template v-if="isSosOpen">
        <view class="sos-content">
          <view class="sos-card help-card"><image src="/static/icons/sos.svg" mode="aspectFit" /><text class="card-title">{{ $t("We’re Here to Help.") }}</text><text class="subcopy">{{ $t("Contact the Melco Operations Center for Immediate Assistance.") }}</text><button class="contact-button" @tap="contactOperations"><image src="/static/icons/call.svg" mode="aspectFit" />{{ $t("Contact Operations") }}</button><text class="muted centered">{{ $t("Demo Only · Use Local Emergency Services in an Emergency.") }}</text></view>
          <view class="sos-card"><text class="card-title">{{ $t("Log an Incident") }}</text><text class="subcopy">{{ $t("Record Details for the Operations Team.") }}</text><text class="field-title">{{ $t("Incident Type") }}</text><picker :range="incidentTypes.map(t)" @change="incidentType = incidentTypes[Number($event.detail.value)] || ''"><view class="picker-field">{{ incidentType ? $t(incidentType) : $t('Select Incident Type') }} <text>›</text></view></picker><text class="field-title">{{ $t("Time") }}</text><view class="picker-field">{{ new Date().toLocaleString(localeTag()) }}</view><text class="field-title">{{ $t("Description") }}</text><textarea v-model="incidentDescription" class="incident-input" :placeholder='$t("Describe what happened")' /><button class="gold-button" @tap="reportIncident">{{ $t("Save Incident Report") }}</button></view>
        </view>
      </template>
      <template v-else-if="activeView === 'order-details' && selectedHistoryOrder">
        <view class="order-details-page">
          <view class="order-summary-heading"><text>{{ selectedHistoryOrder.service }}</text><text class="gold-text">{{ $t(selectedHistoryOrder.status) }}</text></view>
          <text class="order-reference">{{ $t('Order No.') }} {{ selectedHistoryOrder.displayId }} · {{ selectedHistoryOrder.date }}</text>
          <view class="order-route">
            <view class="order-route-stop"><text class="order-field-label">{{ $t("Pickup") }}</text><text class="order-place">{{ selectedHistoryOrder.pickup }}</text><text v-if="selectedHistoryOrder.pickupAddress" class="order-address">{{ selectedHistoryOrder.pickupAddress }}</text></view>
            <view class="order-route-stop"><text class="order-field-label">{{ $t("Drop-Off") }}</text><text class="order-place">{{ selectedHistoryOrder.dropoff }}</text><text v-if="selectedHistoryOrder.dropoffAddress" class="order-address">{{ selectedHistoryOrder.dropoffAddress }}</text></view>
          </view>
          <view class="order-times"><view><text>{{ $t("Accepted Time") }}</text><text>{{ selectedHistoryOrder.accepted }}</text></view><view><text>{{ $t("Pickup Time") }}</text><text>{{ selectedHistoryOrder.started }}</text></view><view><text>{{ $t("Drop-Off Time") }}</text><text>{{ selectedHistoryOrder.ended }}</text></view></view>
          <view class="order-customer"><text class="order-section-title">{{ $t("Customer Details") }}</text><view class="order-customer-identity"><text class="order-avatar">{{ selectedHistoryOrder.customerInitials }}</text><view><text>{{ selectedHistoryOrder.customerName }}</text><text>{{ selectedHistoryOrder.customerPhone }}</text></view></view><view class="order-passengers"><text>{{ $t("Passengers") }}</text><text>{{ selectedHistoryOrder.passengers }}</text></view><text class="order-demo-note">{{ $t("Contact Number Masked") }}</text></view>
          <view class="order-notes"><text class="order-section-title">{{ $t("Journey Notes") }}</text><view><text>{{ $t("Special Instructions") }}</text><text>{{ selectedHistoryOrder.specialInstructions }}</text></view><view><text>{{ $t("Vehicle Requirements") }}</text><text>{{ selectedHistoryOrder.vehicleRequirements }}</text></view><view><text>{{ $t("Customer Service Note") }}</text><text>{{ selectedHistoryOrder.customerServiceNote }}</text></view></view>
        </view>
      </template>
      <template v-else-if="activeView === 'map'">
        <view class="active-map-page">
          <view class="map-card"><view class="map-graphic"><AmapRouteMap :api-key="amapWebKey" :security-js-code="amapSecurityJsCode" :origin="activeTrip?.route.pickupName ?? ''" :destination="activeTrip?.route.dropoffName ?? ''" is-compact-preview is-interactive @metrics="mapMetrics = $event" /><text class="map-tag">{{ $t("●　AMap Live Route") }}</text></view><view class="map-mileage"><view><strong>{{ mapMetrics.coveredKm.toFixed(1) }}</strong><small>{{ $t("km Covered") }}</small></view><view><strong>{{ mapMetrics.hasCurrentPosition ? mapMetrics.distanceToPickupKm.toFixed(1) : '—' }}</strong><small>{{ $t("km to Pickup") }}</small></view><view><strong>{{ mapMetrics.hasCurrentPosition && mapMetrics.etaMinutes ? `${mapMetrics.etaMinutes} ${$t('min')}` : '—' }}</strong><small>{{ $t('ETA to Pickup') }}</small></view></view><text class="map-disclaimer">{{ $t("●　AMap Route Estimate ·") }} {{ $t(mapMetrics.hasCurrentPosition ? 'Live GPS · Blue dashed line: Driver to Pickup' : 'Current Location Unavailable') }}</text></view>
          <view class="journey-card">
            <view class="journey-header"><text>{{ $t("Journey Progress") }}</text><button @tap="openActiveView('details')">{{ $t("Details　›") }}</button></view>
            <view class="journey-route"><small>{{ $t("Pickup") }}</small><text>{{ activeTrip?.route.pickupName }}</text><small>{{ $t("Drop-Off") }}</small><text>{{ activeTrip?.route.dropoffName }}</text></view>
            <view class="journey-stages">
              <view v-for="(stage, index) in activeTripStages" :key="stage" class="journey-stage" :class="{ completed: index < activeStageIndex, active: index === activeStageIndex, pending: index > activeStageIndex }"><text class="journey-stage-number">{{ index + 1 }}</text><text class="journey-stage-label">{{ $t(stage) }}</text><text v-if="activeStageTimes[index]" class="journey-stage-time">{{ formatStageTime(activeStageTimes[index]!) }}</text></view>
            </view>
            <view v-if="activeTrip?.allowedActions[0]" class="slide-confirm active-slide journey-action"><text>{{ $t(activeProgressLabel) }}</text><button class="slide-thumb" role="slider" :aria-label="activeProgressLabel" aria-valuemin="0" aria-valuemax="100" :aria-valuenow="Math.round(activeSlideProgress)" :style="{ left: `calc(${activeSlideProgress}% - ${activeSlideProgress * .56}px + 4px)` }" @pointerdown.stop.prevent="startActivePointerSlide" @pointermove.stop.prevent="moveActivePointerSlide" @pointerup.stop.prevent="finishActivePointerSlide" @pointercancel.stop="finishActivePointerSlide" @touchstart.stop="startActiveSlide" @touchmove.stop.prevent="moveActiveSlide" @touchend.stop="finishActiveSlide" @touchcancel.stop="finishActiveSlide" @keydown.enter="advanceTrip" @keydown.space="advanceTrip">›</button></view>
            <text class="muted journey-help">{{ $t("Update Your Status at Each Stage of the Journey.") }}</text>
          </view>
        </view>
      </template>
      <template v-else-if="activeView === 'details'">
        <view class="active-details-page"><view class="route-card"><view class="route-card-heading"><text>{{ $t("Your Route") }}</text><small>{{ $t(activeTrip?.route.isCrossBorder ? 'Cross-Border' : 'Local') }}</small></view><view class="detail-route"><view><small>{{ $t("Pickup") }}</small><text>{{ activeTrip?.route.pickupName }}</text></view><view><small>{{ $t("Drop-Off") }}</small><text>{{ activeTrip?.route.dropoffName }}</text></view></view><view class="detail-fact"><text>{{ $t("Order No.") }}</text><text>{{ activeTrip?.tripReference || '—' }}</text></view><view class="detail-fact"><text>{{ $t("Scheduled Time") }}</text><text>{{ activeTrip ? new Date(activeTrip.pickupAt).toLocaleString(localeTag()) : '—' }}</text></view><view class="detail-fact"><text>{{ $t("Vehicle") }}</text><text>{{ activeTrip?.assignment.vehicleLabel || '—' }}</text></view><button class="gold-button" @tap="openActiveView('map')">{{ $t("Open Active Trip　›") }}</button></view><view class="route-card passenger-card"><text class="card-title">{{ $t('Passenger') }}</text><view class="passenger-profile"><text class="passenger-avatar">{{ passengerInitials }}</text><view><text class="passenger-name">{{ activeTrip?.passenger.name || '—' }}</text><text class="passenger-meta">{{ activeTrip?.passenger.phoneMasked || $t('Not available') }} · {{ activeTrip?.passengers || 0 }} {{ $t(activeTrip?.passengers === 1 ? 'Passenger' : 'Passengers') }}</text></view></view><view class="passenger-actions"><button @tap="contactPassenger('Call')"><image src="/static/icons/call-gold.svg" mode="aspectFit" />{{ $t("Call") }}</button><button @tap="contactPassenger('Message')"><image src="/static/icons/message-gold.svg" mode="aspectFit" />{{ $t("Message") }}</button></view><text class="passenger-demo-note">{{ $t("Demo Contact Number · Not a Live Passenger") }}</text></view><view class="route-card"><text class="card-title">{{ $t("Journey Notes") }}</text><text>{{ $t("Special Instructions") }}</text><text class="subcopy">{{ activeTrip?.specialRequests || $t('No special instructions') }}</text><text>{{ $t("Vehicle Requirements") }}</text><text class="subcopy">{{ $t(activeTrip?.vehicleClass === 'BUSINESS_SEDAN' ? 'Standard Sedan' : activeTrip?.vehicleClass.replace(/_/g, ' ') || '') }}</text></view></view>
      </template>
      <template v-else-if="tab === 'home'">
        <view class="home-content">
          <view class="profile-card">
            <view class="profile-line"><image src="/static/figma/driver-avatar.jpg" mode="aspectFill" /><view><text class="driver-name">{{ driverSelf?.name || 'Driver' }}</text><text class="driver-subtitle">{{ driverSelf?.assignedVehicle || 'Melco Limousine Fleet' }} · {{ driverSelf?.propertyId || 'Macau' }}</text></view></view>
            <view class="segmented availability-segmented" :data-status="availability" role="status" :aria-label="`${$t('Current Status')}: ${availability ? $t(availability) : $t(driverSelf ? 'Unknown Status' : 'Loading')}`"><text v-for="option in (['Available', 'On Break', 'Off Duty'] as const)" :key="option" :class="{ selected: availability === option }">{{ $t(option) }}</text></view>
            <text v-if="driverSelf && !availability" class="availability-note">{{ $t('Unknown Status') }}</text>
            <text v-if="availability === 'On Trip'" class="availability-note">{{ $t('Current Status') }}: {{ $t('On Trip') }}</text>
          </view>
          <view class="section-heading"><text>{{ $t("Active Trip") }}</text><text class="muted">{{ $t(tripStatus === 'active' ? '1 Active Trip' : 'No Active Trip') }}</text></view>
          <view v-if="tripStatus !== 'active'" class="empty-card"><image src="/static/figma/sedan.svg" mode="aspectFit" /><text>{{ $t("No Order") }}</text></view>
          <view v-else class="active-card">
            <view class="active-heading"><image src="/static/icons/vehicle.svg" mode="aspectFit" /><view><text>{{ $t(activeTrip?.route.isCrossBorder ? 'HK–Macau Transfer' : activeTrip?.serviceType.replace(/_/g, ' ') || '') }}</text><small>{{ $t(activeTrip?.vehicleClass === 'BUSINESS_SEDAN' ? 'Standard Sedan' : activeTrip?.vehicleClass.replace(/_/g, ' ') || '') }}</small></view></view>
            <view class="active-info"><text class="active-info-label">{{ $t('Order No.') }}</text><text class="active-info-value">{{ activeTrip?.tripReference || '—' }}</text></view>
            <view class="active-info"><text class="active-info-label">{{ $t('Customer Details') }}</text><view class="active-customer"><text>{{ activeTrip?.passenger.name || '—' }}</text><small>{{ activeTrip?.passenger.phoneMasked || $t('Not available') }} · {{ activeTrip?.passengers || 0 }} {{ $t(activeTrip?.passengers === 1 ? 'Passenger' : 'Passengers') }}</small></view></view>
            <text class="active-time">{{ activeTrip ? new Date(activeTrip.pickupAt).toLocaleString(localeTag()) : '' }}</text>
            <view class="active-route"><text>{{ activeTrip?.route.pickupName }}</text><text>{{ activeTrip?.route.dropoffName }}</text></view>
            <view class="slide-confirm active-slide"><text>{{ $t(activeProgressLabel) }}</text><button class="slide-thumb" role="slider" :aria-label="activeProgressLabel" aria-valuemin="0" aria-valuemax="100" :aria-valuenow="Math.round(activeSlideProgress)" :style="{ left: `calc(${activeSlideProgress}% - ${activeSlideProgress * .56}px + 4px)` }" @pointerdown.stop.prevent="startActivePointerSlide" @pointermove.stop.prevent="moveActivePointerSlide" @pointerup.stop.prevent="finishActivePointerSlide" @pointercancel.stop="finishActivePointerSlide" @touchstart.stop="startActiveSlide" @touchmove.stop.prevent="moveActiveSlide" @touchend.stop="finishActiveSlide" @touchcancel.stop="finishActiveSlide" @keydown.enter="advanceTrip" @keydown.space="advanceTrip">›</button></view>
            <view class="active-links"><button @tap="openActiveView('map')">{{ $t("Open Trip Map") }}</button><button @tap="openActiveView('details')">{{ $t("View Trip Details") }}</button></view>
          </view>
           <view class="section-heading shift-heading"><text>{{ $t('Next 7 Days Shifts') }}</text><text class="muted">{{ $t('Macau Time') }}</text></view>
           <view class="shift-panel">
             <text v-if="isShiftLoading && !shiftDays.length" class="shift-message">{{ $t('Loading shifts…') }}</text>
             <template v-else-if="shiftError"><text class="shift-message shift-error">{{ shiftError }}</text><button class="shift-retry" @tap="loadDriverShifts()">{{ $t('Retry') }}</button></template>
             <template v-else><view v-for="day in shiftDays" :key="day.businessDate" class="shift-day"><view class="shift-day-heading"><text>{{ formatShiftDate(day.businessDate) }}</text><text>{{ day.businessDate }}</text></view><text v-if="!day.shifts.length" class="shift-empty">{{ $t('No Shift') }}</text><view v-for="(shift, index) in day.shifts" :key="shift.shiftId || `${day.businessDate}-${index}`" class="shift-detail"><view class="shift-main"><text>{{ $t(shiftStatusLabel(shift.status)) }}</text><text>{{ shift.startsAt && shift.endsAt ? `${shift.startsAt}–${shift.endsAt}` : '—' }}<text v-if="shift.endsNextDay" class="shift-next-day"> +1</text></text></view><text class="shift-secondary">{{ $t(shiftPeriodLabel(shift.period)) }}<template v-if="shift.propertyId"> · {{ shift.propertyId }}</template><template v-if="shift.vehiclePlate"> · {{ shift.vehiclePlate }}</template></text><text v-if="shift.role || shift.attendance" class="shift-secondary">{{ shift.role ? $t(shift.role === 'PREFERRED' ? 'Preferred' : 'Standby') : '' }}{{ shift.role && shift.attendance ? ' · ' : '' }}{{ shift.attendance ? $t(shift.attendance === 'ON_TIME' ? 'On Time' : 'Scheduled') : '' }}</text></view></view></template>
           </view>
        </view>
      </template>
      <template v-else-if="tab === 'history'">
        <view class="page-section">
          <view class="segmented history-filters"><view class="history-filter-indicator" :style="{ transform: `translateX(calc(${historyFilterIndex * 100}% + ${historyFilterIndex * 2}px))` }" aria-hidden="true" /><button v-for="filter in (['All Trips', 'Completed', 'Cancelled'] as const)" :key="filter" :class="{ selected: historyFilter === filter }" :aria-pressed="historyFilter === filter" @tap="selectHistoryFilter(filter)">{{ $t(filter) }}</button></view>
          <text v-if="isHistoryLoading && !visibleHistory.length" class="history-state">{{ $t('Loading trip history…') }}</text>
          <text v-else-if="hasHistoryError && !visibleHistory.length" class="history-state">{{ $t('Unable to load trip history.') }}</text>
          <button v-if="hasHistoryError && !visibleHistory.length && !isHistoryLoading" class="history-retry" @tap="loadHistory()">{{ $t('Retry') }}</button>
          <text v-if="hasHistoryLoaded && !hasHistoryError && !isHistoryLoading && !visibleHistory.length" class="history-state">{{ $t('No trips found.') }}</text>
          <view v-for="(trip, index) in visibleHistory" :key="`${historyFilter}-${trip.id}`" :style="{ animationDelay: `${index * 100}ms` }" class="history-card" role="button" tabindex="0" :aria-label="`View order details for ${trip.service}, ${trip.date}`" @tap="openOrderDetails(trip)" @keydown.enter="openOrderDetails(trip)" @keydown.space.prevent="openOrderDetails(trip)"><view class="history-top"><view class="history-service"><image src="/static/icons/vehicle.svg" mode="aspectFit" /><text>{{ trip.service }}</text></view><text class="gold-text">{{ $t(trip.status) }}</text></view><text class="muted history-date">{{ trip.date }}</text><text class="muted history-order">{{ $t('Order No.') }} · {{ trip.displayId }}</text><view class="route-points"><text>{{ trip.pickup }}</text><text>{{ trip.dropoff }}</text></view><view class="history-facts"><view><text>{{ $t("Accepted Time") }}</text><text>{{ trip.accepted }}</text></view><view><text>{{ $t("Pickup Time") }}</text><text>{{ trip.started }}</text></view><view><text>{{ $t("Drop-Off Time") }}</text><text>{{ trip.ended }}</text></view></view></view>
          <text v-if="isHistoryLoading && visibleHistory.length" class="history-state">{{ $t('Loading trip history…') }}</text>
        </view>
      </template>
      <template v-else-if="tab === 'inspection'">
        <view class="page-section inspection-page-section"><view class="inspection-card"><text class="card-title">{{ $t("Vehicle Checklist") }}</text><view class="segmented"><button v-for="type in (['Pre-Trip', 'Post-Trip'] as const)" :key="type" :class="{ selected: inspectionType === type }" @tap="inspectionType = type">{{ $t(type) }}</button></view><text class="muted inspection-help">{{ $t("Check Each Item before Submitting Your Inspection.") }}</text><view v-for="item in inspectionItems" :key="item" class="check-row"><text>{{ $t(item) }}</text><view><button :class="{ checked: answers[item] === 'Pass' }" :aria-pressed="answers[item] === 'Pass'" @tap="answers[item] = 'Pass'"><image class="inspection-check-icon" :src="answers[item] === 'Pass' ? '/static/icons/inspection-check-active.svg' : '/static/icons/inspection-check-inactive.svg'" mode="aspectFit" /><text>{{ $t("Pass") }}</text></button><button :class="{ failed: answers[item] === 'Fail' }" :aria-pressed="answers[item] === 'Fail'" @tap="answers[item] = 'Fail'">{{ $t("Fail") }}</button></view></view><button class="gold-button" @tap="submitInspection">{{ $t("Submit Inspection ☑") }}</button></view><view class="inspection-card issue-card"><text class="card-title">{{ $t("Something Needs Attention?") }}</text><text class="subcopy">{{ $t("Report Vehicle Issues or Maintenance Needs to Keep Every Journey Safe.") }}</text><button class="outline-gold" @tap="openVehicleIssue">{{ $t("Report an Issue　›") }}</button></view></view>
      </template>
      <template v-else>
        <view class="settings-content"><view class="settings-heading"><image src="/static/icons/globe.svg" mode="aspectFit" /><view><text class="card-title">{{ $t("Interface Language") }}</text><text class="subcopy">{{ $t("Select a Language for Your Driver App.") }}</text></view></view><button class="settings-row" @tap="isLanguageOpen = true"><text>{{ $t("Language") }}</text><text class="muted">{{ language }}　›</text></button><text class="muted centered settings-note">{{ $t("Language Preference Is Synced with Your Account.") }}</text><button class="sign-out-button" @tap="signOut">{{ $t("Sign Out") }}</button></view>
      </template>
    </scroll-view>

    <button v-if="tab === 'home' && !isSosOpen && hasIncomingRequest && driverSelf?.capabilities.canAcceptOffers !== false && canReceiveRequest(availability ?? 'Off Duty', tripStatus)" class="request-banner" @tap="openRequest"><image src="/static/icons/dispatch.svg" mode="aspectFit" /><view><text>{{ $t("You Have a New Transfer Order") }}</text><small>{{ $t("Please Tap to View") }}</small></view><text class="banner-arrow">›</text></button>
    <view v-if="!isRequestOpen && activeView !== 'map' && activeView !== 'order-details'" class="bottom-nav"><button v-for="item in ([{ key: 'home', label: 'Home' }, { key: 'history', label: 'History' }, { key: 'inspection', label: 'Inspection' }, { key: 'settings', label: 'Settings' }] as const)" :key="item.key" :class="{ active: tab === item.key && !isSosOpen }" @tap="switchTab(item.key)"><image class="nav-icon" :src="tabIcon(item.key)" mode="aspectFit" /><text>{{ $t(item.label) }}</text></button></view>

    <view v-if="isRequestOpen && currentOffer" class="sheet-backdrop" @tap.self="closeRequest"><view class="request-sheet" @mousemove="moveSlide" @mouseup="finishSlide" @mouseleave="finishSlide"><view class="grabber" /><view class="request-meta"><image src="/static/icons/vehicle.svg" mode="aspectFit" /><view><strong>{{ offerRemainingSeconds }}</strong><small>{{ $t("Seconds to Respond") }}</small></view></view><text class="eyebrow">{{ $t("Incoming Request") }}</text><text class="request-title">{{ currentOffer.tripReference }}</text><text class="subcopy">{{ $t(currentOffer.trip.vehicleClass === 'BUSINESS_SEDAN' ? 'Standard Sedan' : currentOffer.trip.vehicleClass.replace(/_/g, ' ')) }}</text><view class="request-route"><view><small>{{ $t("Pickup") }}</small><text>{{ currentOffer.trip.pickupName }}</text></view><view><small>{{ $t("Drop-Off") }}</small><text>{{ currentOffer.trip.dropoffName }}</text></view></view><view class="request-time"><text>◷　{{ new Date(currentOffer.trip.pickupAt).toLocaleString(localeTag()) }}</text><text>{{ currentOffer.trip.passengers }} {{ $t("Passengers") }}</text></view><button class="outline-gold" @tap="declineTrip">{{ $t("Decline") }}</button><view class="slide-confirm"><text>{{ $t("Accept Trip") }}</text><button class="slide-thumb" role="slider" :aria-label='$t("Accept Trip")' aria-valuemin="0" aria-valuemax="100" :aria-valuenow="Math.round(slideProgress)" :style="{ left: `calc(${slideProgress * .86}% + 4px)` }" @mousedown.stop="startSlide" @touchstart.stop="startSlide" @touchmove.stop.prevent="moveSlide" @touchend.stop="finishSlide" @keydown.enter="acceptTrip" @keydown.space="acceptTrip">›</button></view><text class="muted centered">{{ $t("Declined Trips Are Re-Dispatched to the Next Driver.") }}</text></view></view>
    <view v-if="isVehicleIssueOpen" class="sheet-backdrop issue-sheet-backdrop" @tap="closeVehicleIssue"><view class="issue-sheet" @tap.stop role="dialog" aria-modal="true" :aria-label='$t("Report a Vehicle Issue")'><view class="grabber" /><view class="issue-sheet-heading"><text>{{ $t("Report an Issue") }}</text></view><text class="issue-label">{{ $t("Issue Description") }}</text><textarea v-model="vehicleIssueDescription" class="issue-description" :placeholder='$t("Describe the Issue or Maintenance Needed…")' maxlength="1000" /><button class="gold-button issue-save-button" @tap="submitVehicleIssue">{{ $t("Save Report") }}</button></view></view>
    <view v-if="isLanguageOpen" class="sheet-backdrop" @tap.self="isLanguageOpen = false"><view class="language-sheet" role="dialog" aria-modal="true" :aria-label='$t("Select Language")'><view class="grabber" /><text class="language-title">{{ $t("Select Language") }}</text><button v-for="option in ([{ value: 'English', label: 'English', subtitle: 'English' }, { value: '繁體中文', label: 'Traditional Chinese', subtitle: '繁體中文' }, { value: '简体中文', label: 'Simplified Chinese', subtitle: '简体中文' }] as const)" :key="option.value" class="language-option" :class="{ selected: language === option.value }" @tap="changeLanguage(option.value)"><view><text>{{ $t(option.label) }}</text><small>{{ option.subtitle }}</small></view><text v-if="language === option.value" class="language-check">✓</text></button><button class="language-cancel" @tap="isLanguageOpen = false">{{ $t("Cancel") }}</button></view></view>
  </view>
</template>

<style scoped lang="scss">
.driver-page { min-height:100vh; background:#f8f8f8; color:#171717; font-size:14px; }

.top-bar { display:flex; height:78px; padding:0 16px; align-items:center; justify-content:space-between; background:#fff; }
.brand { width:167px; height:50px; }
.top-title { flex:1; padding-left:44px; text-align:center; font-size:17px; font-weight:700; }
.sos-button { display:flex; align-items:center; gap:7px; color:#c64a4a; font-size:14px; }
.shield { display:inline-flex; width:18px; height:20px; align-items:center; justify-content:center; border-radius:0 0 9px 9px; background:#bd5b5b; color:#fff; font-weight:700; }
.back-button { display:flex; align-items:center; gap:3px; font-size:16px; }.back-button:first-letter { font-size:30px; }
.content { height:calc(100vh - 206px); }.home-content,.page-section,.settings-content,.sos-content { padding:24px 16px 100px; }
.profile-card,.empty-card,.active-card,.history-card,.inspection-card,.sos-card { border-radius:13px; background:#fff; box-shadow:0 1px 14px rgba(0,0,0,.025); }
.profile-card { padding:16px; }.profile-line { display:flex; align-items:center; gap:12px; }.profile-line image { width:45px; height:45px; border-radius:50%; }.profile-line view { display:flex; flex-direction:column; gap:5px; }.driver-name { font-size:16px; }.driver-subtitle,.subcopy { color:#777; font-size:13px; line-height:1.5; }
.segmented { display:flex; margin-top:18px; padding:4px; gap:2px; border-radius:10px; background:#f2f2f2; }.segmented button { flex:1; height:44px; border-radius:8px; color:#575757; font-size:14px; }.segmented button.selected { background:#f1ae2e; color:#fff; }
.availability-segmented{--availability-color:#ebaa32}.availability-segmented[data-status='On Break']{--availability-color:#5b9b72}.availability-segmented[data-status='Off Duty']{--availability-color:#939393}.availability-segmented>text{display:flex;min-width:0;height:44px;flex:1;align-items:center;justify-content:center;border-radius:8px;color:#575757;font-size:14px}.availability-segmented>text.selected{background:var(--availability-color);color:#fff;font-weight:500}.availability-note{display:block;margin-top:9px;color:#8b6b20;font-size:12px}
.shift-heading{margin-top:28px}.shift-panel{overflow:hidden;border-radius:13px;background:#fff;box-shadow:0 1px 14px rgba(0,0,0,.025)}.shift-day{padding:15px 16px;border-bottom:1px solid #ededed}.shift-day:last-child{border-bottom:0}.shift-day-heading,.shift-main{display:flex;align-items:center;justify-content:space-between;gap:10px}.shift-day-heading{margin-bottom:9px;font-weight:600}.shift-day-heading text:last-child{color:#9299a5;font-size:12px;font-weight:400}.shift-empty,.shift-message{display:block;color:#9299a5;font-size:13px}.shift-message{padding:20px 16px}.shift-error{color:#ac4848;overflow-wrap:anywhere}.shift-detail{padding:10px 0;border-top:1px solid #f3f3f3}.shift-main{text-align:right;font-size:15px;font-weight:600}.shift-main text:first-child{text-align:left;color:#b57a17}.shift-secondary{display:block;margin-top:5px;color:#697386;font-size:12px}.shift-next-day{color:#697386;font-size:11px}.shift-retry{width:120px;height:40px;margin:0 16px 16px;border:1px solid #ebaa32;border-radius:8px;background:#fff;color:#b97914;font-size:13px}
.section-heading { display:flex; margin:26px 0 18px; align-items:center; justify-content:space-between; }.section-heading text:first-child { font-size:21px; font-weight:700; }.muted { color:#999; font-size:12px; }.empty-card { display:flex; height:200px; align-items:center; justify-content:center; flex-direction:column; gap:20px; color:#999; }.empty-card image { width:220px; height:102px; }.active-card { padding:22px; }.active-card .card-title { margin:10px 0; }.card-title { display:block; font-size:20px; font-weight:700; }.gold-button { display:flex; width:100%; height:50px; margin-top:18px; align-items:center; justify-content:center; border-radius:9px; background:#f1ae2e; color:#fff; font-size:16px; font-weight:600; }.gold-button:active,.request-banner:active { opacity:.85; }.eyebrow,.gold-text { color:#e99b12; }.eyebrow { font-size:13px; }
.request-banner { position:fixed; right:16px; bottom:calc(72px + env(safe-area-inset-bottom)); left:16px; z-index:4; display:flex; min-height:68px; padding:10px 14px; align-items:center; gap:10px; border-radius:9px; background:#f1ae2e; color:#fff; box-shadow:0 4px 20px rgba(220,155,20,.15); }.request-banner view { display:flex; flex:1; flex-direction:column; align-items:flex-start; gap:5px; font-size:15px; }.request-banner small { font-size:12px; }.banner-arrow { display:flex; width:44px; height:44px; align-items:center; justify-content:center; border-radius:9px; background:#fff; color:#eaa01a; font-size:28px; }
.bottom-nav { position:fixed; right:0; bottom:20px; left:0; z-index:6; display:flex; height:64px; border-top:1px solid #eee; background:#fff; }.bottom-nav button { display:flex; flex:1; align-items:center; justify-content:center; flex-direction:column; color:#999; font-size:11px; }.bottom-nav button.active { color:#eca423; }.nav-icon { font-size:23px; line-height:27px; }
.history-filters { margin:0 0 24px; position:relative; }.history-filter-indicator { position:absolute; top:4px; bottom:4px; left:4px; z-index:0; width:calc((100% - 12px)/3); border-radius:8px; background:#f1ae2e; box-shadow:0 2px 8px rgba(220,155,20,.25); pointer-events:none; transition:transform .28s cubic-bezier(.2,.75,.25,1); }.history-filters button { position:relative; z-index:1; background:transparent; transition:color .25s ease; }.history-filters button.selected { background:transparent; color:#fff; }.history-card { margin-bottom:15px; padding:18px 16px; animation:history-card-enter .46s cubic-bezier(.2,.75,.25,1) both; }.history-top { display:flex; justify-content:space-between; }.history-date { display:block; margin:6px 0 4px 32px; }.history-order { display:block; margin:0 0 14px 32px; color:#4b5563; font-size:12px; }.route-points { display:flex; margin-left:48px; padding:0 0 16px 0; gap:14px; flex-direction:column; border-bottom:1px solid #eee; }.route-points text { position:relative; }.route-points text::before { position:absolute; left:-25px; top:5px; width:8px; height:8px; border-radius:2px; background:#f1ae2e; content:''; }.route-points text:last-child::before { background:#666; }.history-facts { display:flex; padding:14px 0 0 48px; gap:8px; flex-direction:column; }.history-facts view { display:flex; justify-content:space-between; font-size:12px; }.history-facts view text:first-child { color:#666; }@keyframes history-card-enter{from{opacity:0;transform:translateY(24px)}to{opacity:1;transform:translateY(0)}}@media(prefers-reduced-motion:reduce){.history-card{animation:none}.history-filter-indicator{transition:none}}
.inspection-card { padding:16px; }.inspection-card .segmented { margin-bottom:14px; }.inspection-help { display:block; margin:10px 0; }.check-row { display:flex; min-height:64px; align-items:center; justify-content:space-between; gap:8px; border-bottom:1px solid #eee; }.check-row>text { max-width:52%; }.check-row view { display:flex; gap:3px; }.check-row button { min-width:46px; padding:9px 4px; border-radius:8px; background:#f7f7f7; color:#999; font-size:12px; }.check-row button.checked { color:#777; }.check-row button.failed { background:#ffeaea; color:#bd4c4c; }.issue-card { margin-top:16px; }.issue-card .subcopy { display:block; margin-top:10px; }.outline-gold { width:100%; height:50px; margin-top:16px; border:1px solid #e5e5e5; border-radius:9px; color:#eaa01a; font-size:16px; }
.settings-heading { display:flex; margin:2px 6px 24px; gap:14px; }.settings-heading>.gold-text { font-size:22px; }.settings-heading .subcopy { display:block; margin-top:3px; }.settings-row { display:flex; height:57px; padding:0 16px; align-items:center; justify-content:space-between; border-radius:11px; background:#fff; font-size:15px; }.centered { display:block; text-align:center; }.settings-note { margin-top:13px; }.sos-heading { display:block; margin-bottom:16px; font-size:22px; font-weight:700; }.sos-card { margin-bottom:16px; padding:18px 16px; }.sos-card .subcopy { display:block; margin-top:8px; }.sos-card .muted { margin-top:12px; }.field-title { display:block; margin:16px 0 7px; color:#666; font-size:12px; }.picker-field,.incident-input { display:flex; width:100%; min-height:47px; padding:12px; align-items:center; justify-content:space-between; border:1px solid #e6e6e6; border-radius:8px; }.incident-input { height:96px; }
.sheet-backdrop { position:fixed; inset:44px 0 0; z-index:10; background:rgba(0,0,0,.4); }.request-sheet { position:absolute; right:0; bottom:0; left:0; padding:8px 16px 36px; border-radius:16px 16px 0 0; background:#fff; }.grabber { width:36px; height:4px; margin:0 auto 18px; border-radius:4px; background:#ddd; }.request-meta { display:flex; align-items:center; justify-content:space-between; }.request-meta>view { display:flex; align-items:flex-end; flex-direction:column; }.request-meta strong { color:#f0a82a; font-size:28px; }.request-meta small { color:#666; }.request-title { display:block; margin:6px 0; font-size:26px; font-weight:700; }.request-route { display:flex; margin:24px 0 18px; gap:26px; flex-direction:column; }.request-route>view { display:flex; padding-left:22px; gap:5px; flex-direction:column; }.request-route small { color:#999; }.request-route text { font-size:16px; }.request-time { display:flex; justify-content:space-between; color:#666; }.request-sheet .centered { margin-top:11px; }
.sos-button{background:transparent}.sos-button image{width:20px;height:20px}.request-banner>image{width:23px;height:23px}.request-banner{bottom:calc(72px + env(safe-area-inset-bottom));min-height:68px}.bottom-nav{bottom:34px;height:49px}.bottom-nav button{gap:3px}.nav-icon{width:24px;height:24px}.history-service{display:flex;align-items:center;gap:12px}.history-service image{width:24px;height:24px}.history-date{margin-left:40px}.route-points{margin-left:40px}.history-facts{padding-left:40px}.history-card{padding:18px 16px}.sheet-backdrop{background:#0006;animation:fade-in .25s ease-out}.request-sheet{padding:8px 16px 36px;animation:sheet-slide-up .28s cubic-bezier(.2,.8,.2,1)}.request-meta image{width:24px;height:24px}.request-sheet .gold-button{position:relative;margin-top:12px;background:#ebaa32}.request-sheet .outline-gold{color:#e9a024}.request-route{position:relative;gap:20px}.request-route:before{position:absolute;top:18px;bottom:18px;left:4px;border-left:1px dotted #d5d5d5;content:''}.request-route>view{position:relative}.request-route>view:before{position:absolute;left:0;top:6px;width:8px;height:8px;border-radius:2px;background:#ebaa32;content:''}.request-route>view:last-child:before{background:#666}.check-row button.checked{background:#f7f7f7;color:#999}.inspection-card .gold-button{background:#ebaa32}@keyframes sheet-slide-up{from{transform:translateY(100%)}to{transform:translateY(0)}}@keyframes fade-in{from{opacity:0}to{opacity:1}}@media(prefers-reduced-motion:reduce){.sheet-backdrop,.request-sheet{animation:none}}
.request-sheet{height:605px;max-height:calc(100vh - 44px);box-sizing:border-box;overflow-y:auto}.request-meta{margin-bottom:18px}.request-route{margin-top:26px;margin-bottom:33px;gap:29px}.request-sheet .outline-gold{margin-top:31px}.request-sheet .centered{margin-top:19px}.accept-chevron{position:absolute;left:4px;top:4px;display:flex;width:48px;height:42px;align-items:center;justify-content:center;border-radius:7px;background:#fff;color:#eaa128;font-size:28px;font-weight:400}.bottom-nav uni-button{background:transparent;border:0;box-shadow:none}.bottom-nav uni-button::after{display:none}
.sos-top-title{position:absolute;right:95px;left:95px;padding:0;text-align:center}.back-button{color:#eaa32d}.sos-content{padding-top:24px}.help-card{min-height:262px;padding:32px 16px 18px;text-align:center}.help-card>image{display:block;width:24px;height:24px;margin:0 0 13px 14px}.help-card .card-title{font-size:23px}.help-card .subcopy{max-width:290px;margin:10px auto 0;font-size:14px}.contact-button{display:flex;width:100%;height:50px;margin-top:17px;align-items:center;justify-content:center;gap:9px;border-radius:7px;background:#bd5350;color:#fff;font-size:16px}.contact-button image{width:19px;height:19px}.help-card .muted{max-width:290px;margin:10px auto 0;font-size:12px}
.top-bar{position:relative}.sos-button{width:62px;margin:0 0 0 auto;padding:0;background:transparent;justify-content:flex-end}.back-button{position:absolute;left:16px;margin:0;padding:0;background:transparent}.help-card{padding-top:25px;margin-bottom:23px}
.settings-heading image{width:24px;height:24px;margin-top:8px}.settings-row{text-align:left}.language-sheet{position:absolute;right:0;bottom:0;left:0;height:399px;padding:8px 16px 34px;border-radius:16px 16px 0 0;background:#fff;box-sizing:border-box;animation:sheet-slide-up .28s cubic-bezier(.2,.8,.2,1)}.language-sheet .grabber{margin-bottom:18px}.language-title{display:block;margin-bottom:18px;text-align:center;font-size:17px;font-weight:700}.language-option{display:flex;width:100%;height:75px;padding:0 16px;align-items:center;justify-content:space-between;border-bottom:1px solid #e5e5e5;text-align:left;line-height:normal}.language-option.selected{border-radius:10px 10px 0 0;background:#fff3dc;color:#e9a42c}.language-option>view{display:flex;flex-direction:column;gap:6px}.language-option small{color:#666;font-size:12px}.language-check{display:flex;width:19px;height:19px;align-items:center;justify-content:center;border-radius:2px;background:#ebaa32;color:#fff}.language-cancel{width:100%;height:49px;margin-top:16px;border:1px solid #e3e3e3;border-radius:9px;color:#e9a42c;font-size:16px}@media(prefers-reduced-motion:reduce){.language-sheet{animation:none}}
.language-option,.language-cancel{background:#fff}.language-option.selected{background:#fff3dc}
.issue-sheet-backdrop{inset:0}
.issue-sheet{position:absolute;right:0;bottom:0;left:0;max-height:calc(100vh - 44px);padding:8px 16px 42px;border-radius:16px 16px 0 0;background:#fff;box-sizing:border-box;animation:sheet-slide-up .28s cubic-bezier(.2,.8,.2,1);overflow-y:auto}.issue-sheet .grabber{margin-bottom:16px}.issue-sheet-heading{margin-bottom:16px}.issue-sheet-heading>text{font-size:22px;font-weight:700;line-height:28px}.issue-label{display:block;font-size:14px}.issue-description{display:block;width:100%;height:110px;min-height:110px;margin-top:8px;padding:12px;border:1px solid #e5e5e5;border-radius:8px;background:#f8f8f8;box-sizing:border-box;font-size:16px;line-height:21px}.issue-save-button{margin-top:16px;background:#ebaa32}@media(prefers-reduced-motion:reduce){.issue-sheet{animation:none}}
.sign-out-button{width:100%;height:48px;margin-top:24px;border:1px solid #e2e2e2;border-radius:9px;background:#fff;color:#b84c4c}
.slide-confirm{position:relative;display:flex;height:56px;margin-top:12px;align-items:center;justify-content:center;border-radius:8px;background:#ebaa32;color:#fff;font-size:16px;font-weight:600;user-select:none}.slide-thumb{position:absolute;top:4px;display:flex;width:48px;height:48px;margin:0;padding:0;align-items:center;justify-content:center;border-radius:8px;background:#fff;color:#eba127;font-size:30px;font-weight:400;cursor:grab;touch-action:none;transition:left .08s linear}.slide-thumb:active{cursor:grabbing}.slide-thumb::after{display:none}.request-sheet .slide-confirm+.centered{margin-top:11px}@media(prefers-reduced-motion:reduce){.slide-thumb{transition:none}}
.active-card{padding:16px}.active-heading{display:flex;align-items:center;gap:14px}.active-heading image{width:24px;height:24px}.active-heading>view{display:flex;flex:1;flex-direction:column;gap:5px;font-size:16px}.active-heading small{color:#666;font-size:12px}.active-info{display:flex;margin-top:14px;align-items:flex-start;justify-content:space-between;gap:12px;font-size:13px}.active-info-label{flex-shrink:0;color:#6b7280}.active-info-value,.active-customer{min-width:0;text-align:right;overflow-wrap:anywhere}.active-customer{display:flex;align-items:flex-end;flex-direction:column;gap:3px}.active-customer small{color:#6b7280;font-size:12px}.active-time{display:block;margin:18px 0 16px;font-size:13px}.active-route{display:flex;padding:24px 0 5px 22px;gap:22px;flex-direction:column;border-top:1px solid #eee}.active-route text{position:relative;font-size:18px}.active-route text:before{position:absolute;left:-22px;top:8px;width:8px;height:8px;border-radius:2px;background:#eaa62d;content:''}.active-route text:last-child:before{background:#666}.active-route:before{position:absolute;margin-left:-18px;margin-top:32px;height:46px;border-left:1px dotted #d5d5d5;content:''}.active-slide{margin-top:18px}.active-links{display:flex;margin-top:10px}.active-links button{flex:1;height:30px;color:#e99b20;font-size:14px}.active-links button+button{border-left:1px solid #ddd}
.active-slide{margin-top:27px;font-weight:400}.active-links button{background:#fff}
.map-content-scroll{height:calc(100vh - 122px)}.active-map-page,.active-details-page{padding:24px 16px 90px}.map-card,.journey-card,.route-card{margin-bottom:24px;border-radius:12px;background:#fff;box-shadow:0 1px 14px #00000005}.map-card{overflow:hidden}.map-graphic{position:relative;height:320px;overflow:hidden}.map-graphic image{width:100%;height:100%}.map-tag{position:absolute;top:16px;left:16px;padding:10px 11px;border-radius:4px;background:#fff;color:#555;font-size:12px}.map-tag:first-letter{color:#eaa52d}.map-caption{position:absolute;right:8px;bottom:8px;padding:4px;background:#fff;font-size:11px}.map-mileage{display:flex;height:76px;align-items:center}.map-mileage view{display:flex;flex:1;align-items:center;flex-direction:column;gap:4px;border-right:1px solid #e6e6e6}.map-mileage view:last-child{border:0}.map-mileage strong{font-size:22px}.map-mileage small{font-size:11px}.map-disclaimer{display:block;height:40px;padding-top:11px;border-top:1px solid #f4f4f4;color:#666;text-align:center;font-size:12px}.journey-card{padding:24px 16px}.journey-header,.route-card-heading{display:flex;align-items:center;justify-content:space-between}.journey-header>text,.route-card-heading>text{font-size:22px;font-weight:700}.journey-header button,.route-card-heading small{color:#eaa52d;font-size:14px}.journey-route{display:flex;margin:30px 0 20px;gap:7px;flex-direction:column}.journey-route small{color:#999;font-size:12px}.journey-route text{margin-bottom:15px;font-size:16px}.journey-stages{display:flex;margin-bottom:17px;gap:4px;flex-direction:column}.journey-stage{display:flex;min-height:34px;align-items:center;gap:10px;color:#eaa52d}.journey-stage-number{display:flex;width:26px;height:26px;align-items:center;justify-content:center;border-radius:4px;background:#ebaa32;color:#fff}.journey-stage-label{font-size:14px}.journey-stage-time{margin-left:auto;color:#9299a5;font-size:11px}.journey-stage.pending{color:#9299a5}.journey-stage.pending .journey-stage-number{background:#f1f1f1;color:#9299a5}.journey-stage.completed .journey-stage-number{background:#fff3da;color:#eaa52d}.journey-action{margin:16px 0 10px}.journey-help{display:block;text-align:center}.active-details-page .route-card{padding:16px}.detail-route{display:flex;margin-top:26px;gap:23px;flex-direction:column}.detail-route>view{display:flex;padding-left:22px;gap:5px;flex-direction:column;position:relative}.detail-route>view:before{position:absolute;left:0;top:4px;width:8px;height:8px;border-radius:2px;background:#ebaa32;content:''}.detail-route>view:last-child:before{background:#666}.detail-route small{color:#9299a5;font-size:12px}.detail-route text{font-size:16px}.route-actions{display:flex;margin:26px 0 5px;gap:12px}.route-actions button{flex:1;height:50px;border:1px solid #eee;border-radius:8px;background:#fff;color:#eaa32d;font-size:16px}.route-actions button:first-child{border-color:#ebaa32;background:#ebaa32;color:#fff}.detail-fact{display:flex;min-height:52px;align-items:center;justify-content:space-between;border-bottom:1px solid #eee;font-size:13px}.detail-fact text:last-child{font-weight:500}.route-card>.gold-button{margin-top:16px}.route-card .card-title{margin-bottom:15px}.route-card>.subcopy{display:block;margin:8px 0 20px}
.journey-header button{display:flex;width:auto;min-width:0;height:auto;min-height:44px;margin:0 0 0 auto;padding:0;align-items:center;justify-content:flex-end;background:transparent;line-height:normal;text-align:right}.journey-header button::after{display:none}.map-disclaimer:first-letter{color:#eaa52d}
.passenger-card{padding:16px}.passenger-card .card-title{margin-bottom:16px}.passenger-profile{display:flex;align-items:center;gap:12px}.passenger-avatar{display:flex;width:40px;height:40px;align-items:center;justify-content:center;border-radius:8px;background:#fff3da;color:#eaa52d;font-size:14px}.passenger-profile>view{display:flex;min-width:0;gap:4px;flex-direction:column}.passenger-name{font-size:16px}.passenger-meta{color:#4f596b;font-size:12px}.passenger-actions{display:flex;margin-top:14px;gap:10px}.passenger-actions button{display:flex;height:44px;flex:1;align-items:center;justify-content:center;gap:8px;border:1px solid #e4e4e4;border-radius:8px;background:#fff;color:#eaa52d;font-size:15px}.passenger-actions button::after{display:none}.passenger-actions image{width:20px;height:20px}.passenger-demo-note{display:block;margin-top:12px;color:#4f596b;font-size:11px}
.history-date{margin-bottom:20px}.route-points{gap:20px;padding-bottom:20px}.history-facts{padding-top:18px}
.request-banner{height:68px;min-height:68px;box-sizing:border-box;line-height:normal;justify-content:flex-start}
.content{height:calc(100vh - 134px - env(safe-area-inset-bottom))}.map-content-scroll{height:calc(100vh - 78px)}
.segmented button{display:flex;align-items:center;justify-content:center;padding:0;line-height:normal}
.bottom-nav{bottom:0;height:calc(56px + env(safe-area-inset-bottom));padding:0 4px env(safe-area-inset-bottom);box-sizing:border-box}
.bottom-nav button,.bottom-nav uni-button{display:flex;height:49px;min-height:49px;margin:0;padding:0;align-items:center;justify-content:center;flex-direction:column;gap:3px;border:0;background:transparent;box-shadow:none;font-weight:500;line-height:13px}
.bottom-nav button.active,.bottom-nav uni-button.active{color:#ebaa32}
.bottom-nav .nav-icon{display:block;width:24px;height:24px;min-height:24px;flex:0 0 24px}
.sheet-backdrop{inset:0}.request-sheet{max-height:100vh}
.order-content-scroll{height:calc(100vh - 78px);background:#fff}
.order-details-page{min-height:100%;box-sizing:border-box;padding:24px 16px 36px;background:#fff}
.order-summary-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;font-size:16px}
.order-summary-heading .gold-text{font-size:12px}
.order-reference{display:block;margin-top:18px;color:#4b5563;font-size:13px}
.order-route{position:relative;display:flex;margin-top:26px;gap:29px;flex-direction:column}
.order-route:before{position:absolute;top:8px;bottom:28px;left:4px;border-left:1px dashed #ddd;content:''}
.order-route-stop{position:relative;display:flex;padding-left:22px;gap:5px;flex-direction:column}
.order-route-stop:before{position:absolute;top:6px;left:0;width:8px;height:8px;border-radius:2px;background:#eeb12f;content:''}
.order-route-stop:last-child:before{background:#666}
.order-field-label{color:#9299a5;font-size:12px}
.order-place{font-size:16px}
.order-address{color:#536078;font-size:13px}
.order-times{display:flex;margin-top:31px;padding:13px 0 31px;gap:7px;flex-direction:column;border-top:1px solid #e5e5e5;border-bottom:1px solid #e5e5e5;font-size:12px}
.order-times view{display:flex;justify-content:space-between;gap:12px}
.order-times view text:first-child{color:#536078}
.order-section-title{display:block;margin-bottom:20px;font-size:21px;font-weight:700}
.order-customer{padding:24px 0 30px;border-bottom:1px solid #e5e5e5}
.order-customer-identity{display:flex;align-items:center;gap:12px}
.order-avatar{display:flex;width:44px;height:44px;align-items:center;justify-content:center;border-radius:8px;background:#fff4de;color:#e9a123;font-size:16px}
.order-customer-identity view{display:flex;gap:5px;flex-direction:column;font-size:16px}
.order-customer-identity view text:last-child{color:#536078;font-size:13px}
.order-passengers{display:flex;margin-top:30px;padding-bottom:17px;justify-content:space-between;border-bottom:1px solid #f0f0f0;font-size:13px}
.order-demo-note{display:block;margin-top:13px;color:#9299a5;font-size:12px}
.order-notes{padding-top:24px}
.order-notes view{display:flex;margin-bottom:17px;gap:5px;flex-direction:column;line-height:1.5}
.order-notes view text:first-child{font-size:14px}
.order-notes view text:last-child{color:#536078;font-size:14px}
.history-card[role="button"]{cursor:pointer}
.history-card[role="button"]:focus-visible{outline:2px solid #e9a123;outline-offset:2px}
.history-state{display:block;padding:28px 16px;color:#9299a5;font-size:14px;text-align:center}
.history-retry{width:120px;margin:0 auto 16px;border:1px solid #ebaa32;border-radius:8px;background:#fff;color:#b97914;font-size:14px}
.inspection-help{margin-bottom:5px}
.check-row>text{max-width:calc(100% - 152px)}
.check-row view{gap:4px;flex-shrink:0}
.check-row button{display:flex;flex:0 0 64px;width:64px;min-width:64px;height:44px;margin:0;padding:0 6px;align-items:center;justify-content:center;gap:4px;border:0;border-radius:8px;background:#f9f9f9;color:#999;font-size:12px;line-height:normal;white-space:nowrap;box-sizing:border-box}
.check-row button:first-child{flex-basis:67px;width:67px;min-width:67px}
.check-row button.checked{background:#ebaa32;color:#fff}
.check-row button.failed{background:#bd5350;color:#fff}
.check-row button::after{display:none}
.inspection-check-icon{display:block;flex:0 0 24px;width:24px;height:24px}
.inspection-card .gold-button{margin-top:15px}

@media(min-width:768px){.order-details-page,.inspection-page-section{padding-right:24px;padding-left:24px}}
</style>
