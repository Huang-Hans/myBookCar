import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ withUrl: vi.fn() }))

vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: { Connected: 'Connected', Reconnecting: 'Reconnecting', Disconnected: 'Disconnected' },
  LogLevel: { Warning: 3 },
  HubConnectionBuilder: class HubConnectionBuilder {
    withUrl(url: string, options: unknown): this { mocks.withUrl(url, options); return this }
    withAutomaticReconnect(): this { return this }
    configureLogging(): this { return this }
    build() {
      return {
        state: 'Disconnected',
        on: vi.fn(),
        onreconnecting: vi.fn(),
        onreconnected: vi.fn(),
        onclose: vi.fn(),
      }
    }
  },
}))

import { isAuthenticationFailure, NotificationHubClient } from './notificationHub'

describe('driver notification connection', () => {
  beforeEach(() => { mocks.withUrl.mockClear() })

  it('negotiates on the same-origin API path without cookie credentials', async () => {
    new NotificationHubClient('/api/dispatch/driver/v1/notifications', () => 'driver-token')
    const [url, options] = mocks.withUrl.mock.calls[0] as [string, { withCredentials: boolean; accessTokenFactory: () => Promise<string> }]
    expect(url).toBe('/api/dispatch/driver/v1/notifications')
    expect(options.withCredentials).toBe(false)
    expect(await options.accessTokenFactory()).toBe('driver-token')
  })

  it('does not treat a permission denial as an expired token', () => {
    expect(isAuthenticationFailure(new Error('403 Forbidden'))).toBe(false)
    expect(isAuthenticationFailure(new Error('401 Unauthorized'))).toBe(true)
  })
})
