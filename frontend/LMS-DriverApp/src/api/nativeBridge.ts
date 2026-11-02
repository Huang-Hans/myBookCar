import { apiBaseUrl } from './baseUrl'

// window.LmsDriverNative is injected only by the Android shell (android/.../bridge/DriverNativeBridge.java).
// In a browser it is absent and every helper reports "unavailable", so H5 keeps its own behaviour.
export interface DriverNativeBridge {
  isAvailable(): boolean
  startTripLocation(tripId: string, lastSeq: number, gatewayOrigin: string): boolean
  stopTripLocation(tripId: string): number
  getTripLocationState(): string
  getSession(): string
  saveSession(sessionJson: string): void
  clearSession(): void
  refreshSession(gatewayOrigin: string, staleRefreshToken: string, callbackId: string): void
}

export interface NativeTripLocationState { tripId: string; lastSeq: number; isRunning: boolean; isActive: boolean }
export type NativeTripLocationStopReason = 'TRIP_ENDED' | 'SESSION_EXPIRED' | 'LOCATION_UNAVAILABLE'
export type DriverNativeEvent =
  | { type: 'TRIP_LOCATION_STOPPED'; tripId: string; lastSeq: number; reason: NativeTripLocationStopReason }
  | { type: 'LOCATION_PERMISSION_CHANGED' }
export interface NativeRefreshResult { ok: boolean; statusCode: number; code: number; message: string; traceId: string; session?: unknown }

interface NativeWindow {
  LmsDriverNative?: DriverNativeBridge
  __lmsDriverNativeCallback?: (callbackId: string, json: string) => void
  __lmsDriverNativeEvent?: (json: string) => void
}

const pendingCallbacks = new Map<string, (json: string) => void>()
const eventHandlers = new Set<(event: DriverNativeEvent) => void>()
const nativeWindow = (): NativeWindow | null => typeof window === 'undefined' ? null : window as unknown as NativeWindow

function parseJson(json: string): unknown {
  try { return JSON.parse(json) as unknown }
  catch { return null }
}

export function driverNativeBridge(): DriverNativeBridge | null {
  const bridge = nativeWindow()?.LmsDriverNative
  try { return bridge && bridge.isAvailable() ? bridge : null }
  catch { return null }
}

// Native code needs an absolute origin; the bundled page is served from the gateway origin itself.
export const nativeGatewayOrigin = (): string => apiBaseUrl || (nativeWindow() ? window.location.origin : '')

function installReceivers(): void {
  const target = nativeWindow()
  if (!target || target.__lmsDriverNativeCallback) return
  target.__lmsDriverNativeCallback = (callbackId, json) => {
    const resolve = pendingCallbacks.get(callbackId)
    pendingCallbacks.delete(callbackId)
    resolve?.(json)
  }
  target.__lmsDriverNativeEvent = (json) => {
    const event = parseJson(json) as DriverNativeEvent | null
    if (event && typeof event.type === 'string') for (const handler of eventHandlers) handler(event)
  }
}

export function onDriverNativeEvent(handler: (event: DriverNativeEvent) => void): () => void {
  installReceivers()
  eventHandlers.add(handler)
  return () => { eventHandlers.delete(handler) }
}

export function nativeTripLocationState(bridge: DriverNativeBridge): NativeTripLocationState {
  const value = parseJson(bridge.getTripLocationState()) as Partial<NativeTripLocationState> | null
  return {
    tripId: typeof value?.tripId === 'string' ? value.tripId : '',
    lastSeq: Number.isSafeInteger(value?.lastSeq) ? Number(value?.lastSeq) : 0,
    isRunning: value?.isRunning === true,
    isActive: value?.isActive === true,
  }
}

// Refresh runs natively under one lock shared with the location service: refresh tokens rotate,
// and two parallel refreshes with the same token would revoke the whole session.
export function refreshNativeSession(bridge: DriverNativeBridge, staleRefreshToken: string): Promise<NativeRefreshResult> {
  installReceivers()
  return new Promise((resolve) => {
    const callbackId = `refresh_${Date.now()}_${Math.random().toString(36).slice(2, 10)}`
    pendingCallbacks.set(callbackId, (json) => {
      const result = parseJson(json) as NativeRefreshResult | null
      resolve(result ?? { ok: false, statusCode: 0, code: 50000, message: 'Session refresh failed.', traceId: 'unavailable' })
    })
    bridge.refreshSession(nativeGatewayOrigin(), staleRefreshToken, callbackId)
  })
}
