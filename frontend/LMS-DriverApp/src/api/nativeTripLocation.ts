import { driverLocationReporter, readTripLocationSequence, writeTripLocationSequence } from './driverLocations'
import { driverNativeBridge, nativeGatewayOrigin, nativeTripLocationState } from './nativeBridge'

// Trip the native service stopped with 40400 / 40900; not restarted until the page reports another trip.
let endedTripId: string | null = null

/**
 * Hands trip reporting to the APK shell's foreground service, which keeps reporting in the background.
 * Returns true when native code owns reporting for this state, so the page reporter must stay idle;
 * false in a browser or while location permission is still missing (the page reporter then runs).
 */
export function synchronizeNativeTripLocation(sharingTripId: string | null): boolean {
  const bridge = driverNativeBridge()
  if (!bridge) return false
  if (!sharingTripId) {
    driverLocationReporter.endTrip()
    stopNativeTripLocation()
    return true
  }
  if (sharingTripId === endedTripId) return true
  const state = nativeTripLocationState(bridge)
  if (state.isRunning && state.tripId === sharingTripId) return true
  if (state.tripId !== sharingTripId) stopNativeTripLocation()
  // The page reporter may have run as a fallback; its sequence is persisted, so the native side continues it.
  driverLocationReporter.endTrip()
  return bridge.startTripLocation(sharingTripId, readTripLocationSequence(sharingTripId), nativeGatewayOrigin())
}

/** Stops the native reporter (if any) and keeps its last sequence for the trip. */
export function stopNativeTripLocation(): void {
  const bridge = driverNativeBridge()
  if (!bridge) return
  const state = nativeTripLocationState(bridge)
  if (!state.tripId || (!state.isRunning && !state.isActive)) return
  rememberSequence(state.tripId, bridge.stopTripLocation(state.tripId))
}

/** Called when the native service stopped on its own. */
export function recordNativeTripLocationStop(tripId: string, lastSeq: number, hasTripEnded: boolean): void {
  if (hasTripEnded) endedTripId = tripId
  rememberSequence(tripId, lastSeq)
}

function rememberSequence(tripId: string, lastSeq: number): void {
  if (tripId && lastSeq > readTripLocationSequence(tripId)) writeTripLocationSequence(tripId, lastSeq)
}
