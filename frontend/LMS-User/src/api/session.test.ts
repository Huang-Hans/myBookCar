import { afterEach, describe, expect, it, vi } from 'vitest'
import { isAccessTokenExpiring } from './session'

function token(exp: number): string {
  return `header.${btoa(JSON.stringify({ exp }))}.signature`
}

describe('staff notification token expiry', () => {
  afterEach(() => { vi.unstubAllGlobals(); sessionStorage.removeItem('lmsStaffSession') })

  it('refreshes a token before its expiry reaches the negotiation request', () => {
    const now = Date.UTC(2026, 9, 19, 7)
    expect(isAccessTokenExpiring(token(now / 1000 + 30), now)).toBe(true)
    expect(isAccessTokenExpiring(token(now / 1000 + 120), now)).toBe(false)
  })

  it('does not treat an opaque or malformed token as locally expired', () => {
    expect(isAccessTokenExpiring('opaque-token')).toBe(false)
    expect(isAccessTokenExpiring('a.invalid.b')).toBe(false)
  })

  it('shares one refresh for concurrent notification requests with an expiring token', async () => {
    sessionStorage.setItem('lmsStaffSession', JSON.stringify({ accessToken: token(Math.floor(Date.now() / 1000) + 30), refreshToken: 'refresh-token' }))
    const fetchImpl = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      code: 20000,
      message: 'success',
      data: { accessToken: 'new-access-token', refreshToken: 'new-refresh-token' },
      traceId: 'tr_refresh',
    }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchImpl)
    vi.resetModules()
    const session = await import('./session')

    await expect(Promise.all([session.getStaffHubAccessToken(), session.getStaffHubAccessToken()]))
      .resolves.toEqual(['new-access-token', 'new-access-token'])
    expect(fetchImpl).toHaveBeenCalledOnce()
    expect(fetchImpl.mock.calls[0]?.[0]).toContain('/api/v1/refresh')
  })
})
