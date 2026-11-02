import { describe, expect, it } from 'vitest'
import bookingsPage from './index.vue?raw'

describe('booking status filters', () => {
  it('honours validated filters passed from the home overview', () => {
    expect(bookingsPage).toContain('onLoad((query) => {')
    expect(bookingsPage).toContain('bookingFilterFromValue(query?.filter)')
    expect(bookingsPage).toContain('bookingFilterFromHash(typeof window')
    expect(bookingsPage).toContain('if (requested) filter.value = requested')
    expect(bookingsPage).toContain("status: filter.value === 'ALL' || filter.value === 'ACTIVE' ? undefined : filter.value")
  })

  it('uses connected segments with a sliding selected indicator', () => {
    expect(bookingsPage).toContain('gap:0')
    expect(bookingsPage).toContain('class="filter-indicator" :style="indicatorStyle"')
    expect(bookingsPage).toContain('translateX(${selectedFilterIndex.value * 100}%)')
    expect(bookingsPage).toContain('transition:transform 240ms ease')
    expect(bookingsPage).toContain(':aria-pressed="filter === item.key"')
  })

  it('keeps search, filters, and the create action together in a sticky toolbar', () => {
    const toolbar = bookingsPage.match(/<view class="sticky-actions">([\s\S]*?)\n    <\/view>/)?.[1]
    expect(toolbar).toContain('class="search"')
    expect(toolbar).toContain('class="filters"')
    expect(toolbar).toContain("t('＋ 新增')")
    expect(bookingsPage).toContain('.sticky-actions { position:sticky; z-index:9; top:0;')
  })

  it('reveals trip cards upward one by one after each successful load', () => {
    expect(bookingsPage).toContain('listAnimationCycle.value += 1')
    expect(bookingsPage).toContain(':key="`${listAnimationCycle}-${trip.tripId}`"')
    expect(bookingsPage).toContain('animationDelay: `${index * 65}ms`')
    expect(bookingsPage).toContain('animation:trip-rise 480ms')
    expect(bookingsPage).toContain('transform:translateY(26px)')
    expect(bookingsPage).toContain('@media (prefers-reduced-motion:reduce)')
  })
})
