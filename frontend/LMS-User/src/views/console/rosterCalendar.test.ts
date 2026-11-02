import { describe, expect, it } from 'vitest'
import { mondayOffset, monthDates, shiftMonth } from './rosterCalendar'

describe('monthly roster dates', () => {
  it('navigates across years and includes historical and future dates', () => {
    expect(shiftMonth('2026-01', -1)).toBe('2025-12')
    expect(shiftMonth('2026-12', 1)).toBe('2027-01')
    expect(monthDates('2026-10')).toHaveLength(31)
    expect(monthDates('2026-10').at(-1)).toBe('2026-10-31')
  })

  it('places dates under the correct Monday-first weekday', () => {
    expect(mondayOffset('2026-10-01')).toBe(3)
    expect(mondayOffset('2026-11-01')).toBe(6)
    expect(monthDates('2028-02')).toHaveLength(29)
  })
})
