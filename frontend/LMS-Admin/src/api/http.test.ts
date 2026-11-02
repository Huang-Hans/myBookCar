import { describe, expect, it, vi } from 'vitest'
import { ApiRequestError, createApiClient } from './http'
import type { ServiceBannerResponse } from './http'
import { apiBaseUrl } from './baseUrl'

function jsonResponse(status: number, value: unknown, headers?: HeadersInit): Response {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json', ...headers } })
}

describe('API client', () => {
  it('uses the same-origin gateway proxy', () => {
    expect(apiBaseUrl).toBe('')
  })
  it('returns success data and records trace ID', async () => {
    const logTrace = vi.fn()
    const fetchImpl = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(200, { code: 20000, message: 'success', data: { serviceName: 'LMS.ApiGateway', description: 'LMS API Gateway' }, traceId: 'tr_123' }))
    const client = createApiClient({ baseUrl: '/api', fetchImpl, logTrace })
    await expect(client.request<ServiceBannerResponse>('/')).resolves.toEqual({ serviceName: 'LMS.ApiGateway', description: 'LMS API Gateway' })
    expect(logTrace).toHaveBeenCalledWith('tr_123', 20000)
  })
  it('retains pagination metadata for server-paged lists', async () => {
    const response = jsonResponse(200, { code: 20000, message: 'success', data: [{ driverId: 'drv_1' }], meta: { traceId: 'tr_page', pagination: { currentPage: 2, pageSize: 10, totalItems: 23, totalPages: 3 } }, traceId: 'tr_page' })
    const client = createApiClient({ baseUrl: '', fetchImpl: vi.fn<typeof fetch>().mockResolvedValue(response) })
    await expect(client.requestEnvelope<{ driverId: string }[]>('/api/dispatch/staff/v1/drivers?page=2&pageSize=10')).resolves.toMatchObject({ data: [{ driverId: 'drv_1' }], meta: { pagination: { totalItems: 23 } } })
  })
  it('refreshes once after an expired token', async () => {
    const fetchImpl = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse(401, { code: 40101, message: 'Token expired', errors: [], traceId: 'tr_old' }))
      .mockResolvedValueOnce(jsonResponse(200, { code: 20000, message: 'success', data: { serviceName: 'Gateway', description: 'Ready' }, traceId: 'tr_new' }))
    const refreshToken = vi.fn().mockResolvedValue(true)
    const client = createApiClient({ baseUrl: '', fetchImpl, refreshToken })
    await expect(client.request<ServiceBannerResponse>('/')).resolves.toEqual({ serviceName: 'Gateway', description: 'Ready' })
    expect(refreshToken).toHaveBeenCalledOnce()
    expect(fetchImpl).toHaveBeenCalledTimes(2)
  })
  it('surfaces field errors and trace ID', async () => {
    const onError = vi.fn()
    const fetchImpl = vi.fn<typeof fetch>().mockResolvedValue(jsonResponse(400, { code: 40001, message: 'Validation failed', errors: [{ field: 'emailAddress', message: 'Invalid email format' }], traceId: 'tr_invalid' }))
    const client = createApiClient({ baseUrl: '', fetchImpl, onError })
    await expect(client.request('/')).rejects.toMatchObject({ status: 400, code: 40001, traceId: 'tr_invalid', errors: [{ field: 'emailAddress', message: 'Invalid email format' }] } satisfies Partial<ApiRequestError>)
    expect(onError).toHaveBeenCalledOnce()
  })
  it('honors Retry-After and retries a rate-limited request once', async () => {
    const fetchImpl = vi.fn<typeof fetch>()
      .mockResolvedValueOnce(jsonResponse(429, { code: 42900, message: 'Too many requests', errors: [], traceId: 'tr_limited' }, { 'Retry-After': '2' }))
      .mockResolvedValueOnce(jsonResponse(200, { code: 20000, message: 'success', data: 1, traceId: 'tr_ok' }))
    const sleep = vi.fn().mockResolvedValue(undefined)
    const client = createApiClient({ baseUrl: '', fetchImpl, sleep })
    await expect(client.request<number>('/')).resolves.toBe(1)
    expect(sleep).toHaveBeenCalledWith(2000)
  })
})
