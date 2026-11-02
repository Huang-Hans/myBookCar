import { computed, ref } from 'vue'
import { customerApi, customerTripNotice, type CustomerTripSummary } from '@/api/customer'
import { getCustomerAccessToken } from '@/api/http'
import { onCustomerNotificationsConnected, subscribeCustomerNotifications } from '@/api/notifications'
import type { NotificationMessageV1 } from '@/api/notificationContract'

export interface CustomerNotice {
  eventId: string
  summary: string
  occurredAt: string
  tripId: string
  isRead: boolean
}

const activeTripStorageKey = 'lmsClientActiveTripId'
const dismissedTripBannerStorageKey = 'lmsClientDismissedTripBanner'

function readActiveTripId(): string {
  if (!getCustomerAccessToken() || typeof sessionStorage === 'undefined') return ''
  try { return sessionStorage.getItem(activeTripStorageKey) ?? '' }
  catch { return '' }
}

function saveActiveTripId(tripId: string): void {
  if (typeof sessionStorage === 'undefined') return
  try {
    if (tripId) sessionStorage.setItem(activeTripStorageKey, tripId)
    else sessionStorage.removeItem(activeTripStorageKey)
  } catch { /* Storage unavailability does not block notifications. */ }
}

export const currentCustomerTrip = ref<CustomerTripSummary | null>(null)
export const currentCustomerTripId = ref(readActiveTripId())
export const customerNotices = ref<CustomerNotice[]>([])
export const unreadCustomerNoticeCount = computed(() => customerNotices.value.filter((notice) => !notice.isRead).length)
export const currentCustomerTripLabel = computed(() => customerTripNotice(currentCustomerTrip.value ?? undefined))

// 当前行程横幅可关闭。关闭只对“同一行程的同一状态”有效：状态推进（例如出发 → 到达）后横幅重新出现，提醒客户有新进展。
const dismissedTripBannerKey = ref(readDismissedTripBannerKey())
const currentTripBannerKey = computed(() => currentCustomerTrip.value ? `${currentCustomerTrip.value.tripId}:${currentCustomerTrip.value.status}` : '')
export const isTripBannerVisible = computed(() =>
  Boolean(currentCustomerTripId.value && currentCustomerTripLabel.value && currentTripBannerKey.value !== dismissedTripBannerKey.value))

export function dismissTripBanner(): void {
  dismissedTripBannerKey.value = currentTripBannerKey.value
  saveDismissedTripBannerKey(dismissedTripBannerKey.value)
}

function readDismissedTripBannerKey(): string {
  if (typeof sessionStorage === 'undefined') return ''
  try { return sessionStorage.getItem(dismissedTripBannerStorageKey) ?? '' }
  catch { return '' }
}

function saveDismissedTripBannerKey(key: string): void {
  if (typeof sessionStorage === 'undefined') return
  try {
    if (key) sessionStorage.setItem(dismissedTripBannerStorageKey, key)
    else sessionStorage.removeItem(dismissedTripBannerStorageKey)
  } catch { /* Storage unavailability only means the banner shows again after a reload. */ }
}
// Alerts 角标只计未读通知；当前行程在列表中置顶显示，但不计入角标。
export const customerNotificationBadgeCount = computed(() => unreadCustomerNoticeCount.value)
export const latestCustomerNotice = ref<CustomerNotice | null>(null)
export const latestCustomerNoticeAt = ref(0)

let isInitialized = false
let refreshPromise: Promise<void> | null = null
let sessionVersion = 0
let lastRefreshAt = 0

export function setCurrentCustomerTrip(trip: CustomerTripSummary | null | undefined): void {
  currentCustomerTrip.value = trip ?? null
  currentCustomerTripId.value = trip?.tripId ?? ''
  saveActiveTripId(currentCustomerTripId.value)
  lastRefreshAt = Date.now()
}

export function recordCustomerNotice(message: NotificationMessageV1): void {
  const { eventId, eventType, notification } = message.data
  if (!['DRIVER_ASSIGNED', 'TRIP_STATUS_CHANGED'].includes(eventType)
    || notification.targetType !== 'TRIP' || !notification.targetId
    || customerNotices.value.some((notice) => notice.eventId === eventId)) return

  const notice: CustomerNotice = {
    eventId,
    summary: notification.summary,
    occurredAt: notification.occurredAt,
    tripId: notification.targetId,
    isRead: false,
  }
  customerNotices.value = [notice, ...customerNotices.value].slice(0, 20)
  latestCustomerNotice.value = notice
  latestCustomerNoticeAt.value = Date.now()
}

export function markCustomerNoticeRead(eventId: string): void {
  const notice = customerNotices.value.find((item) => item.eventId === eventId)
  if (notice) notice.isRead = true
}

export function markAllCustomerNoticesRead(): void {
  customerNotices.value.forEach((notice) => { notice.isRead = true })
}

export function clearCustomerNotices(): void {
  sessionVersion += 1
  refreshPromise = null
  lastRefreshAt = 0
  currentCustomerTrip.value = null
  currentCustomerTripId.value = ''
  saveActiveTripId('')
  dismissedTripBannerKey.value = ''
  saveDismissedTripBannerKey('')
  customerNotices.value = []
  latestCustomerNotice.value = null
  latestCustomerNoticeAt.value = 0
}

export function refreshCurrentCustomerTrip(force = false): Promise<void> {
  const accessToken = getCustomerAccessToken()
  if (!accessToken) {
    clearCustomerNotices()
    return Promise.resolve()
  }
  if (refreshPromise) return refreshPromise
  if (!force && Date.now() - lastRefreshAt < 30_000) return Promise.resolve()
  const requestedVersion = sessionVersion
  const request = customerApi.getSelf()
    .then((response) => {
      if (requestedVersion === sessionVersion && accessToken === getCustomerAccessToken()) {
        setCurrentCustomerTrip(response.data.currentTrip)
      }
    })
    .catch(() => { /* Keep the last known trip until the next successful refresh. */ })
  refreshPromise = request
  void request.finally(() => { if (refreshPromise === request) refreshPromise = null })
  return request
}

export function initializeCustomerNoticeStore(): void {
  if (isInitialized) return
  isInitialized = true
  subscribeCustomerNotifications((message) => {
    recordCustomerNotice(message)
    if (message.data.notification.targetType === 'TRIP') void refreshCurrentCustomerTrip(true)
  })
  onCustomerNotificationsConnected(() => { void refreshCurrentCustomerTrip(true) })
}
