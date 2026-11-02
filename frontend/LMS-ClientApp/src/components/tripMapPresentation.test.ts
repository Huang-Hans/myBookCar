import { describe, expect, it } from 'vitest'
import { TRIP_MAP_ROUTE_COLOR, tripStopMarkerContent, tripVehicleMarkerContent } from '../../../shared/tripMap/presentation'
import mapSource from './AmapRouteMap.vue?raw'
import orderDetailsSource from '../pages/order-details/index.vue?raw'

describe('shared trip map presentation', () => {
  it('uses the staff tracking colors and A/B markers', () => {
    expect(TRIP_MAP_ROUTE_COLOR).toBe('#16A34A')
    expect(tripStopMarkerContent('pickup')).toContain('track-stop-pickup">A</span>')
    expect(tripStopMarkerContent('dropoff')).toContain('track-stop-dropoff">B</span>')
  })

  it('renders a vehicle with a safe heading value', () => {
    expect(tripVehicleMarkerContent(450)).toContain('rotate(90deg)')
    expect(tripVehicleMarkerContent(Number.NaN)).not.toContain('rotate(')
    expect(tripVehicleMarkerContent()).toContain('track-vehicle')
  })

  it('applies the shared markers to the customer AMap component', () => {
    expect(mapSource).toContain("tripStopMarkerContent('pickup')")
    expect(mapSource).toContain('tripVehicleMarkerContent(headingDegrees)')
    expect(mapSource).not.toContain("mapStyle: 'amap://styles/whitesmoke'")
  })

  it('hides the driver address row until a GPS position is available', () => {
    expect(mapSource).toContain('<button v-if="hasCurrentPosition" class="address-row"')
    expect(mapSource).not.toContain('|| !hasCurrentPosition')
  })

  it('shows the trip stop names instead of map coordinates in order details', () => {
    expect(orderDetailsSource).toContain(':pickup-name="booking.pickup"')
    expect(orderDetailsSource).toContain(':dropoff-name="booking.destination"')
    expect(mapSource).toContain('props.pickupName?.trim() || positionLabel(')
    expect(mapSource).toContain('props.dropoffName?.trim() || positionLabel(')
  })
})
