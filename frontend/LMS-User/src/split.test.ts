import { describe, expect, it } from 'vitest'
import router from './router'

describe('Customer Service application boundary', () => {
  it('registers Console routes but not Portal routes', () => {
    const paths = router.getRoutes().map((route) => route.path)
    expect(paths).toContain('/console/:section')
    expect(paths).not.toContain('/portal/:section')
    expect(router.resolve('/console/tracking').matched.some((route) => route.path === '/console/:section')).toBe(true)
  })
})
