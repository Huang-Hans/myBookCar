import { describe, expect, it } from 'vitest'
import { bookingEmptyTitle } from './bookingEmptyState'

describe('bookingEmptyTitle', () => {
  it.each([
    ['In Progress', 'No In Progress Journeys'],
    ['Completed', 'No Completed Journeys'],
    ['Cancelled', 'No Cancelled Journeys'],
  ] as const)('shows the %s empty-state title', (filter, title) => {
    expect(bookingEmptyTitle(filter)).toBe(title)
  })
})
