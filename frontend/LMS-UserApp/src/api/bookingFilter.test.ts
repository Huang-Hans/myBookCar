import { describe, expect, it } from 'vitest'
import { bookingFilterFromHash, bookingFilterFromValue } from './bookingFilter'

describe('booking page route filter', () => {
  it('reads a completed deep link before the page renders', () => {
    expect(bookingFilterFromHash('#/pages/bookings/index?filter=COMPLETED')).toBe('COMPLETED')
    expect(bookingFilterFromHash('#/pages/bookings/index?filter=ACTIVE')).toBe('ACTIVE')
  })

  it('ignores missing or unsupported filter values', () => {
    expect(bookingFilterFromHash('#/pages/bookings/index')).toBeNull()
    expect(bookingFilterFromHash('#/pages/bookings/index?filter=UNKNOWN')).toBeNull()
    expect(bookingFilterFromValue(['COMPLETED'])).toBeNull()
  })
})
