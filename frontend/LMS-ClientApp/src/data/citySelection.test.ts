import { describe, expect, it } from 'vitest'
import { resolveCitySelection } from './citySelection'

describe('resolveCitySelection', () => {
  it('selects a city that is available', () => {
    expect(resolveCitySelection(['Macau', 'Hong Kong'], 'Hong Kong', 'Macau')).toBe('Hong Kong')
  })

  it('keeps the current city when an unavailable value is requested', () => {
    expect(resolveCitySelection(['Macau', 'Hong Kong'], 'Shenzhen', 'Macau')).toBe('Macau')
  })
})
