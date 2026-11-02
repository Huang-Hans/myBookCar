import { beforeEach, describe, expect, it, vi } from 'vitest'
import { driverApi } from './driver'

interface MockRequestOptions {
  url: string
  method: string
  header: Record<string, string>
  success(response: { statusCode: number; data: unknown; header: Record<string, string> }): void
}

const request = vi.fn()
const reLaunch = vi.fn()
const storage = new Map<string, unknown>()

beforeEach(() => {
  request.mockReset()
  reLaunch.mockReset()
  storage.clear()
  vi.stubGlobal('uni', { request, reLaunch, getStorageSync: (key: string) => storage.get(key), removeStorageSync: (key: string) => storage.delete(key) })
})

describe('driver shifts HTTP contract', () => {
  it('requests a page of the driver’s shifts from the staff shift endpoint with the unified envelope', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 200,
      header: { 'x-trace-id': 'tr_shifts' },
      data: { code: 20000, message: 'success', traceId: 'tr_shifts', data: [{ shiftId: 'sh_1', businessDate: '2026-10-22', status: 'ON_DUTY' }], meta: { traceId: 'tr_shifts', pagination: { currentPage: 2, pageSize: 50, totalItems: 53, totalPages: 2 } } },
    }))

    const response = await driverApi.listShiftsForDriver('driver_01a123d9b78a79fcb6b4193753ec6a90', 2)
    const options = request.mock.calls[0]?.[0] as MockRequestOptions
    const url = new URL(options.url, 'http://localhost')
    expect(url.pathname).toBe('/api/dispatch/staff/v1/drivers/driver_01a123d9b78a79fcb6b4193753ec6a90/shifts')
    expect(url.searchParams.get('page')).toBe('2')
    expect(url.searchParams.get('pageSize')).toBe('50')
    expect(url.searchParams.get('sort')).toBe('-businessDate,-shiftId')
    expect(options.method).toBe('GET')
    expect(response.data[0]?.shiftId).toBe('sh_1')
    expect(response.meta?.pagination?.totalPages).toBe(2)
  })

  it('surfaces a forbidden response with its trace ID', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 403,
      header: { 'x-trace-id': 'tr_shift_denied' },
      data: { code: 40300, message: 'Insufficient permission', traceId: 'tr_shift_denied', errors: [] },
    }))
    await expect(driverApi.listShiftsForDriver('driver_01')).rejects.toMatchObject({ code: 40300, traceId: 'tr_shift_denied' })
  })

  it('does not sign the driver out when the optional shifts query is unavailable', async () => {
    storage.set('lmsDriverSession', { accessToken: 'driver_access', refreshToken: 'driver_refresh', principalType: 'DRIVER' })
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 401,
      header: { 'x-trace-id': 'tr_shift_unauthorized' },
      data: { code: 40100, message: 'Not authenticated for this route', traceId: 'tr_shift_unauthorized', errors: [] },
    }))

    await expect(driverApi.listShiftsForDriver('driver_01')).rejects.toMatchObject({ code: 40100, traceId: 'tr_shift_unauthorized' })
    expect(storage.has('lmsDriverSession')).toBe(true)
    expect(reLaunch).not.toHaveBeenCalled()
    expect(request).toHaveBeenCalledTimes(1)
  })
})
