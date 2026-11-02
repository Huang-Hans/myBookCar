export const notificationEventTypes = [
  'BOOKING_CREATED',
  'DRIVER_OFFER_CREATED',
  'DRIVER_OFFER_CLOSED',
  'DRIVER_OFFER_ACCEPTED',
  'DRIVER_ASSIGNED',
  'TRIP_STATUS_CHANGED',
] as const

export type NotificationEventType = typeof notificationEventTypes[number]
export type NotificationTargetType = 'TRIP' | 'DRIVER_OFFER' | 'QUALIFICATION' | null

export interface NotificationContentV1 {
  notificationId: string | null
  type: 'SYSTEM_ALERT' | null
  summary: string
  tripReference: string | null
  occurredAt: string
  targetType: NotificationTargetType
  targetId: string | null
}

export interface NotificationEventV1 {
  eventId: string
  schemaVersion: 1
  eventType: NotificationEventType
  notification: NotificationContentV1
}

export interface NotificationMessageV1 {
  code: 20000
  message: 'success'
  data: NotificationEventV1
  traceId: string
}

export interface NotificationSnapshotItem {
  eventId: string
  eventType: 'BOOKING_CREATED'
  notification: NotificationContentV1
  isRead: boolean
  readAt: string | null
}

export interface NotificationSnapshot {
  items: NotificationSnapshotItem[]
  unreadCount: number
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === 'string'
}

function isUtcDateTime(value: unknown): value is string {
  return typeof value === 'string' && value.endsWith('Z') && Number.isFinite(Date.parse(value))
}

export function isNotificationMessageV1(value: unknown): value is NotificationMessageV1 {
  if (!value || typeof value !== 'object') return false
  const envelope = value as Partial<NotificationMessageV1>
  const data = envelope.data as Partial<NotificationEventV1> | undefined
  const notification = data?.notification as Partial<NotificationContentV1> | undefined
  if (envelope.code !== 20000 || envelope.message !== 'success' || typeof envelope.traceId !== 'string' || !envelope.traceId) return false
  if (!data || typeof data.eventId !== 'string' || !data.eventId || data.schemaVersion !== 1 || !notificationEventTypes.includes(data.eventType as NotificationEventType)) return false
  if (!notification || typeof notification.summary !== 'string' || !isUtcDateTime(notification.occurredAt)) return false
  if (!isNullableString(notification.notificationId) || !isNullableString(notification.tripReference) || !isNullableString(notification.targetId)) return false
  if (notification.type !== null && notification.type !== 'SYSTEM_ALERT') return false
  if (![null, 'TRIP', 'DRIVER_OFFER', 'QUALIFICATION'].includes(notification.targetType as NotificationTargetType)) return false
  return (notification.targetType === null) === (notification.targetId === null)
}

export class EventIdCache {
  private readonly ids = new Set<string>()
  constructor(private readonly capacity = 500) {}

  addIfNew(eventId: string): boolean {
    if (this.ids.has(eventId)) return false
    this.ids.add(eventId)
    if (this.ids.size > this.capacity) {
      const oldest = this.ids.values().next().value as string | undefined
      if (oldest) this.ids.delete(oldest)
    }
    return true
  }
}
