import { describe, expect, it } from 'vitest'
import detailsPage from './index.vue?raw'
import driverMapPage from '../driver-map/index.vue?raw'
import routeMap from '../../components/AmapRouteMap.vue?raw'
import hub from '../../api/notificationHub.ts?raw'

describe('customer driver GPS presentation', () => {
  it('subscribes through the existing customer notification connection', () => {
    expect(hub).toContain("this.connection.on('tripLocationUpdated'")
    expect(detailsPage).toContain('startWatching(trip.value.summary.tripId)')
    expect(detailsPage).toContain('onHide(() => { isPageActive = false; stopWatching() })')
    expect(detailsPage).toContain(':driver-location="driverLocation"')
    expect(driverMapPage).toContain(':driver-location="driverLocation"')
    expect(driverMapPage).toContain('focus-driver-on-load')
  })

  it('uses only the reported driver coordinates for the map marker', () => {
    expect(routeMap).toContain('watch(() => props.driverLocation, updateDriverLocation)')
    expect(routeMap).toContain('point.longitude, point.latitude')
    expect(routeMap).toContain('tripVehicleMarkerContent(headingDegrees)')
    expect(routeMap).toContain('focusDriver(isFirstFix)')
    expect(routeMap).toContain('focusRoute()')
    expect(routeMap).toContain('buildAmapDrivingUrl(props.apiKey, origin, destination)')
    expect(routeMap).toContain('renderAmapTileMap(origin, destination, [])')
    expect(routeMap).not.toContain('uni.getLocation(')
    expect(routeMap).not.toContain('navigator.geolocation')
  })

  it('uses a compact address panel in the trip preview while retaining the full map details', () => {
    expect(detailsPage).toContain('is-compact-preview is-interactive')
    expect(detailsPage).not.toContain(':show-address-panel="false"')
    expect(detailsPage).toContain('booking.pickup')
    expect(detailsPage).toContain('booking.destination')
    expect(driverMapPage).toContain('class="route"')
    expect(routeMap).toContain(':class="{ compact: isCompactPreview }"')
    expect(routeMap).toContain('.map-address-panel.compact{left:8px;bottom:8px;width:180px')
    expect(routeMap).toContain('props.isCompactPreview ? [24, 24, 24, 24]')
  })
})
