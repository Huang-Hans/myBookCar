import { describe, expect, it } from 'vitest'
import { availableDepartureHours, availableDepartureMinutes, buildDepartureDateOptions, isFutureDeparture, localDateValue, nextSelectableDeparture, timeParts } from './departurePicker'

describe('departure picker data', () => {
  it('builds consecutive local calendar dates with Figma labels', () => {
    expect(buildDepartureDateOptions(new Date(2026, 9, 13, 12), 3)).toEqual([
      { value: '2026-10-13', label: 'Tue, Oct 13' },
      { value: '2026-10-14', label: 'Wed, Oct 14' },
      { value: '2026-10-15', label: 'Thu, Oct 15' },
    ])
  })

  it('formats local dates without UTC day shifts', () => {
    expect(localDateValue(new Date(2026, 0, 2, 0, 30))).toBe('2026-01-02')
  })

  it('parses valid times and safely bounds invalid values', () => {
    expect(timeParts('11:05')).toEqual([11, 5])
    expect(timeParts('29:88')).toEqual([23, 59])
    expect(timeParts('invalid')).toEqual([0, 0])
  })

  it('rejects an empty date range', () => {
    expect(() => buildDepartureDateOptions(new Date(2026, 9, 13), 0)).toThrow('dayCount must be a positive integer.')
  })

  it('starts at the next selectable minute and rolls over midnight', () => {
    expect(nextSelectableDeparture(new Date(2026, 9, 22, 10, 15, 23))).toEqual({ date: '2026-10-22', time: '10:16' })
    expect(nextSelectableDeparture(new Date(2026, 9, 22, 23, 59, 59))).toEqual({ date: '2026-10-23', time: '00:00' })
  })

  it('does not offer past hours or minutes', () => {
    const now = new Date(2026, 9, 22, 10, 15, 23)
    expect(availableDepartureHours('2026-10-21', now)).toEqual([])
    expect(availableDepartureHours('2026-10-22', now)[0]).toBe('10')
    expect(availableDepartureMinutes('2026-10-22', '10', now)[0]).toBe('16')
    expect(availableDepartureMinutes('2026-10-22', '11', now)[0]).toBe('00')
    expect(availableDepartureHours('2026-10-23', now)[0]).toBe('00')
  })

  it('rejects past, current and malformed departures', () => {
    const now = new Date(2026, 9, 22, 10, 15, 23)
    expect(isFutureDeparture('2026-10-22', '10:15', now)).toBe(false)
    expect(isFutureDeparture('2026-10-22', '10:16', now)).toBe(true)
    expect(isFutureDeparture('2026-02-30', '10:16', now)).toBe(false)
    expect(isFutureDeparture('2026-10-22', '24:00', now)).toBe(false)
  })
})
