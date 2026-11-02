import { describe, expect, it } from 'vitest'
import { availableDepartureHours, availableDepartureMinutes, departureDateOptions, isFutureMacauDeparture, nextMacauDeparture } from './bookingDeparture'

describe('Macau booking departure wheel', () => {
  it('starts at the next available minute in Macau even across midnight', () => {
    expect(nextMacauDeparture(new Date('2026-10-22T02:15:23Z'))).toEqual({ date: '2026-10-22', time: '10:16' })
    expect(nextMacauDeparture(new Date('2026-10-22T15:59:59Z'))).toEqual({ date: '2026-10-23', time: '00:00' })
  })

  it('offers one wheel with consecutive dates and only future hours and minutes', () => {
    const now = new Date('2026-10-22T02:15:23Z')
    expect(departureDateOptions('2026-10-22', 'en', 2).map((option) => option.value)).toEqual(['2026-10-22', '2026-10-23'])
    expect(availableDepartureHours('2026-10-22', now)[0]).toBe('10')
    expect(availableDepartureMinutes('2026-10-22', '10', now)[0]).toBe('16')
    expect(availableDepartureMinutes('2026-10-22', '11', now)[0]).toBe('00')
  })

  it('rejects past, current and malformed local departures', () => {
    const now = new Date('2026-10-22T02:15:23Z')
    expect(isFutureMacauDeparture('2026-10-22', '10:15', now)).toBe(false)
    expect(isFutureMacauDeparture('2026-10-22', '10:16', now)).toBe(true)
    expect(isFutureMacauDeparture('2026-02-30', '10:16', now)).toBe(false)
    expect(isFutureMacauDeparture('2026-10-22', '24:00', now)).toBe(false)
  })
})
