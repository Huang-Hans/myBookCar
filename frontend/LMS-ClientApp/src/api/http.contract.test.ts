import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiRequest, loginCustomer } from './http'

interface MockRequestOptions {
  url: string
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

describe('customer HTTP contract', () => {
  it('stores a successful customer session', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 200, header: { 'x-trace-id': 'tr_login' }, data: { code: 20000, message: 'success', traceId: 'tr_login', data: { accessToken: 'access', tokenType: 'Bearer', expiresIn: 900, refreshToken: 'refresh', refreshExpiresIn: 604800, principalType: 'CUSTOMER' } } }))
    await loginCustomer('+85361234567', 'password')
    expect((storage.get('lmsCustomerSession') as { accessToken: string }).accessToken).toBe('access')
  })

  it('surfaces the application code, field errors and traceId', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 400, header: {}, data: { code: 40001, message: 'Validation failed', traceId: 'tr_invalid', errors: [{ field: 'phone', message: 'Invalid phone' }] } }))
    await expect(loginCustomer('invalid', 'password')).rejects.toMatchObject({ code: 40001, traceId: 'tr_invalid', errors: [{ field: 'phone', message: 'Invalid phone' }] })
  })

  it('does not redirect when anonymous login credentials are rejected', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 401, header: {}, data: { code: 40100, message: 'Not authenticated', traceId: 'tr_login_denied', errors: [] } }))
    await expect(loginCustomer('+85361234567', 'wrong-password')).rejects.toMatchObject({ code: 40100, traceId: 'tr_login_denied' })
    expect(reLaunch).not.toHaveBeenCalled()
  })

  it('refreshes an expired token once and retries the original request', async () => {
    storage.set('lmsCustomerSession', { accessToken: 'expired', tokenType: 'Bearer', expiresIn: 0, refreshToken: 'refresh', refreshExpiresIn: 100, principalType: 'CUSTOMER' })
    request
      .mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 401, header: {}, data: { code: 40101, message: 'Token expired', traceId: 'tr_expired', errors: [] } }))
      .mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 200, header: {}, data: { code: 20000, message: 'success', traceId: 'tr_refresh', data: { accessToken: 'new', tokenType: 'Bearer', expiresIn: 900, refreshToken: 'new-refresh', refreshExpiresIn: 100, principalType: 'CUSTOMER' } } }))
      .mockImplementationOnce((options: MockRequestOptions) => options.success({ statusCode: 200, header: {}, data: { code: 20000, message: 'success', traceId: 'tr_success', data: { name: 'Guest' } } }))
    const response = await apiRequest<{ name: string }>('/api/booking/customer/v1/me')
    expect(response.data.name).toBe('Guest')
    expect(request).toHaveBeenCalledTimes(3)
  })
})
