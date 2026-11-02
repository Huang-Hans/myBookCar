import { describe, expect, it } from 'vitest'
import detailsPage from './index.vue?raw'

describe('order details presentation', () => {
  it('hides the popular routes section', () => {
    expect(detailsPage).not.toContain('topRoutes')
    expect(detailsPage).not.toContain('$t("Top Routes")')
  })

  it('shows distance and pickup ETA without the speed metric', () => {
    expect(detailsPage).toContain('$t("km Covered")')
    expect(detailsPage).toContain('$t("km to Pickup")')
    expect(detailsPage).toContain('$t(\'ETA to Pickup\')')
    expect(detailsPage).not.toContain("$t('km/h')")
    expect(detailsPage).not.toContain('mapMetrics.speedKmh')
  })

  it('keeps key order facts visible and collapses remaining details by default', () => {
    expect(detailsPage).toContain('const isOrderDetailsExpanded = ref(false)')
    expect(detailsPage).toContain('{{ $t("Order No.") }}')
    expect(detailsPage).toContain('{{ $t("Status") }}')
    expect(detailsPage).toContain('{{ $t("Pickup") }}')
    expect(detailsPage).toContain('{{ $t("Drop-Off") }}')
    expect(detailsPage).toContain('v-if="isOrderDetailsExpanded"')
    expect(detailsPage).toContain("isOrderDetailsExpanded ? 'Hide Full Details' : 'Show Full Details'")
    expect(detailsPage).toContain('src="/static/icons/chevron-down.svg"')
    expect(detailsPage).toContain('color:#eaa42b')
    expect(detailsPage).not.toContain("? '⌃' : '⌄'")
  })
})
