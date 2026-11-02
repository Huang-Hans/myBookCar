import { describe, expect, it } from 'vitest'
import detailsPage from './index.vue?raw'

describe('order details status tracker', () => {
  it('renders the fixed trip-stage progression instead of repeated timeline statuses', () => {
    expect(detailsPage).toContain('customerTripStages')
    expect(detailsPage).toContain('currentStageIndex')
    expect(detailsPage).toContain("{{ $t(stage) }}")
    // 时间线里同一状态可能重复出现（多个 In Progress），不再逐条渲染。
    expect(detailsPage).not.toContain('trip?.timeline')
  })

  it('keeps a single status line for cancelled trips', () => {
    expect(detailsPage).toContain("trip.summary.status !== 'CANCELLED'")
    expect(detailsPage).toContain("$t(customerTripStatusLabel(trip.summary.status))")
  })

  it('shows each stage time node from the trip timeline', () => {
    expect(detailsPage).toContain('customerStageTimes')
    expect(detailsPage).toContain('trip-stage-time')
    expect(detailsPage).toContain('formatStageTime(stageTimes[index]!)')
  })
})
