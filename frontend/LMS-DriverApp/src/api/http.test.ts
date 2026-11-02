import { describe, expect, it } from 'vitest'
import { buildQuery, createIdempotencyKey } from './http'
import { availabilityToCode, codeToAvailability } from './driver'
import { resolveApiBaseUrl } from './baseUrl'

describe('driver API helpers', () => {
  it('uses the same-origin proxy on H5 and the HTTPS gateway by default for native builds', () => {
    expect(resolveApiBaseUrl('h5')).toBe('')
    expect(resolveApiBaseUrl('h5', 'file:')).toBe('https://melco1.weprogram.site')
    expect(resolveApiBaseUrl('app')).toBe('https://melco1.weprogram.site')
    expect(resolveApiBaseUrl('app-harmony')).toBe('https://melco1.weprogram.site')
  })
  it('builds contract query strings', () => expect(buildQuery({ view: 'HISTORY', pageSize: 20, status: undefined })).toBe('?view=HISTORY&pageSize=20'))
  it('creates idempotency keys', () => expect(createIdempotencyKey('progress')).toMatch(/^progress_\d+_[a-z0-9]+$/))
  it('maps availability labels', () => { expect(availabilityToCode('On Break')).toBe('BREAK'); expect(codeToAvailability('OFF_DUTY')).toBe('Off Duty') })
})
