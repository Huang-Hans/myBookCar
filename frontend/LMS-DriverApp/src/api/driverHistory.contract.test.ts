import { beforeEach, describe, expect, it, vi } from 'vitest'
import { driverApi } from './driver'

interface MockRequestOptions {
  url: string
  success(response: { statusCode: number; data: unknown; header: Record<string, string> }): void
}

const request = vi.fn()

beforeEach(() => {
  request.mockReset()
  vi.stubGlobal('uni', { request, getStorageSync: () => null })
})

describe('driver history API contract', () => {
  it('requests the selected history status and page from the backend', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 200,
      header: { 'x-trace-id': 'tr_history' },
      data: { code: 20000, message: 'success', traceId: 'tr_history', data: [], meta: { traceId: 'tr_history', pagination: { currentPage: 2, pageSize: 20, totalItems: 24, totalPages: 2 } } },
    }))

    const response = await driverApi.listTrips('HISTORY', 'COMPLETED', 2)
    const options = request.mock.calls[0]?.[0] as MockRequestOptions

    expect(options.url).toContain('/api/booking/driver/v1/me/trips?')
    expect(new URL(options.url, 'http://localhost').searchParams.get('view')).toBe('HISTORY')
    expect(new URL(options.url, 'http://localhost').searchParams.get('status')).toBe('COMPLETED')
    expect(new URL(options.url, 'http://localhost').searchParams.get('page')).toBe('2')
    expect(response.meta?.pagination?.totalPages).toBe(2)
  })

  it('requests all terminal trips without a status filter', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 200,
      header: { 'x-trace-id': 'tr_all_history' },
      data: { code: 20000, message: 'success', traceId: 'tr_all_history', data: [], meta: { traceId: 'tr_all_history', pagination: { currentPage: 1, pageSize: 20, totalItems: 0, totalPages: 0 } } },
    }))

    await driverApi.listTrips('HISTORY')
    const options = request.mock.calls[0]?.[0] as MockRequestOptions
    const query = new URL(options.url, 'http://localhost').searchParams
    expect(query.get('view')).toBe('HISTORY')
    expect(query.has('status')).toBe(false)
    expect(query.get('page')).toBe('1')
  })
})
