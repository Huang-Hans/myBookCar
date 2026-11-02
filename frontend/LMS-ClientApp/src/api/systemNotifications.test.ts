import { afterEach, describe, expect, it, vi } from 'vitest'
import { showSystemNotification } from '../../../shared/systemNotifications'

describe('system notifications', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('creates a native notification with the received message and payload', () => {
    const createPushMessage = vi.fn()
    vi.stubGlobal('uni', { createPushMessage })

    showSystemNotification('LMS', 'Trip updated', { eventId: 'event-1' })

    expect(createPushMessage).toHaveBeenCalledWith({ title: 'LMS', content: 'Trip updated', payload: { eventId: 'event-1' } })
  })

  it('does not create an empty notification or require the native API in a browser', () => {
    vi.stubGlobal('uni', undefined)

    expect(() => showSystemNotification('LMS', '   ', {})).not.toThrow()
    expect(() => showSystemNotification('LMS', 'Trip updated', {})).not.toThrow()
  })
})
