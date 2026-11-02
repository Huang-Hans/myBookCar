import { describe, expect, it } from 'vitest'
import { macauDateBoundary } from './dates'

describe('Macau date filters', () => {
  it('sends inclusive start and exclusive next-day end as UTC', () => {
    expect(macauDateBoundary('2026-10-12', false)).toBe('2026-10-11T16:00:00Z')
    expect(macauDateBoundary('2026-10-12', true)).toBe('2026-10-12T16:00:00Z')
  })
})
