import { describe, expect, it } from 'vitest'
import dispatchPage from './index.vue?raw'

describe('dispatch order entrance animation', () => {
  it('reveals order cards upward in list order after an interactive load', () => {
    expect(dispatchPage).toContain('if (animate) listAnimationCycle.value += 1')
    expect(dispatchPage).toContain('v-for="(order, index) in orders"')
    expect(dispatchPage).toContain(':key="`${listAnimationCycle}-${order.tripId}`"')
    expect(dispatchPage).toContain('animationDelay: `${index * 65}ms`')
    expect(dispatchPage).toContain('animation:order-rise 480ms')
    expect(dispatchPage).toContain('transform:translateY(26px)')
  })

  it('does not replay animations during background polling and respects reduced motion', () => {
    expect(dispatchPage).toContain('if (!selected.value) void load(false)')
    expect(dispatchPage).toContain('@media (prefers-reduced-motion:reduce)')
    expect(dispatchPage).toContain('.order { animation:none; }')
  })

  it('uses Dispatch API totals and reads on-duty drivers from the shift board', () => {
    expect(dispatchPage).toContain('staffApi.dispatchOrderSummaries()')
    expect(dispatchPage).toContain('summary.value.counts?.PENDING')
    expect(dispatchPage).toContain('staffApi.dispatchOrder(order.tripId)')
    expect(dispatchPage).toContain('staffApi.dispatchOffers(order.tripId)')
    expect(dispatchPage).toContain('staffApi.shiftBoard(detail.data.pickupAt, detail.data.propertyId)')
    expect(dispatchPage).toContain('shiftBoardCandidates(board.data, detail.data.propertyId, detail.data.pickupAt, detail.data.vehicleClass)')
    expect(dispatchPage).toContain('v-else-if="candidatesLoading"')
    expect(dispatchPage).toContain('candidateError.value = displayError(cause)')
  })
})
