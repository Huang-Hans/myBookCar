import { describe, expect, it } from 'vitest'
import router from './router'

describe('Admin application boundary', () => {
  it('registers Portal routes but not Customer Service routes', () => {
    const paths = router.getRoutes().map((route) => route.path)
    expect(paths).toContain('/portal/:section')
    expect(paths).not.toContain('/console/:section')
    expect(router.resolve('/portal/locations').matched.some((route) => route.path === '/portal/:section')).toBe(true)
  })
})
