import { describe, expect, it } from 'vitest'
import { EventIdCache, isNotificationMessageV1 } from './notificationContract'

const message = {
  code: 20000,
  message: 'success',
  data: {
    eventId: 'evt_001',
    schemaVersion: 1,
    eventType: 'DRIVER_ASSIGNED',
    notification: {
      notificationId: null,
      type: null,
      summary: 'A driver was assigned.',
      tripReference: 'TR-001',
      occurredAt: '2026-10-13T08:30:00Z',
      targetType: 'TRIP',
      targetId: 'trip_001',
    },
  },
  traceId: 'tr_001',
}

describe('SignalR notification contract', () => {
  it('accepts a valid v1 notification envelope', () => {
    expect(isNotificationMessageV1(message)).toBe(true)
  })

  it('rejects mismatched target fields and non-UTC timestamps', () => {
    expect(isNotificationMessageV1({ ...message, data: { ...message.data, notification: { ...message.data.notification, targetId: null } } })).toBe(false)
    expect(isNotificationMessageV1({ ...message, data: { ...message.data, notification: { ...message.data.notification, occurredAt: '2026-10-13T16:30:00+08:00' } } })).toBe(false)
  })

  it('deduplicates event IDs and evicts the oldest ID at capacity', () => {
    const cache = new EventIdCache(2)
    expect(cache.addIfNew('evt_1')).toBe(true)
    expect(cache.addIfNew('evt_1')).toBe(false)
    expect(cache.addIfNew('evt_2')).toBe(true)
    expect(cache.addIfNew('evt_3')).toBe(true)
    expect(cache.addIfNew('evt_1')).toBe(true)
  })
})
