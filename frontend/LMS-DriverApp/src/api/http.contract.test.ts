import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest, loginDriver, logoutDriver } from './http'

interface MockRequestOptions {
  success(response: { statusCode: number; data: unknown; header: Record<string, string> }): void
}

const storage = new Map<string, unknown>()
const request = vi.fn()
const reLaunch = vi.fn()

beforeEach(() => {
  storage.clear()
  request.mockReset()
  reLaunch.mockReset()
  vi.stubGlobal('uni', {
    request,
    getStorageSync: (key: string) => storage.get(key),
    setStorageSync: (key: string, value: unknown) => storage.set(key, value),
    removeStorageSync: (key: string) => storage.delete(key),
    reLaunch,
  })
  vi.stubGlobal('plus', {})
})

describe('driver HTTP contract', () => {
  it('stores a successful driver session', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 200, header: {}, data: { code: 20000, message: 'success', traceId: 'tr_login', data: { accessToken: 'access', tokenType: 'Bearer', expiresIn: 900, refreshToken: 'refresh', refreshExpiresIn: 604800, principalType: 'DRIVER' } } }))
    await loginDriver('+85361234567', 'password')
    expect((storage.get('lmsDriverSession') as { accessToken: string }).accessToken).toBe('access')
  })

  it('preserves a failure traceId for support', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 403, header: {}, data: { code: 40300, message: 'Insufficient permission', traceId: 'tr_denied', errors: [] } }))
    await expect(apiRequest('/api/dispatch/driver/v1/me')).rejects.toMatchObject({ code: 40300, traceId: 'tr_denied' })
  })

  it('does not refresh on a non-expired 40100 response', async () => {
    storage.set('lmsDriverSession', { accessToken: 'access', refreshToken: 'refresh', principalType: 'DRIVER' })
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 401, header: {}, data: { code: 40100, message: 'Not authenticated', traceId: 'tr_session', errors: [] } }))
    await expect(apiRequest('/api/dispatch/driver/v1/me')).rejects.toMatchObject({ code: 40100, traceId: 'tr_session' })
    expect(request).toHaveBeenCalledTimes(1)
    expect(storage.has('lmsDriverSession')).toBe(false)
    expect(reLaunch).toHaveBeenCalledWith({ url: '/pages/login/index' })
  })

  it('does not redirect when anonymous login credentials are rejected', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 401, header: {}, data: { code: 40100, message: 'Not authenticated', traceId: 'tr_login_denied', errors: [] } }))
    await expect(loginDriver('+85361234567', 'wrong-password')).rejects.toMatchObject({ code: 40100, traceId: 'tr_login_denied' })
    expect(reLaunch).not.toHaveBeenCalled()
  })

  it('accepts the documented 204 logout response and clears the session', async () => {
    storage.set('lmsDriverSession', { accessToken: 'access', tokenType: 'Bearer', expiresIn: 900, refreshToken: 'refresh', refreshExpiresIn: 100, principalType: 'DRIVER' })
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 204, header: { 'x-trace-id': 'tr_logout' }, data: '' }))
    await logoutDriver()
    expect(storage.has('lmsDriverSession')).toBe(false)
  })
})
