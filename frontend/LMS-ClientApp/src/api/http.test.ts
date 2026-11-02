import { describe, expect, it } from 'vitest'
import { buildQuery, createIdempotencyKey } from './http'
import { productCodeToServiceId, serviceIdToProductCode } from './customer'
import { apiBaseUrl, resolveApiBaseUrl } from './baseUrl'

describe('customer API helpers', () => {
  it('uses the same-origin proxy on H5 and the Melco HTTPS gateway on native builds', () => {
    expect(apiBaseUrl).toBe('')
    expect(resolveApiBaseUrl('h5')).toBe('')
    expect(resolveApiBaseUrl('h5', 'file:')).toBe('https://melco1.weprogram.site')
    expect(resolveApiBaseUrl('app')).toBe('https://melco1.weprogram.site')
  })
  it('encodes camelCase query parameters and omits undefined values', () => {
    expect(buildQuery({ page: 1, pageSize: 20, search: 'Macau Airport', region: undefined }))
      .toBe('?page=1&pageSize=20&search=Macau%20Airport')
  })

  it('creates distinct idempotency keys', () => {
    expect(createIdempotencyKey('trip')).toMatch(/^trip_\d+_[a-z0-9]+$/)
  })

  it('maps presentation service ids to contract product codes', () => {
    expect(serviceIdToProductCode('cross-border')).toBe('CROSS_BORDER_TRANSFER')
    expect(productCodeToServiceId('PARTY')).toBe('party')
  })
})
