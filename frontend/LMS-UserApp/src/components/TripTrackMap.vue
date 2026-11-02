<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { t } from '../i18n'
import { mapConfig } from '../../../map.config'
import { TRIP_MAP_ROUTE_COLOR, tripStopMarkerContent, tripVehicleMarkerContent } from '../../../shared/tripMap/presentation'
import type { TripLocation } from '../../../shared/tripLocations/contracts'
import { splitTrackSegments } from '../api/tripTrackSegments'
import { buildDrivingUrl, distanceMeters, parseDrivingRoute, tripMapMetrics, type DrivingRoute, type MapPosition, type TripMapMetrics } from '../api/tripMapRoute'

export interface MapStop {
  name: string
  longitude?: number
  latitude?: number
}

const props = withDefaults(defineProps<{ origin: MapStop; destination: MapStop; trackPoints?: TripLocation[]; showVehicle?: boolean; showApproachRoute?: boolean }>(), { trackPoints: () => [], showVehicle: true, showApproachRoute: false })
const emit = defineEmits<{ metrics: [value: TripMapMetrics] }>()
const mapHost = ref<HTMLElement | { $el?: HTMLElement } | null>(null)
const mapError = ref('')
const mapReady = ref(false)
const isApproximateRoute = ref(false)
const resolvedOrigin = ref<MapPosition | null>(null)
const resolvedDestination = ref<MapPosition | null>(null)
const latestPoint = computed(() => props.showVehicle ? props.trackPoints[props.trackPoints.length - 1] : undefined)
const driverPosition = computed<MapPosition | null>(() => latestPoint.value ? [latestPoint.value.longitude, latestPoint.value.latitude] : null)

interface LngLatLike { lng?: number; lat?: number; getLng?: () => number; getLat?: () => number }
// WINSON-DEVIATION: AMap's browser SDK exposes PascalCase constructors; this is not an LMS JSON contract.
/* eslint-disable @typescript-eslint/naming-convention */
interface Overlay { setMap(map: MapInstance | null): void }
interface Marker extends Overlay { setPosition(position: unknown): void; setAngle(degrees: number): void }
type Polyline = Overlay
interface MapInstance { destroy(): void; setFitView(overlays?: Overlay[]): void; setCenter(position: unknown): void; setZoom(zoom: number): void; zoomIn(): void; zoomOut(): void; on(event: string, handler: () => void): void }
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

const TRACK_COLOR = '#1677ff'
const USER_MOVE_EVENTS = ['dragstart', 'mousewheel', 'dblclick', 'touchstart']
const LOOKUP_TIMEOUT_MS = 3_000
const DEFAULT_CENTER: [number, number] = [113.5439, 22.1987]

let map: MapInstance | null = null
let vehicle: Marker | null = null
let trackLines: Polyline[] = []
let routeLine: Polyline | null = null
let approachRouteLine: Polyline | null = null
let stopMarkers: Marker[] = []
let plannedRoute: DrivingRoute | null = null
let amapApi: AMapApi | null = null
let cancelled = false
let ready = false
let drawToken = 0
let hasUserMovedMap = false
let fittedKey = ''
let approachRouteKey = ''
let approachDrawToken = 0

function readLngLat(point: LngLatLike): [number, number] | null {
  const longitude = point.getLng?.() ?? point.lng
  const latitude = point.getLat?.() ?? point.lat
  if (longitude === undefined || latitude === undefined) return null
  return [longitude, latitude]
}

function toLngLat(point: TripLocation): unknown {
  return new (amapApi as AMapApi).LngLat(point.longitude, point.latitude)
}

function formatPosition(position: MapPosition | null, fallback: string): string {
  return position ? `${position[1].toFixed(5)}, ${position[0].toFixed(5)}` : fallback
}

function focusPosition(position: MapPosition | null): void {
  if (!position || !map || !amapApi) return
  hasUserMovedMap = true
  map.setCenter(new amapApi.LngLat(position[0], position[1]))
  map.setZoom(16)
}

function zoomBy(direction: 1 | -1): void {
  if (!map) return
  hasUserMovedMap = true
  if (direction === 1) map.zoomIn()
  else map.zoomOut()
}

function placeVehicle(latest: TripLocation | undefined): void {
  if (!amapApi || !vehicle) return
  if (!latest || !props.showVehicle) { vehicle.setMap(null); return }
  vehicle.setPosition(toLngLat(latest))
  vehicle.setAngle(latest.headingDegrees ?? 0)
  vehicle.setMap(map)
}

// The view follows the stops and the track until the user moves the map; it is refitted only when what is shown changes.
function fitViewIfNeeded(): void {
  const driver = driverPosition.value
  const isDriverNearby = driver && [resolvedOrigin.value, resolvedDestination.value].some(
    (stop) => stop && distanceMeters(driver, stop) <= 50_000,
  )
  const visibleVehicle = isDriverNearby && vehicle ? [vehicle] : []
  const overlays = [...stopMarkers, ...(routeLine ? [routeLine] : []), ...(approachRouteLine ? [approachRouteLine] : []), ...trackLines, ...visibleVehicle]
  const key = `${stopMarkers.length}:${Boolean(routeLine)}:${Boolean(approachRouteLine)}:${trackLines.length > 0}:${visibleVehicle.length > 0}`
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
    map: target, path: segment.map(toLngLat), strokeColor: TRACK_COLOR, strokeOpacity: .9, strokeWeight: 5,
    strokeStyle: 'solid', lineJoin: 'round', lineCap: 'round', zIndex: 50,
  }))
}

function drawRoute(points: readonly MapPosition[], route: DrivingRoute | null): void {
  routeLine?.setMap(null)
  if (!amapApi || !map) return
  routeLine = new amapApi.Polyline({
    map, path: points.map((position) => new (amapApi as AMapApi).LngLat(position[0], position[1])),
    strokeColor: TRIP_MAP_ROUTE_COLOR, strokeOpacity: 1, strokeWeight: 7,
    strokeStyle: 'solid', lineJoin: 'round', lineCap: 'round', showDir: true, zIndex: 40,
  })
  plannedRoute = route
  isApproximateRoute.value = route === null
  fittedKey = ''
  renderTrack()
}

function drawApproachLine(points: readonly MapPosition[]): void {
  approachRouteLine?.setMap(null)
  approachRouteLine = null
  if (!amapApi || !map || points.length < 2) return
  approachRouteLine = new amapApi.Polyline({
    map, path: points.map((position) => new (amapApi as AMapApi).LngLat(position[0], position[1])),
    strokeColor: '#1677ff', strokeOpacity: .95, strokeWeight: 6,
    strokeStyle: 'dashed', lineJoin: 'round', lineCap: 'round', showDir: true, zIndex: 60,
  })
  fittedKey = ''
  fitViewIfNeeded()
}

function updateApproachRoute(): void {
  const driver = driverPosition.value
  const pickup = resolvedOrigin.value
  if (!props.showApproachRoute || !driver || !pickup || distanceMeters(driver, pickup) < 30) {
    approachDrawToken++
    approachRouteKey = ''
    drawApproachLine([])
    return
  }
  // GPS updates within the same map grid reuse the route instead of requesting AMap on every poll.
  const key = `${driver.map((coordinate) => coordinate.toFixed(3)).join(',')}:${pickup.join(',')}`
  if (key === approachRouteKey) return
  approachRouteKey = key
  const token = ++approachDrawToken
  drawApproachLine([driver, pickup])
  void loadDrivingRoute(driver, pickup).then((route) => {
    if (token !== approachDrawToken || !route) return
    drawApproachLine(route.points)
  })
}

function renderTrack(): void {
  if (!amapApi || !map) return
  const points = props.trackPoints
  drawTrackLines(points)
  placeVehicle(points[points.length - 1])
  updateApproachRoute()
  fitViewIfNeeded()
  emit('metrics', tripMapMetrics(plannedRoute, resolvedOrigin.value, points, props.showVehicle))
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
  const stored = storedPosition(stop)
  if (stored) return stored
  const name = stop.name.trim()
  const found = name ? await searchPlace(name) ?? await geocodeName(name) : null
  const pair = found ? readLngLat(found) : null
  return pair
}

function storedPosition(stop: MapStop): [number, number] | null {
  return typeof stop.longitude === 'number' && typeof stop.latitude === 'number' ? [stop.longitude, stop.latitude] : null
}

function placeStopMarker(amap: AMapApi, nextMap: MapInstance, index: number, pair: [number, number]): void {
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
  resolvedOrigin.value = origin
  resolvedDestination.value = destination
  fittedKey = ''
  renderTrack()
}

function loadDrivingRoute(origin: MapPosition, destination: MapPosition): Promise<DrivingRoute | null> {
  const url = buildDrivingUrl(mapConfig.webKey, origin, destination)
  if (!url) return Promise.resolve(null)
  return new Promise((resolve) => {
    uni.request({
      url, method: 'GET', timeout: 8_000,
      success: (response) => resolve(response.statusCode === 200 ? parseDrivingRoute(response.data) : null),
      fail: () => resolve(null),
    })
  })
}

function resetOverlays(): void {
  map?.destroy()
  map = null
  mapReady.value = false
  vehicle = null
  trackLines = []
  routeLine = null
  approachRouteLine = null
  approachRouteKey = ''
  approachDrawToken++
  stopMarkers = []
  plannedRoute = null
  resolvedOrigin.value = null
  resolvedDestination.value = null
  hasUserMovedMap = false
  fittedKey = ''
}

function createMap(amap: AMapApi, host: HTMLElement): MapInstance {
  resetOverlays()
  const nextMap = new amap.Map(host, { zoom: 12, center: DEFAULT_CENTER, resizeEnable: true, viewMode: '2D' })
  USER_MOVE_EVENTS.forEach((event) => nextMap.on(event, () => { hasUserMovedMap = true }))
  vehicle = new amap.Marker({ position: new amap.LngLat(DEFAULT_CENTER[0], DEFAULT_CENTER[1]), content: tripVehicleMarkerContent(), offset: new amap.Pixel(-10, -18), zIndex: 90 })
  map = nextMap
  mapReady.value = true
  return nextMap
}

// The map, the stored stops and the track show immediately; the name lookup only refines the stops afterwards.
async function draw(): Promise<void> {
  if (!amapApi || !mapHost.value || cancelled) return
  const host = mapHost.value instanceof HTMLElement ? mapHost.value : mapHost.value.$el
  if (!host) { mapError.value = 'Unable to load Amap.'; return }
  const token = ++drawToken
  mapError.value = ''
  const amap = amapApi
  const nextMap = createMap(amap, host)
  const stored = [storedPosition(props.origin), storedPosition(props.destination)] as const
  if (stored[0] && stored[1]) {
    placeStops(amap, nextMap, stored[0], stored[1])
    drawRoute([stored[0], stored[1]], null)
  }
  else renderTrack()
  const [lookedUpOrigin, lookedUpDestination] = await Promise.all([lookupStop(props.origin), lookupStop(props.destination)])
  // A newer draw has already replaced this map, so there is nothing left to draw on.
  if (token !== drawToken || cancelled) return
  const origin = lookedUpOrigin ?? stored[0]
  const destination = lookedUpDestination ?? stored[1]
  if (!origin || !destination) { mapError.value = 'Unable to place the trip on the map.'; return }
  placeStops(amap, nextMap, origin, destination)
  drawRoute([origin, destination], null)
  const route = await loadDrivingRoute(origin, destination)
  if (token !== drawToken || cancelled) return
  if (route) drawRoute(route.points, route)
}

async function start(): Promise<void> {
  try {
    const config = mapConfig
    if (!config.webKey) { mapError.value = 'AMap is not configured.'; return }
    if (config.securityJsCode) {
      const securedWindow = window as Window & { _AMapSecurityConfig?: { securityJsCode: string } }
      securedWindow._AMapSecurityConfig = { securityJsCode: config.securityJsCode }
    }
    // eslint-disable-next-line @typescript-eslint/naming-convention
    const host = window as Window & { AMap?: AMapApi }
    if (!host.AMap) {
      await new Promise<void>((resolve, reject) => {
        const script = document.createElement('script')
        script.src = `https://webapi.amap.com/maps?v=2.0&key=${encodeURIComponent(config.webKey)}`
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
watch(() => [props.showVehicle, props.showApproachRoute], () => renderTrack())
onUnmounted(() => { cancelled = true; map?.destroy(); map = null })
</script>

<template>
  <div class="track-map-wrap">
    <div ref="mapHost" class="track-map" role="img" :aria-label="t('Trip map')"></div>
    <div v-if="mapReady" class="map-provider">●　{{ t('高德路線預估') }}</div>
    <div v-if="mapReady" class="map-controls">
      <button type="button" :aria-label="t('放大地圖')" @tap.stop="zoomBy(1)">+</button>
      <button type="button" :aria-label="t('縮小地圖')" @tap.stop="zoomBy(-1)">−</button>
    </div>
    <div v-if="mapReady && isApproximateRoute" class="map-approximate">{{ t('暫無道路路線，顯示直線估算') }}</div>
    <div v-if="mapReady && resolvedOrigin && resolvedDestination" class="map-address-panel" :aria-label="t('行程地址')">
      <button type="button" class="address-row" @tap.stop="focusPosition(resolvedOrigin)"><span class="address-dot pickup">A</span><span class="address-text"><small>{{ t('上車點') }}</small><span>{{ origin.name || t('未知上車地點') }}</span></span></button>
      <button type="button" class="address-row" @tap.stop="focusPosition(resolvedDestination)"><span class="address-dot dropoff">B</span><span class="address-text"><small>{{ t('下車點') }}</small><span>{{ destination.name || t('未知下車地點') }}</span></span></button>
      <button v-if="driverPosition" type="button" class="address-row" @tap.stop="focusPosition(driverPosition)"><span class="address-dot driver">➤</span><span class="address-text"><small>{{ t('司機位置') }}</small><span>{{ formatPosition(driverPosition, '') }}</span></span></button>
    </div>
    <p v-if="mapError" class="track-map-error">{{ t(mapError) }}</p>
  </div>
</template>

<style>
.track-map-wrap { position: relative; z-index:0; isolation:isolate; min-width: 0; height: 340px; overflow: hidden; background: #eef2f3; }
.track-map { width: 100%; height: 100%; background: #eef2f3; }
.track-map-error { position: absolute; inset: 0; display: grid; place-items: center; margin: 0; padding: 14px; background: #eef2f3; color: #bd3d4b; font-size: 12px; }
.map-provider { position: absolute; top: 12px; left: 12px; z-index: 3; padding: 8px 11px; border-radius: 6px; background: #fff; color: #243044; box-shadow: 0 2px 8px #10182024; font-size: 11px; }
.map-provider::first-letter { color: #f1ae2e; }
.map-controls { position: absolute; top: 12px; right: 12px; z-index: 3; overflow: hidden; border-radius: 8px; background: #fff; box-shadow: 0 2px 10px #10182029; }
.map-controls button { display: grid; width: 34px; height: 34px; margin: 0; padding: 0; place-items: center; border: 0; border-radius: 0; background: #fff; color: #101820; font-size: 20px; line-height: 1; }
.map-controls button + button { border-top: 1px solid #eceef4; }
.map-approximate { position: absolute; top: 53px; left: 12px; z-index: 3; padding: 6px 9px; border-radius: 6px; background: #fff4de; color: #8a5b12; font-size: 10px; }
.map-address-panel { position: absolute; bottom: 38px; left: 8px; z-index: 3; width: 190px; max-width: calc(100% - 16px); overflow: hidden; border-radius: 8px; background: #fff; box-shadow: 0 3px 12px #10182029; }
.address-row { display: flex; box-sizing: border-box; width: 100%; min-height: 36px; margin: 0; padding: 5px 8px; align-items: center; gap: 7px; border: 0; border-radius: 0; background: #fff; text-align: left; }
.address-row + .address-row { border-top: 1px solid #eef0f4; }
.address-dot { display: grid; width: 18px; height: 18px; flex: 0 0 18px; place-items: center; border-radius: 50%; color: #fff; font-size: 9px; font-weight: 700; }
.address-dot.pickup { background: #f1ae2e; }.address-dot.dropoff { background: #101820; }.address-dot.driver { background: #1677ff; }
.address-text { display: flex; min-width: 0; flex: 1; flex-direction: column; gap: 1px; color: #101820; font-size: 10px; }
.address-text small { color: #7b8495; font-size: 9px; }
.address-text > span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.track-vehicle { display: block; width: 20px; height: 36px; filter: drop-shadow(0 2px 4px #0006); }
.track-vehicle svg { display: block; }
.track-stop { display: grid; place-items: center; width: 26px; height: 26px; border: 2px solid #fff; border-radius: 50%; color: #fff; font-size: 12px; font-weight: 700; box-shadow: 0 1px 4px #0006; }
.track-stop-pickup { background: #f1ae2e; }
.track-stop-dropoff { background: #101820; }
</style>
