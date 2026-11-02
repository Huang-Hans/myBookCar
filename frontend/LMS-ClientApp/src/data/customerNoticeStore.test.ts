import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { NotificationMessageV1 } from '@/api/notificationContract'

const mocks = vi.hoisted(() => ({
  getSelf: vi.fn(),
  getAccessToken: vi.fn(() => 'token_1'),
  subscribe: vi.fn(),
  onConnected: vi.fn(),
}))

vi.mock('@/api/customer', () => ({
  customerApi: { getSelf: mocks.getSelf },
  customerTripNotice: (trip?: { tripId: string }) => trip ? `Current ${trip.tripId}` : '',
}))
vi.mock('@/api/http', () => ({ getCustomerAccessToken: mocks.getAccessToken }))
vi.mock('@/api/notifications', () => ({
  subscribeCustomerNotifications: mocks.subscribe,
  onCustomerNotificationsConnected: mocks.onConnected,
}))

import {
  clearCustomerNotices, currentCustomerTrip, currentCustomerTripId, currentCustomerTripLabel, customerNotices, customerNotificationBadgeCount,
  initializeCustomerNoticeStore, latestCustomerNotice, markAllCustomerNoticesRead,
  markCustomerNoticeRead, recordCustomerNotice, refreshCurrentCustomerTrip,
  setCurrentCustomerTrip, unreadCustomerNoticeCount,
} from './customerNoticeStore'

const storage = new Map<string, string>()
vi.stubGlobal('sessionStorage', {
  getItem: (key: string) => storage.get(key) ?? null,
  setItem: (key: string, value: string) => { storage.set(key, value) },
  removeItem: (key: string) => { storage.delete(key) },
})

const event = (eventId = 'evt_1', eventType: NotificationMessageV1['data']['eventType'] = 'DRIVER_ASSIGNED'): NotificationMessageV1 => ({
  code: 20000,
  message: 'success',
  traceId: 'tr_1',
  data: {
    eventId, schemaVersion: 1, eventType,
    notification: {
      notificationId: null, type: null, summary: 'Your driver is on the way.',
      tripReference: 'TR-001', occurredAt: '2026-10-20T08:30:00Z',
      targetType: 'TRIP', targetId: 'trip_1',
    },
  },
})

beforeEach(() => {
  clearCustomerNotices()
  storage.clear()
  mocks.getSelf.mockReset()
  mocks.getAccessToken.mockReturnValue('token_1')
})

describe('customer notices shared across pages', () => {
  it('keeps new trip updates, deduplicates them, and tracks read state', () => {
    recordCustomerNotice(event())
    recordCustomerNotice(event())
    recordCustomerNotice(event('evt_2', 'TRIP_STATUS_CHANGED'))
    recordCustomerNotice(event('evt_3', 'BOOKING_CREATED'))

    expect(customerNotices.value.map((notice) => notice.eventId)).toEqual(['evt_2', 'evt_1'])
    expect(latestCustomerNotice.value?.tripId).toBe('trip_1')
    expect(unreadCustomerNoticeCount.value).toBe(2)
    expect(customerNotificationBadgeCount.value).toBe(2)
    markCustomerNoticeRead('evt_2')
    expect(unreadCustomerNoticeCount.value).toBe(1)
    markAllCustomerNoticesRead()
    expect(unreadCustomerNoticeCount.value).toBe(0)
    expect(customerNotificationBadgeCount.value).toBe(0)
    expect(customerNotices.value).toHaveLength(2)
  })

  it('keeps the current trip visible and clears it with session notices', () => {
    setCurrentCustomerTrip({ tripId: 'trip_1' } as Parameters<typeof setCurrentCustomerTrip>[0])
    recordCustomerNotice(event())
    expect(currentCustomerTripLabel.value).toBe('Current trip_1')
    expect(currentCustomerTripId.value).toBe('trip_1')
    expect(customerNotificationBadgeCount.value).toBe(1)
    markAllCustomerNoticesRead()
    expect(customerNotificationBadgeCount.value).toBe(0)
    clearCustomerNotices()
    expect(currentCustomerTrip.value).toBeNull()
    expect(currentCustomerTripId.value).toBe('')
    expect(customerNotices.value).toEqual([])
    expect(unreadCustomerNoticeCount.value).toBe(0)
    expect(customerNotificationBadgeCount.value).toBe(0)
  })

  it('loads the active trip without exposing a stale response after logout', async () => {
    let resolveRequest: (value: { data: { currentTrip: { tripId: string } } }) => void = () => {}
    mocks.getSelf.mockReturnValue(new Promise((resolve) => { resolveRequest = resolve }))
    const pending = refreshCurrentCustomerTrip()
    clearCustomerNotices()
    resolveRequest({ data: { currentTrip: { tripId: 'old_trip' } } })
    await pending
    expect(currentCustomerTrip.value).toBeNull()
  })

  it('subscribes once to live customer notifications', () => {
    initializeCustomerNoticeStore()
    initializeCustomerNoticeStore()
    expect(mocks.subscribe).toHaveBeenCalledTimes(1)
    const handler = mocks.subscribe.mock.calls[0]?.[0] as (message: NotificationMessageV1) => void
    mocks.getSelf.mockResolvedValue({ data: { currentTrip: null } })
    handler(event())
    expect(unreadCustomerNoticeCount.value).toBe(1)
  })

  it('restores the numeric badge immediately after a page reload', async () => {
    setCurrentCustomerTrip({ tripId: 'trip_1' } as Parameters<typeof setCurrentCustomerTrip>[0])
    expect(storage.get('lmsClientActiveTripId')).toBe('trip_1')

    vi.resetModules()
    const reloaded = await import('./customerNoticeStore')
    expect(reloaded.currentCustomerTripId.value).toBe('trip_1')
    expect(reloaded.customerNotificationBadgeCount.value).toBe(0)
  })
})
