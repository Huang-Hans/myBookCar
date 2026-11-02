import { beforeEach, describe, expect, it, vi } from 'vitest'
import { clearSession, getSession, request, saveSession } from './http'
import { apiBaseUrl, resolveApiBaseUrl } from './baseUrl'

type Response = { statusCode: number; data: unknown; header?: Record<string, string> }
const storage = new Map<string, unknown>()
const queued: Response[] = []
const requests: Array<{ url: string; method: string; header: Record<string, string> }> = []
beforeEach(() => {
  storage.clear(); queued.length = 0; requests.length = 0
  vi.stubGlobal('uni', {
    getStorageSync: (key: string) => storage.get(key), setStorageSync: (key: string, value: unknown) => storage.set(key, value), removeStorageSync: (key: string) => storage.delete(key), reLaunch: vi.fn(),
    request: (options: { url: string; method: string; header: Record<string, string>; success: (value: Response) => void }) => {
      requests.push({ url: options.url, method: options.method, header: options.header })
      const response = queued.shift()
      if (!response) throw new Error('No mocked response')
      options.success(response)
    },
  })
  vi.stubGlobal('plus', {})
})

describe('standard envelope transport', () => {
  it('uses the same-origin proxy on H5 and the Melco HTTPS gateway on native builds', () => {
    expect(apiBaseUrl).toBe('')
    expect(resolveApiBaseUrl('h5')).toBe('')
    expect(resolveApiBaseUrl('h5', 'file:')).toBe('https://melco1.weprogram.site')
    expect(resolveApiBaseUrl('app')).toBe('https://melco1.weprogram.site')
  })
  it('sends bearer and trace headers and returns a success envelope', async () => {
    saveSession({ accessToken: 'token' })
    queued.push({ statusCode: 200, data: { code: 20000, message: 'success', data: [], traceId: 'tr_response' } })
    const response = await request<unknown[]>('/api/booking/staff/v1/trips')
    expect(response.traceId).toBe('tr_response')
    expect(requests[0]?.header.Authorization).toBe('Bearer token')
    expect(requests[0]?.url).toBe(`${apiBaseUrl}/api/booking/staff/v1/trips`)
    expect(requests[0]?.header['X-Trace-Id']).toMatch(/^tr_/)
    clearSession()
  })
  it('raises a field-level failure with the server trace ID', async () => {
    queued.push({ statusCode: 400, data: { code: 40001, message: 'Validation failed', errors: [{ field: 'pickupAt', message: 'Required' }], traceId: 'tr_failure' } })
    await expect(request('/api/booking/staff/v1/trips')).rejects.toMatchObject({ code: 40001, traceId: 'tr_failure', errors: [{ field: 'pickupAt', message: 'Required' }] })
  })
  it('refreshes an expired token once and retries the request', async () => {
    saveSession({ accessToken: 'old', refreshToken: 'refresh' })
    queued.push(
      { statusCode: 401, data: { code: 40101, message: 'Expired', errors: [], traceId: 'tr_1' } },
      { statusCode: 200, data: { code: 20000, message: 'success', data: { accessToken: 'new', refreshToken: 'next' }, traceId: 'tr_2' } },
      { statusCode: 200, data: { code: 20000, message: 'success', data: [], traceId: 'tr_3' } },
    )
    await request('/api/booking/staff/v1/trips')
    expect(requests.map((item) => item.url)).toEqual([`${apiBaseUrl}/api/booking/staff/v1/trips`, `${apiBaseUrl}/api/v1/refresh`, `${apiBaseUrl}/api/booking/staff/v1/trips`])
    expect(requests[2]?.header.Authorization).toBe('Bearer new')
    clearSession()
  })
  it('preserves a server failure and clears invalid authentication', async () => {
    queued.push({ statusCode: 500, data: { code: 50000, message: 'Internal error', errors: [], traceId: 'tr_server' } })
    await expect(request('/api/booking/staff/v1/trips')).rejects.toMatchObject({ statusCode: 500, code: 50000, traceId: 'tr_server' })
    saveSession({ accessToken: 'invalid' })
    queued.push({ statusCode: 401, data: { code: 40100, message: 'Unauthenticated', errors: [], traceId: 'tr_auth' } })
    await expect(request('/api/booking/staff/v1/trips')).rejects.toMatchObject({ statusCode: 401, code: 40100 })
    expect(getSession()).toBeNull()
    expect(uni.reLaunch).toHaveBeenCalledWith(expect.objectContaining({ url: '/pages/login/index' }))
  })
})
