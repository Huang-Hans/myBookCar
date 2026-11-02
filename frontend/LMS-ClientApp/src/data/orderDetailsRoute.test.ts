import { describe, expect, it } from 'vitest'
import { orderDetailsRoute } from './orderDetailsRoute'

describe('order details navigation', () => {
  it('opens the requested trip directly', () => {
    expect(orderDetailsRoute('trip_001')).toBe('/pages/order-details/index?id=trip_001')
  })

  it('encodes trip IDs as a single query value', () => {
    expect(orderDetailsRoute('trip/001?status=active')).toBe('/pages/order-details/index?id=trip%2F001%3Fstatus%3Dactive')
  })
})
