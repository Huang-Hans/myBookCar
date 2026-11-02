<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useLocale } from '../i18n'
import { loadAmapConfig } from '../config/amap'
import type { TripLocation } from '../../../shared/tripLocations/contracts'
import { splitTrackSegments } from '../api/tripTrackSegments'
import { TRIP_MAP_ROUTE_COLOR, tripStopMarkerContent, tripVehicleMarkerContent } from '../../../shared/tripMap/presentation'

export interface MapStop {
  name: string
  longitude?: number
  latitude?: number
}

const props = withDefaults(defineProps<{ origin: MapStop; destination: MapStop; trackPoints?: TripLocation[]; showVehicle?: boolean }>(), { trackPoints: () => [], showVehicle: true })
const { t } = useLocale()
const mapHost = ref<HTMLElement | null>(null)
const mapError = ref('')
const isMapReady = ref(false)
const pickupPosition = ref<[number, number] | null>(null)
const dropoffPosition = ref<[number, number] | null>(null)
const latestPoint = computed(() => props.showVehicle ? props.trackPoints[props.trackPoints.length - 1] ?? null : null)
const driverPosition = computed<[number, number] | null>(() => latestPoint.value ? [latestPoint.value.longitude, latestPoint.value.latitude] : null)
// 地图是否正在跟随司机：打开时有司机位置就居中到司机并跟随；用户拖动或缩放后停止跟随，可点“回到司机”恢复。
const isFollowingDriver = ref(false)
const driverUpdatedAt = computed(() => {
  const recordedAt = latestPoint.value?.recordedAt
  return recordedAt ? new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false, timeZone: 'Asia/Macau' }).format(new Date(recordedAt)) : ''
})
const driverSpeed = computed(() => latestPoint.value?.speedKmh == null ? '' : `${Math.round(latestPoint.value.speedKmh)} km/h`)

interface LngLatLike { lng?: number; lat?: number; getLng?: () => number; getLat?: () => number }
interface Marker { setPosition(position: unknown): void }
interface MapInstance { destroy(): void }
interface DrivingRoute { time?: number; steps?: { path?: LngLatLike[] }[] }
// WINSON-DEVIATION: AMap's browser SDK exposes PascalCase constructors; this is not an LMS JSON contract.
/* eslint-disable @typescript-eslint/naming-convention */
interface Overlay { setMap(map: MapInstance | null): void }
interface Marker extends Overlay { setPosition(position: unknown): void; setAngle(degrees: number): void }
type Polyline = Overlay
interface MapInstance {
  destroy(): void
  setFitView(overlays?: Overlay[]): void
  setCenter(position: [number, number]): void
  setZoom(zoom: number): void
  zoomIn(): void
  zoomOut(): void
  on(event: string, handler: () => void): void
}
interface AMapApi {
  Map: new (element: HTMLElement, options: object) => MapInstance
  LngLat: new (longitude: number, latitude: number) => unknown
  Pixel: new (x: number, y: number) => unknown
  Marker: new (options: object) => Marker
  Polyline: new (options: object) => Polyline
  plugin(names: string[], ready: () => void): void
  PlaceSearch: new (options: object) => { search(keyword: string, callback: (status: string, result: string | { poiList?: { pois?: { location?: LngLatLike }[] } }) => void): void }
  Geocoder: new (options: object) => { getLocation(address: string, callback: (status: string, result: { geocodes?: { location?: LngLatLike }[] }) => void): void }
}
/* eslint-enable @typescript-eslint/naming-convention */

const USER_MOVE_EVENTS = ['dragstart', 'mousewheel', 'dblclick', 'touchstart']
const LOOKUP_TIMEOUT_MS = 3_000
const DRIVER_ZOOM = 16
const DEFAULT_CENTER: [number, number] = [113.5439, 22.1987]

let map: MapInstance | null = null
let vehicle: Marker | null = null
let trackLines: Polyline[] = []
let stopMarkers: Marker[] = []
let amapApi: AMapApi | null = null
let cancelled = false
let ready = false
let drawToken = 0
let hasUserMovedMap = false
let fittedKey = ''

function readLngLat(point: LngLatLike): [number, number] | null {
  const longitude = point.getLng?.() ?? point.lng
  const latitude = point.getLat?.() ?? point.lat
  if (longitude === undefined || latitude === undefined) return null
  return [longitude, latitude]
}

function toLngLat(point: TripLocation): unknown {
  return new (amapApi as AMapApi).LngLat(point.longitude, point.latitude)
}

function positionLabel(position: [number, number] | null, fallback: string): string {
  return position ? `${position[1].toFixed(5)}, ${position[0].toFixed(5)}` : fallback
}

function focusLocation(target: 'pickup' | 'dropoff' | 'driver'): void {
  if (!map) return
  if (target === 'driver') { followDriver(); return }
  const position = target === 'pickup' ? pickupPosition.value : dropoffPosition.value
  if (!position) return
  stopFollowing()
  map.setZoom(DRIVER_ZOOM)
  map.setCenter(position)
}

// 居中到司机并开始跟随：之后司机位置更新时地图跟着移动。
function followDriver(): void {
  if (!map || !driverPosition.value) return
  hasUserMovedMap = false
  isFollowingDriver.value = true
  map.setZoom(DRIVER_ZOOM)
  map.setCenter(driverPosition.value)
}

// 右上角的 + / − 缩放按钮；手动缩放后不再自动跟随司机。
function zoomMap(direction: 'in' | 'out'): void {
  if (!map) return
  stopFollowing()
  if (direction === 'in') map.zoomIn()
  else map.zoomOut()
}

function stopFollowing(): void {
  hasUserMovedMap = true
  isFollowingDriver.value = false
}

function placeVehicle(latest: TripLocation | undefined): void {
  if (!amapApi || !vehicle) return
  if (!latest || !props.showVehicle) { vehicle.setMap(null); return }
  vehicle.setPosition(toLngLat(latest))
  vehicle.setAngle(latest.headingDegrees ?? 0)
  vehicle.setMap(map)
}

// 视野规则：有司机位置时以司机为中心（首次居中，之后跟随）；没有司机位置时包住上下车点与轨迹。用户移动地图后不再自动调整。
function updateView(): void {
  if (!map || hasUserMovedMap) return
  if (driverPosition.value) {
    if (!isFollowingDriver.value) { followDriver(); return }
    map.setCenter(driverPosition.value)
    return
  }
  fitViewIfNeeded()
}

function fitViewIfNeeded(): void {
  const overlays = [...stopMarkers, ...trackLines]
  const key = `${stopMarkers.length}:${trackLines.length > 0}`
  if (!map || hasUserMovedMap || overlays.length === 0 || key === fittedKey) return
  map.setFitView(overlays)
  fittedKey = key
}

// Amap rejects a polyline with fewer than two points; each silent stretch of the track is drawn as its own line.
function drawTrackLines(points: readonly TripLocation[]): void {
  trackLines.forEach((line) => line.setMap(null))
  const amap = amapApi
  const target = map
  if (!amap || !target) return
  const drawable = splitTrackSegments(points).filter((segment) => segment.length >= 2)
  trackLines = drawable.map((segment) => new amap.Polyline({
    map: target, path: segment.map(toLngLat), strokeColor: TRIP_MAP_ROUTE_COLOR, strokeOpacity: 1, strokeWeight: 7,
    strokeStyle: 'solid', lineJoin: 'round', lineCap: 'round', zIndex: 40,
  }))
}

function renderTrack(): void {
  if (!amapApi || !map) return
  const points = props.trackPoints
  drawTrackLines(points)
  placeVehicle(points[points.length - 1])
  updateView()
}

function withTimeout<T>(lookup: Promise<T | null>): Promise<T | null> {
  return Promise.race([lookup, new Promise<null>((resolve) => setTimeout(() => resolve(null), LOOKUP_TIMEOUT_MS))])
}

async function searchPlace(name: string): Promise<LngLatLike | null> {
  if (!amapApi) return null
  const placeSearch = new amapApi.PlaceSearch({ city: '澳门', citylimit: false, pageSize: 5, pageIndex: 1 })
  return withTimeout(new Promise<LngLatLike | null>((resolve) => {
    placeSearch.search(name, (status, result) => {
      if (status === 'complete' && typeof result !== 'string') { resolve(result.poiList?.pois?.[0]?.location ?? null); return }
      console.warn(JSON.stringify({ event: 'amapPlaceSearchFailed', name, status, info: typeof result === 'string' ? result : '' }))
      resolve(null)
    })
  }))
}

async function geocodeName(name: string): Promise<LngLatLike | null> {
  if (!amapApi) return null
  const geocoder = new amapApi.Geocoder({ city: '澳门' })
  return withTimeout(new Promise<LngLatLike | null>((resolve) => {
    geocoder.getLocation(name, (status, result) => resolve(status === 'complete' ? result.geocodes?.[0]?.location ?? null : null))
  }))
}

// Same lookup order as the driver app (place search, then geocoder by name) so both ends show the stops at the same spot.
async function lookupStop(stop: MapStop): Promise<[number, number] | null> {
  const name = stop.name.trim()
  const found = name ? await searchPlace(name) ?? await geocodeName(name) : null
  const pair = found ? readLngLat(found) : null
  console.info(JSON.stringify({ event: 'tripMapStopResolved', name, source: pair ? 'amapLookup' : 'storedCoordinates', position: pair ?? [stop.longitude, stop.latitude] }))
  return pair
}

function storedPosition(stop: MapStop): [number, number] | null {
  return typeof stop.longitude === 'number' && typeof stop.latitude === 'number' ? [stop.longitude, stop.latitude] : null
}

function placeStopMarker(amap: AMapApi, nextMap: MapInstance, index: number, pair: [number, number]): void {
  if (index === 0) pickupPosition.value = pair
  else dropoffPosition.value = pair
  const position = new amap.LngLat(pair[0], pair[1])
  const existing = stopMarkers[index]
  if (existing) { existing.setPosition(position); return }
  const kind = index === 0 ? 'pickup' : 'dropoff'
  const title = t(index === 0 ? 'Pickup location' : 'Drop-off location')
  stopMarkers[index] = new amap.Marker({ map: nextMap, position, title, content: tripStopMarkerContent(kind), offset: new amap.Pixel(-13, -13) })
}

function placeStops(amap: AMapApi, nextMap: MapInstance, origin: [number, number], destination: [number, number]): void {
  placeStopMarker(amap, nextMap, 0, origin)
  placeStopMarker(amap, nextMap, 1, destination)
  fittedKey = ''
  renderTrack()
}

function resetOverlays(): void {
  isMapReady.value = false
  pickupPosition.value = null
  dropoffPosition.value = null
  map?.destroy()
  map = null
  vehicle = null
  trackLines = []
  stopMarkers = []
  hasUserMovedMap = false
  isFollowingDriver.value = false
  fittedKey = ''
}

function createMap(amap: AMapApi, host: HTMLElement): MapInstance {
  resetOverlays()
  const nextMap = new amap.Map(host, { zoom: 12, center: DEFAULT_CENTER, resizeEnable: true })
  USER_MOVE_EVENTS.forEach((event) => nextMap.on(event, stopFollowing))
  vehicle = new amap.Marker({ position: new amap.LngLat(DEFAULT_CENTER[0], DEFAULT_CENTER[1]), content: tripVehicleMarkerContent(), offset: new amap.Pixel(-10, -18) })
  map = nextMap
  isMapReady.value = true
  return nextMap
}

// The map, the stored stops and the track show immediately; the name lookup only refines the stops afterwards.
async function draw(): Promise<void> {
  if (!amapApi || !mapHost.value || cancelled) return
  const token = ++drawToken
  mapError.value = ''
  const amap = amapApi
  const nextMap = createMap(amap, mapHost.value)
  const stored = [storedPosition(props.origin), storedPosition(props.destination)] as const
  if (stored[0] && stored[1]) placeStops(amap, nextMap, stored[0], stored[1])
  else renderTrack()
  const [lookedUpOrigin, lookedUpDestination] = await Promise.all([lookupStop(props.origin), lookupStop(props.destination)])
  // A newer draw has already replaced this map, so there is nothing left to draw on.
  if (token !== drawToken || cancelled) return
  const origin = lookedUpOrigin ?? stored[0]
  const destination = lookedUpDestination ?? stored[1]
  if (!origin || !destination) { mapError.value = 'Unable to place the trip on the map.'; return }
  placeStops(amap, nextMap, origin, destination)
}

async function start(): Promise<void> {
  try {
    const config = await loadAmapConfig()
    if (!config.key) { mapError.value = 'Add the Amap key in public/config/amap.json.'; return }
    // eslint-disable-next-line @typescript-eslint/naming-convention
    const host = window as Window & { AMap?: AMapApi }
    if (!host.AMap) {
      await new Promise<void>((resolve, reject) => {
        const script = document.createElement('script')
        script.src = `https://webapi.amap.com/maps?v=2.0&key=${encodeURIComponent(config.key)}`
        script.async = true
        script.onload = () => resolve()
        script.onerror = () => reject(new Error('Unable to load Amap.'))
        document.head.appendChild(script)
      })
    }
    if (!host.AMap || cancelled) return
    amapApi = host.AMap
    // The map itself must not depend on the lookup plugins: if they fail to load, stops fall back to stored coordinates.
    await withTimeout(new Promise<true>((resolve) => amapApi?.plugin(['AMap.PlaceSearch', 'AMap.Geocoder'], () => resolve(true))))
    ready = true
    await draw()
  } catch (cause) {
    mapError.value = cause instanceof Error ? cause.message : 'Unable to load Amap.'
  }
}

onMounted(() => { void start() })
watch(() => [props.origin.name, props.origin.longitude, props.origin.latitude, props.destination.name, props.destination.longitude, props.destination.latitude], () => { if (ready) void draw() })
watch(() => props.trackPoints, () => renderTrack())
watch(() => props.showVehicle, () => renderTrack())
onUnmounted(() => { cancelled = true; isMapReady.value = false; map?.destroy(); map = null })
</script>

<template>
  <div class="track-map-wrap">
    <div class="track-map-stage">
      <div ref="mapHost" class="track-map" role="img" :aria-label="t('Trip map')"></div>
      <div class="track-zoom" role="group" :aria-label="t('Trip map')">
        <button type="button" :disabled="!isMapReady" aria-label="+" @click.stop="zoomMap('in')">+</button>
        <button type="button" :disabled="!isMapReady" aria-label="−" @click.stop="zoomMap('out')">−</button>
      </div>
      <div class="track-location-panel">
        <button type="button" class="track-location-row" :disabled="!isMapReady || !pickupPosition" :aria-label="t('Pickup location')" @click.stop="focusLocation('pickup')">
          <span class="track-location-badge pickup-badge">A</span>
          <span class="track-location-text"><small>{{ t('Pickup location') }}</small><span>{{ positionLabel(pickupPosition, origin.name) }}</span></span>
          <span class="track-location-chevron" aria-hidden="true">›</span>
        </button>
        <button type="button" class="track-location-row" :disabled="!isMapReady || !dropoffPosition" :aria-label="t('Drop-off location')" @click.stop="focusLocation('dropoff')">
          <span class="track-location-badge dropoff-badge">B</span>
          <span class="track-location-text"><small>{{ t('Drop-off location') }}</small><span>{{ positionLabel(dropoffPosition, destination.name) }}</span></span>
          <span class="track-location-chevron" aria-hidden="true">›</span>
        </button>
        <button type="button" class="track-location-row" :disabled="!isMapReady || !driverPosition" :aria-label="t('Driver location')" @click.stop="focusLocation('driver')">
          <span class="track-location-badge driver-badge" aria-hidden="true"><svg viewBox="0 0 24 24" width="14" height="14"><path d="M21 3 3 10.5l7.5 3L14 21z" fill="currentColor" /></svg></span>
          <span class="track-location-text"><small>{{ t('Driver location') }}</small><span>{{ positionLabel(driverPosition, t('Waiting for location updates')) }}</span></span>
          <span class="track-location-chevron" aria-hidden="true">›</span>
        </button>
      </div>
    </div>
    <p v-if="mapError" class="form-error">{{ t(mapError) }}</p>
  </div>
</template>

<style scoped>
.track-map-stage { position: relative; overflow: hidden; border-radius: 10px; background: #eef0f4; }
.track-map-stage .track-map { height: clamp(360px, 58vh, 500px); }
.track-zoom { position: absolute; z-index: 5; top: 12px; right: 12px; display: flex; overflow: hidden; flex-direction: column; border-radius: 8px; background: #fff; box-shadow: 0 2px 10px rgba(16, 24, 32, .16); }
.track-zoom button { width: 34px; height: 34px; margin: 0; padding: 0; border: 0; background: #fff; color: #1d2333; font-size: 20px; line-height: 1; cursor: pointer; }
.track-zoom button + button { border-top: 1px solid #eceef4; }
.track-zoom button:hover:not(:disabled) { background: #f4f5fa; }
.track-zoom button:disabled { cursor: default; opacity: .5; }
.track-location-panel { position: absolute; z-index: 5; left: 12px; bottom: 12px; display: flex; width: 262px; max-width: calc(100% - 24px); padding: 4px 0; flex-direction: column; border-radius: 10px; background: #fff; box-shadow: 0 6px 22px rgba(16, 24, 32, .18); }
.track-location-row { display: flex; width: 100%; margin: 0; padding: 10px 14px; align-items: center; justify-content: flex-start; gap: 12px; border: 0; border-radius: 0; background: transparent; color: inherit; text-align: left; line-height: normal; cursor: pointer; }
.track-location-row + .track-location-row { border-top: 1px solid #eef0f4; }
.track-location-row:hover:not(:disabled) { background: #f6f7fc; }
.track-location-row:disabled { cursor: default; opacity: 1; }
.track-location-row:focus-visible { outline: 2px solid #3a42a8; outline-offset: -2px; }
.track-location-badge { display: grid; width: 26px; height: 26px; flex: 0 0 26px; place-items: center; border-radius: 50%; color: #fff; font-size: 12px; font-weight: 700; }
.pickup-badge { background: #f1ae2e; }
.dropoff-badge { background: #101820; }
.driver-badge { background: #1677ff; }
.track-location-text { display: flex; min-width: 0; flex: 1; gap: 2px; flex-direction: column; }
.track-location-text small { color: #6b7285; font-size: 11px; }
.track-location-text > span { overflow: hidden; color: #1d2333; font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.track-location-chevron { color: #4b5266; font-size: 18px; }
</style>
