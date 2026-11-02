import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import planRideSource from './index.vue?raw'

describe('trip location selection', () => {
  it('opens the address picker instead of allowing direct pickup or drop-off editing', () => {
    const page = parse(planRideSource).descriptor
    const template = page.template?.content ?? ''
    const script = page.scriptSetup?.content ?? ''
    const styles = page.styles.map((style) => style.content).join('\n')

    expect(template).not.toMatch(/<input[^>]*v-model="(?:pickup|dropoff)"/)
    expect(template).toMatch(/class="location-value"[^>]*@tap="openLocationSheet\('pickup'\)"/)
    expect(template).toMatch(/class="location-value"[^>]*@tap="openLocationSheet\('dropoff'\)"/)
    expect(template).toContain(':search-placeholder="$t(\'Search locations\')"')
    expect(template).toContain(':is-confirm-disabled="!locationNames.includes(pendingLocation)"')
    expect(script).toContain('const item = locations.value.find((location) => location.name === pendingLocation.value)')
    expect(script).toContain('pickupLocationId.value = item.locationId')
    expect(script).toContain('dropoffLocationId.value = item.locationId')
    expect(styles).toMatch(/\.location-value\s*\{[^}]*display:flex;[^}]*justify-content:flex-start;[^}]*text-align:left;/)
  })
})
