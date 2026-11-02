import { ref } from 'vue'
import { api, getStaffAccessToken, getStaffHubAccessToken, refreshStaffAccessToken } from './session'
import { NotificationHubClient } from './notificationHub'
import { TripLocationSubscription, type TripLocationState } from '../../../shared/tripLocations/subscription'
import type { NotificationMessageV1, NotificationSnapshot, NotificationSnapshotItem } from './notificationContract'
import type { ErrorEnvelope, SuccessEnvelope } from './http'
import { apiBaseUrl } from './baseUrl'

const hubOrigin = apiBaseUrl || (typeof window !== 'undefined' ? window.location.origin : 'https://melco1.weprogram.site')
const hubUrl = `${hubOrigin}/api/booking/staff/v1/notifications`
const hub = new NotificationHubClient(hubUrl, getStaffHubAccessToken, refreshStaffAccessToken)

export const bookingNotifications = ref<NotificationSnapshotItem[]>([])
export const bookingNotificationUnreadCount = ref(0)
type BookingCreatedHandler = (message: NotificationMessageV1) => void | Promise<void>
type NotificationConnectedHandler = () => void | Promise<void>
const bookingCreatedHandlers = new Set<BookingCreatedHandler>()
const notificationConnectedHandlers = new Set<NotificationConnectedHandler>()

function newestFirst(left: NotificationSnapshotItem, right: NotificationSnapshotItem): number {
  const occurred = Date.parse(right.notification.occurredAt) - Date.parse(left.notification.occurredAt)
  return occurred || (left.notification.notificationId ?? '').localeCompare(right.notification.notificationId ?? '')
}

async function receiveBookingCreated(message: NotificationMessageV1): Promise<void> {
  if (message.data.eventType !== 'BOOKING_CREATED' || message.data.notification.targetType !== 'TRIP') return
  const tripId = message.data.notification.targetId
  const existing = bookingNotifications.value.find((item) => item.eventId === message.data.eventId
    || Boolean(tripId && item.notification.targetId === tripId))
  if (existing) return
  const item: NotificationSnapshotItem = {
    eventId: message.data.eventId,
    eventType: 'BOOKING_CREATED',
    notification: message.data.notification,
    isRead: false,
    readAt: null,
  }
  bookingNotifications.value = [item, ...bookingNotifications.value].sort(newestFirst).slice(0, 20)
  bookingNotificationUnreadCount.value += 1
  for (const handler of bookingCreatedHandlers) {
    try { await handler(message) }
    catch (error) { console.error(JSON.stringify({ event: 'bookingCreatedHandlerFailed', traceId: message.traceId, message: error instanceof Error ? error.message : 'Unknown handler error' })) }
  }
}

export function subscribeBookingCreated(handler: BookingCreatedHandler): () => void {
  bookingCreatedHandlers.add(handler)
  return () => { bookingCreatedHandlers.delete(handler) }
}

export function subscribeStaffNotificationConnected(handler: NotificationConnectedHandler): () => void {
  notificationConnectedHandlers.add(handler)
  return () => { notificationConnectedHandlers.delete(handler) }
}

export async function refreshBookingNotificationSnapshot(): Promise<void> {
  const envelope = await hub.invoke<SuccessEnvelope<NotificationSnapshot> | ErrorEnvelope>('getNotificationSnapshot', { page: 1, pageSize: 20 })
  if (envelope.code !== 20000 || (!('data' in envelope)) || !envelope.data || !Array.isArray(envelope.data.items)) {
    console.error('LMS notification snapshot failed', { traceId: envelope.traceId, code: envelope.code })
    throw new Error(envelope.message)
  }
  bookingNotifications.value = envelope.data.items.slice().sort(newestFirst)
  bookingNotificationUnreadCount.value = Math.max(0, envelope.data.unreadCount)
}

export async function markBookingNotificationRead(item: NotificationSnapshotItem): Promise<void> {
  const notificationId = item.notification.notificationId
  if (item.isRead) return
  if (notificationId) await api.request<unknown>(`/api/booking/staff/v1/console-notifications/${encodeURIComponent(notificationId)}/read`, { method: 'POST' })
  item.isRead = true
  item.readAt = new Date().toISOString()
  bookingNotificationUnreadCount.value = Math.max(0, bookingNotificationUnreadCount.value - 1)
}

export async function startStaffNotificationHub(): Promise<void> {
  if (!getStaffAccessToken()) return
  await hub.start()
}

export const stopStaffNotificationHub = (): Promise<void> => hub.stop()

export const watchStaffTripLocation = (tripId: string, onState: (state: TripLocationState) => void): TripLocationSubscription =>
  new TripLocationSubscription(hub, tripId, onState)

hub.subscribe(receiveBookingCreated)
hub.onConnected(async () => {
  try { await refreshBookingNotificationSnapshot() }
  catch (error) { console.error(JSON.stringify({ event: 'bookingNotificationSnapshotFailed', message: error instanceof Error ? error.message : 'Unknown snapshot error' })) }
  for (const handler of notificationConnectedHandlers) {
    try { await handler() }
    catch (error) { console.error(JSON.stringify({ event: 'notificationConnectedHandlerFailed', message: error instanceof Error ? error.message : 'Unknown handler error' })) }
  }
})
