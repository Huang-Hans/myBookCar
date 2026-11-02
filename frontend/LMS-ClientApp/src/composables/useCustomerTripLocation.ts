import { ref } from 'vue'
import { watchCustomerTripLocation } from '@/api/notifications'
import type { TripLocation } from '../../../shared/tripLocations/contracts'
import type { TripLocationSubscription } from '../../../shared/tripLocations/subscription'

export function useCustomerTripLocation() {
  const driverLocation = ref<TripLocation | null>(null)
  const isSharing = ref(false)
  let tripId = ''
  let subscription: TripLocationSubscription | null = null

  function stopWatching(): void {
    subscription?.dispose()
    subscription = null
    tripId = ''
    driverLocation.value = null
    isSharing.value = false
  }

  function startWatching(nextTripId: string): void {
    if (subscription && tripId === nextTripId) return
    stopWatching()
    if (!nextTripId) return
    tripId = nextTripId
    subscription = watchCustomerTripLocation(nextTripId, (state) => {
      if (state.tripId !== tripId) return
      isSharing.value = state.isSharing
      driverLocation.value = state.isSharing ? state.location : null
    })
  }

  return { driverLocation, isSharing, startWatching, stopWatching }
}
