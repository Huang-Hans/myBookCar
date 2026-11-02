import { describe, expect, it } from 'vitest'
import { formatDateTime, isActiveTrip, statusLabel, toUtc } from './presentation'
import type { Trip } from './staff'
import { setLocale } from '../i18n'

describe('staff presentation', () => {
  it('converts Macau local booking time to UTC', () => {
    expect(toUtc('2026-10-16', '14:30')).toBe('2026-10-16T06:30:00Z')
    expect(toUtc('', '')).toBeNull()
  })
  it('formats trip status and active state', () => {
    expect(statusLabel('DRIVER_EN_ROUTE')).toBe('司機前往中')
    expect(formatDateTime('invalid')).toBe('—')
    expect(isActiveTrip({ status: 'CONFIRMED' } as Trip)).toBe(true)
    expect(isActiveTrip({ status: 'COMPLETED' } as Trip)).toBe(false)
  })
  it('uses the selected language for trip status', () => {
    setLocale('en')
    expect(statusLabel('DRIVER_EN_ROUTE')).toBe('Driver en route')
    setLocale('zh-Hant')
  })
})
