import { describe, expect, it } from 'vitest'
import homePage from './index.vue?raw'

describe('active trip card', () => {
  it('shows the current order reference and customer details from the active trip', () => {
    expect(homePage).toContain("{{ $t('Order No.') }}</text><text class=\"active-info-value\">{{ activeTrip?.tripReference || '—' }}")
    expect(homePage).toContain("{{ $t('Customer Details') }}")
    expect(homePage).toContain("{{ activeTrip?.passenger.name || '—' }}")
    expect(homePage).toContain("{{ activeTrip?.passenger.phoneMasked || $t('Not available') }}")
  })

  it('does not show the decorative chevron in the active trip heading', () => {
    expect(homePage).not.toContain('active-chevron')
  })

  it('does not display the speed metric on the active trip map', () => {
    expect(homePage).not.toContain("$t('km/h')")
    expect(homePage).toContain("$t('ETA to Pickup')")
  })
})
