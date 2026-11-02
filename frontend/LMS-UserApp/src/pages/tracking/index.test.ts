import { describe, expect, it } from 'vitest'
import trackingPage from './index.vue?raw'

describe('trip tracking progress', () => {
  it('fills from zero to the current status on load and smooths later width changes', () => {
    expect(trackingPage).toContain("? '20%' : ['DRIVER_ASSIGNED','DRIVER_EN_ROUTE'].includes(trip.status) ? '50%' : '80%'")
    expect(trackingPage).toContain('transform-origin:left center')
    expect(trackingPage).toContain('animation:progress-fill 900ms')
    expect(trackingPage).toContain('from { transform:scaleX(0); } to { transform:scaleX(1); }')
    expect(trackingPage).toContain('transition:width 550ms ease')
    expect(trackingPage).toContain('@media (prefers-reduced-motion:reduce)')
  })
})
