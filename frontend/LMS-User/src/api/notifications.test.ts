import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { NotificationMessageV1 } from './notificationContract'

const mocks = vi.hoisted(() => ({
  receive: vi.fn(),
  connected: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn(),
  request: vi.fn(),
}))

vi.mock('./notificationHub', () => {
  class NotificationHubClient {
    subscribe(handler: (message: unknown) => unknown): () => void {
      mocks.receive.mockImplementation(handler)
      return () => undefined
    }

    onConnected(handler: () => unknown): () => void {
      mocks.connected.mockImplementation(handler)
      return () => undefined
    }

    start = mocks.start
    stop = mocks.stop
    invoke = mocks.invoke
  }
  // The mocked export must keep the production class name.
  // eslint-disable-next-line @typescript-eslint/naming-convention
  return { NotificationHubClient }
})

vi.mock('./session', () => ({
  api: { request: mocks.request },
  getStaffAccessToken: vi.fn(() => 'access-token'),
  getStaffHubAccessToken: vi.fn(() => Promise.resolve('access-token')),
  refreshStaffAccessToken: vi.fn(() => Promise.resolve('refreshed-token')),
}))

import { bookingNotifications, bookingNotificationUnreadCount, subscribeBookingCreated, subscribeStaffNotificationConnected } from './notifications'

function bookingCreated(eventId: string): NotificationMessageV1 {
  return {
    code: 20000,
    message: 'success',
    data: {
      eventId,
      schemaVersion: 1,
      eventType: 'BOOKING_CREATED',
      notification: {
        notificationId: `notice_${eventId}`,
        type: 'SYSTEM_ALERT',
        summary: 'A new booking is ready for dispatch.',
        tripReference: 'TR-1',
        occurredAt: '2026-10-15T05:00:00Z',
        targetType: 'TRIP',
        targetId: 'trip_1',
      },
    },
    traceId: 'tr_notification_test',
  }
}

describe('staff booking notifications', () => {
  beforeEach(() => {
    bookingNotifications.value = []
    bookingNotificationUnreadCount.value = 0
    mocks.receive.mockClear()
    mocks.connected.mockClear()
    mocks.invoke.mockReset()
  })

  it('increments the unread count and notifies dispatch listeners once per event', async () => {
    const listener = vi.fn()
    const unsubscribe = subscribeBookingCreated(listener)
    const message = bookingCreated('evt_1')

    await mocks.receive(message)
    await mocks.receive(message)

    expect(bookingNotificationUnreadCount.value).toBe(1)
    expect(bookingNotifications.value).toHaveLength(1)
    expect(listener).toHaveBeenCalledOnce()
    expect(listener).toHaveBeenCalledWith(message)
    unsubscribe()
  })

  it('stops notifying a page after it unsubscribes', async () => {
    const listener = vi.fn()
    const unsubscribe = subscribeBookingCreated(listener)
    unsubscribe()

    await mocks.receive(bookingCreated('evt_2'))

    expect(listener).not.toHaveBeenCalled()
    expect(bookingNotificationUnreadCount.value).toBe(1)
  })

  it('refreshes the snapshot and page subscribers when the SignalR connection completes', async () => {
    mocks.invoke.mockResolvedValue({
      code: 20000,
      message: 'success',
      data: { items: [], unreadCount: 0 },
      traceId: 'tr_notification_snapshot',
    })
    const listener = vi.fn()
    const unsubscribe = subscribeStaffNotificationConnected(listener)

    await mocks.connected()

    expect(mocks.invoke).toHaveBeenCalledWith('getNotificationSnapshot', { page: 1, pageSize: 20 })
    expect(listener).toHaveBeenCalledOnce()
    unsubscribe()
  })

})
