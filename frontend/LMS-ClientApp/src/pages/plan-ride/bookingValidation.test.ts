import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import planRideSource from './index.vue?raw'

const page = parse(planRideSource).descriptor
const script = page.scriptSetup?.content ?? ''
const template = page.template?.content ?? ''

describe('plan ride booking validation', () => {
  it('selects catalog defaults by service without preselecting favorites', () => {
    expect(script).toContain('locations.value[0]')
    expect(script).toContain('defaultDropoffLocation(locations.value, pickupLocationId.value, service.value)')
    expect(script).toContain("region: 'HONG_KONG'")
    expect(script).toContain("const dropoff = ref('')")
    expect(script).not.toContain('loadFavoriteLocations')
    expect(template).toContain('<button v-if="pickupFavorite" class="location-chip"')
    expect(template).toContain('<button v-if="dropoffFavorite" class="location-chip"')
  })

  it('filters past departure picker options and checks again before proceeding', () => {
    expect(script).toContain('nextSelectableDeparture(new Date())')
    expect(script).toContain('availableDepartureHours(')
    expect(script).toContain('availableDepartureMinutes(')
    expect(script).toContain("schedule.value === 'Schedule' && !isFutureDeparture(date.value, time.value, new Date())")
  })

  it('preflights the route and shows API errors on this page before navigation', () => {
    const preflight = script.indexOf('await customerApi.getRideOptions({')
    const navigate = script.indexOf('uni.navigateTo({ url: `/pages/choose-ride/index?${query}` })')
    expect(preflight).toBeGreaterThan(0)
    expect(navigate).toBeGreaterThan(preflight)
    expect(script).toContain('error.message')
    expect(script).toContain('error.traceId')
    expect(template).toContain('<AppBottomSheet :open="dialog !== null"')
    expect(template).toContain(':disabled="isCheckingRoute"')
  })
})
