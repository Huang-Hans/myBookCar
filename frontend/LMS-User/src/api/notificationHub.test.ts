import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  withUrl: vi.fn(),
  start: vi.fn(),
  stop: vi.fn().mockResolvedValue(undefined),
  state: 'Disconnected',
}))

// The mock must preserve the upstream SignalR export and enum names.
/* eslint-disable @typescript-eslint/naming-convention */
vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: { Connected: 'Connected', Reconnecting: 'Reconnecting', Disconnected: 'Disconnected' },
  LogLevel: { Warning: 3 },
  HubConnectionBuilder: class HubConnectionBuilder {
    withUrl(url: string, options: unknown): this { mocks.withUrl(url, options); return this }
    withAutomaticReconnect(): this { return this }
    configureLogging(): this { return this }
    build() {
      return {
        get state() { return mocks.state },
        start: mocks.start,
        stop: mocks.stop,
        on: vi.fn(),
        onreconnecting: vi.fn(),
        onreconnected: vi.fn(),
        onclose: vi.fn(),
      }
    }
  },
}))
/* eslint-enable @typescript-eslint/naming-convention */

import { NotificationHubClient } from './notificationHub'

describe('staff notification connection authentication', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    mocks.withUrl.mockClear()
    mocks.start.mockReset()
    mocks.state = 'Disconnected'
  })
  afterEach(() => { vi.useRealTimers() })

  it('negotiates without cross-origin cookie credentials', () => {
    new NotificationHubClient('/notifications', () => 'access-token')
    expect(mocks.withUrl).toHaveBeenCalledWith('/notifications', expect.objectContaining({ withCredentials: false, accessTokenFactory: expect.any(Function) }))
  })

  it('refreshes once and immediately retries a 401 negotiation', async () => {
    mocks.start.mockRejectedValueOnce(new Error('Status code 401')).mockResolvedValueOnce(undefined)
    const refresh = vi.fn().mockResolvedValue('new-token')
    const hub = new NotificationHubClient('/notifications', () => 'old-token', refresh)

    await hub.start()

    expect(refresh).toHaveBeenCalledOnce()
    expect(mocks.start).toHaveBeenCalledTimes(2)
    expect(vi.getTimerCount()).toBe(0)
  })

  it('does not retry indefinitely after authentication is rejected', async () => {
    mocks.start.mockRejectedValue(new Error('Status code 401'))
    const refresh = vi.fn().mockResolvedValue('new-token')
    const hub = new NotificationHubClient('/notifications', () => 'old-token', refresh)

    await hub.start()
    await vi.runOnlyPendingTimersAsync()

    expect(refresh).toHaveBeenCalledOnce()
    expect(mocks.start).toHaveBeenCalledTimes(2)
    expect(vi.getTimerCount()).toBe(0)
  })

  it('does not refresh or retry a forbidden connection', async () => {
    mocks.start.mockRejectedValueOnce(new Error('Status code 403'))
    const refresh = vi.fn()
    const hub = new NotificationHubClient('/notifications', () => 'access-token', refresh)

    await hub.start()

    expect(refresh).not.toHaveBeenCalled()
    expect(mocks.start).toHaveBeenCalledOnce()
    expect(vi.getTimerCount()).toBe(0)
  })
})
