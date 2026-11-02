import { describe, expect, it } from 'vitest'

import { buildLoginRoute, resolvePostLoginRoute } from './authNavigation'

describe('customer authentication navigation', () => {
  it('preserves a protected page and its query for sign-in', () => {
    const loginRoute = buildLoginRoute('/pages/plan-ride/index?service=limousine')
    expect(loginRoute).toBe('/pages/login/index?redirect=%2Fpages%2Fplan-ride%2Findex%3Fservice%3Dlimousine')
    expect(resolvePostLoginRoute(loginRoute.split('redirect=')[1])).toBe('/pages/plan-ride/index?service=limousine')
  })

  it('rejects external and malformed redirects', () => {
    expect(resolvePostLoginRoute('https://example.com')).toBe('/pages/home/index')
    expect(resolvePostLoginRoute('//example.com/pages/home/index')).toBe('/pages/home/index')
    expect(resolvePostLoginRoute('%E0%A4%A')).toBe('/pages/home/index')
  })

  it('does not redirect from login back to login', () => {
    expect(resolvePostLoginRoute('/pages/login/index')).toBe('/pages/home/index')
  })
})
