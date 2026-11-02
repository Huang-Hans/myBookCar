import { describe, expect, it } from 'vitest'
import tripDetailPage from './index.vue?raw'
import dispatchSheet from '../../components/TripDispatchSheet.vue?raw'

describe('trip detail dispatch entry', () => {
  it('only offers dispatch from a confirmed, unassigned trip', () => {
    expect(tripDetailPage).toContain("trip.value?.status === 'CONFIRMED'")
    expect(tripDetailPage).toContain('if (!canOpenDispatch.value || !trip.value) return')
    expect(tripDetailPage).toContain('v-if="canOpenDispatch" class="dispatch-link"')
  })

  it('opens an in-page dispatch sheet for the matching trip', () => {
    expect(tripDetailPage).toContain('@tap="openDispatch"')
    expect(tripDetailPage).toContain('isDispatchSheetOpen.value = true')
    expect(tripDetailPage).toContain('<TripDispatchSheet v-if="isDispatchSheetOpen && trip" :trip-id="trip.tripId"')
    expect(tripDetailPage).not.toContain('/pages/dispatch/index?tripId=')
  })

  it('keeps the AMap attribution inside the map and below the dispatch sheet', () => {
    expect(tripDetailPage).toContain('.map-card { position:relative; z-index:0; isolation:isolate;')
    expect(dispatchSheet).toContain('.dispatch-overlay { position:fixed; z-index:20;')
    expect(dispatchSheet).toContain('class="dispatch-sheet"')
  })

  it('loads the authoritative dispatch order and on-duty drivers before inviting', () => {
    expect(dispatchSheet).toContain('staffApi.dispatchOrder(props.tripId)')
    expect(dispatchSheet).toContain('if (detail.data.canDispatch)')
    expect(dispatchSheet).toContain('staffApi.shiftBoard(detail.data.pickupAt, detail.data.propertyId)')
    expect(dispatchSheet).toContain('shiftBoardCandidates(board.data, detail.data.propertyId, detail.data.pickupAt, detail.data.vehicleClass)')
    expect(dispatchSheet).toContain('staffApi.offerDriver(current.tripId, driver.driverId, current.version, key)')
    expect(dispatchSheet).toContain('emit(\'offered\')')
  })
})
