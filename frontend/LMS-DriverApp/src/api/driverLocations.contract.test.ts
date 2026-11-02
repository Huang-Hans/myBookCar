import { beforeEach, describe, expect, it, vi } from 'vitest'
import { driverApi } from './driver'

interface MockRequestOptions {
  url: string
  method: string
  data: unknown
  header: Record<string, string>
  success(response: { statusCode: number; data: unknown; header: Record<string, string> }): void
}

const request = vi.fn()
beforeEach(() => {
  request.mockReset()
  vi.stubGlobal('uni', {
    request,
    getStorageSync: () => ({ accessToken: 'driver-token', refreshToken: 'refresh-token' }),
  })
})

describe('driver location HTTP contract', () => {
  it('posts GCJ-02 points to the driver trip resource with bearer auth', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 200,
      header: { 'x-trace-id': 'tr_location' },
      data: { code: 20000, message: 'success', data: { acceptedSeq: 7 }, traceId: 'tr_location' },
    }))
    const point = { seq: 7, latitude: 22.198745, longitude: 113.543873, recordedAt: '2026-10-19T01:12:05.120Z' }
    const response = await driverApi.reportLocations('trip_1', [point])
    const options = request.mock.calls[0]?.[0] as MockRequestOptions
    expect(options.url).toMatch(/\/api\/booking\/driver\/v1\/me\/trips\/trip_1\/locations$/)
    expect(options.method).toBe('POST')
    expect(options.header.Authorization).toBe('Bearer driver-token')
    expect(options.header['Content-Type']).toBe('application/json; charset=utf-8')
    expect(options.data).toEqual({ coordinateSystem: 'GCJ02', points: [point] })
    expect(response.data.acceptedSeq).toBe(7)
    expect(response.traceId).toBe('tr_location')
  })

  it('preserves a non-sharing conflict code and traceId', async () => {
    request.mockImplementationOnce((options: MockRequestOptions) => options.success({
      statusCode: 409,
      header: { 'x-trace-id': 'tr_not_sharing' },
      data: { code: 40900, message: 'Trip is not sharing', errors: [], traceId: 'tr_not_sharing' },
    }))
    await expect(driverApi.reportLocations('trip_1', [{ seq: 1, latitude: 22.2, longitude: 113.5, recordedAt: '2026-10-19T01:12:05.120Z' }]))
      .rejects.toMatchObject({ statusCode: 409, code: 40900, traceId: 'tr_not_sharing' })
  })
})
