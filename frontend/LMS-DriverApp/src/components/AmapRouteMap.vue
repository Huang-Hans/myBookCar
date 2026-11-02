<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import L, { type Map as LeafletMap, type Marker as LeafletMarker } from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { TRIP_MAP_ROUTE_COLOR, tripStopMarkerContent, tripVehicleMarkerContent } from '../../../shared/tripMap/presentation'

import { buildAmapDrivingUrl, buildAmapGeocodeUrl, buildAmapPlaceSearchUrl, buildAmapRegeoUrl, buildAmapStaticMapUrl, calculateAmapRouteMetrics, parseAmapPosition, toAmapPosition, wgs84ToGcj02, type AmapPosition, type AmapRouteMetrics, type MapLocation } from '@/data/amap'

interface AmapGeocodeResponse {
  status: string
  geocodes?: Array<{ location: string }>
}

interface AmapRegeoResponse {
  status: string
  regeocode?: { formatted_address?: string }
}

interface AmapDrivingResponse {
  status: string
  route?: { paths?: Array<{ distance?: string; duration?: string; steps?: Array<{ polyline: string }> }> }
}

interface AmapPlaceSearchResponse {
  status: string
  pois?: Array<{ location: string }>
}

interface AmapMapInstance {
  add(overlays: AmapOverlay[]): void
  destroy(): void
  setFitView(overlays: AmapOverlay[], immediately?: boolean, avoid?: number[], maxZoom?: number): void
  zoomIn(): void
  zoomOut(): void
  setCenter(position: AmapPosition): void
  setZoom(zoom: number): void
}

interface AmapOverlay {}

interface AmapNamespace {
  Map: new (container: HTMLElement, options: Record<string, unknown>) => AmapMapInstance
  Marker: new (options: Record<string, unknown>) => AmapOverlay
  Polyline: new (options: Record<string, unknown>) => AmapOverlay
}

type MapState = 'loading' | 'ready' | 'error' | 'unconfigured'
type InteractiveState = 'idle' | 'loading' | 'ready' | 'error'

const props = withDefaults(defineProps<{
  apiKey: string
  securityJsCode?: string
  origin: MapLocation
  destination: MapLocation
  city?: string
  isInteractive?: boolean
  showAddressPanel?: boolean
  isCompactPreview?: boolean
}>(), { city: '澳门', securityJsCode: '', isInteractive: false, showAddressPanel: true, isCompactPreview: false })
const emit = defineEmits<{ metrics: [value: AmapRouteMetrics] }>()

const state = ref<MapState>('loading')
const interactiveState = ref<InteractiveState>('idle')
const mapImageUrl = ref('')
const mapContainer = ref<HTMLElement | { $el?: HTMLElement } | null>(null)
const hasCurrentPosition = ref(false)
const isLocating = ref(false)
const currentAddressText = ref('')
const staticZoom = ref(0)
const cachedPositions = ref<{ origin: AmapPosition; destination: AmapPosition; currentPosition: AmapPosition | null; routePoints: AmapPosition[]; approachPoints: AmapPosition[] } | null>(null)
const mapDescription = computed(() => state.value === 'unconfigured' ? 'AMap is not configured' : 'Unable to load AMap')
const startLabel = computed(() => locationLabel(props.origin))
const endLabel = computed(() => locationLabel(props.destination))
const currentLabel = computed(() => isLocating.value ? 'Locating…' : currentAddressText.value || 'Unavailable')
const panelPositions = computed(() => ({
  pickup: startLabel.value,
  dropoff: endLabel.value,
  driver: positionLabel(cachedPositions.value?.currentPosition, currentLabel.value),
}))

let liveMap: AmapMapInstance | null = null
let loadedAmap: AmapNamespace | null = null
let fallbackMap: LeafletMap | null = null
let fallbackCurrentMarker: LeafletMarker | null = null
let loadVersion = 0
let amapLoader: Promise<AmapNamespace> | null = null
const routeMetrics = { distanceMeters: 0, durationSeconds: 0 }

function locationLabel(location: MapLocation): string {
  if (typeof location === 'string') return location.trim()
  return `${location.latitude.toFixed(5)}, ${location.longitude.toFixed(5)}`
}

function positionLabel(position: AmapPosition | null | undefined, fallback: string): string {
  return position ? `${position[1].toFixed(5)}, ${position[0].toFixed(5)}` : fallback
}

function focusLocation(target: 'pickup' | 'dropoff' | 'driver'): void {
  if (interactiveState.value !== 'ready') return
  const positions = cachedPositions.value
  const position = target === 'pickup' ? positions?.origin : target === 'dropoff' ? positions?.destination : positions?.currentPosition
  if (!position) return
  if (fallbackMap) fallbackMap.setView(leafletPosition(position), 16)
  else if (liveMap) { liveMap.setZoom(16); liveMap.setCenter(position) }
}

function fitMapPadding(): number[] {
  return props.isCompactPreview ? [24, 24, 24, 24] : [88, 48, 300, 48]
}

function fitTilePadding(): { paddingTopLeft: [number, number]; paddingBottomRight: [number, number]; maxZoom: number } {
  return props.isCompactPreview
    ? { paddingTopLeft: [24, 24], paddingBottomRight: [24, 24], maxZoom: 17 }
    : { paddingTopLeft: [48, 48], paddingBottomRight: [48, 130], maxZoom: 17 }
}

function requestJson<T>(url: string): Promise<T> {
  return new Promise((resolve, reject) => {
    uni.request({
      url,
      method: 'GET',
      success: (response) => {
        if (response.statusCode >= 200 && response.statusCode < 300) resolve(response.data as T)
        else reject(new Error(`AMap request failed with HTTP ${response.statusCode}`))
      },
      fail: reject
    })
  })
}

// uni.getLocation on H5 reads navigator.geolocation (WGS-84) while the AMap map uses GCJ-02;
// native app and mini-program builds already return GCJ-02 for type gcj02, so convert on H5 only.
// The uni build replaces the exact text `process.env.UNI_PLATFORM` with the target platform.
declare const process: { env: { UNI_PLATFORM?: string } }
const isH5Build = process.env.UNI_PLATFORM === 'h5'

function getCurrentPosition(timeoutMs = 10_000): Promise<{ position: AmapPosition; speedMetersPerSecond: number } | null> {
  return new Promise((resolve) => {
    let settled = false
    let browserFallbackStarted = false
    const finish = (value: { position: AmapPosition; speedMetersPerSecond: number } | null): void => {
      if (settled) return
      settled = true
      clearTimeout(timer)
      clearTimeout(browserFallbackTimer)
      resolve(value)
    }
    const tryBrowserGeolocation = (): void => {
      if (settled || browserFallbackStarted || !isH5Build || typeof navigator === 'undefined' || !navigator.geolocation) return
      browserFallbackStarted = true
      navigator.geolocation.getCurrentPosition(
        (location) => {
          const raw: AmapPosition = [location.coords.longitude, location.coords.latitude]
          finish({ position: wgs84ToGcj02(raw), speedMetersPerSecond: Number(location.coords.speed) || 0 })
        },
        () => finish(null),
        { enableHighAccuracy: true, timeout: Math.max(1_000, timeoutMs - 3_000), maximumAge: 15_000 }
      )
    }
    const timer = setTimeout(() => finish(null), timeoutMs)
    const browserFallbackTimer = setTimeout(tryBrowserGeolocation, 3_000)
    uni.getLocation({
      type: 'gcj02',
      isHighAccuracy: true,
      success: (location) => {
        const raw: AmapPosition = [location.longitude, location.latitude]
        finish({ position: isH5Build ? wgs84ToGcj02(raw) : raw, speedMetersPerSecond: Number(location.speed) || 0 })
      },
      fail: tryBrowserGeolocation
    })
  })
}

async function resolvePosition(location: MapLocation): Promise<AmapPosition> {
  const coordinates = toAmapPosition(location)
  if (coordinates) return coordinates
  const name = typeof location === 'string' ? location.trim() : ''
  const placeUrl = buildAmapPlaceSearchUrl(props.apiKey, name, props.city)
  if (!placeUrl) throw new Error('AMap geocoding input is incomplete')
  const placeResponse = await requestJson<AmapPlaceSearchResponse>(placeUrl)
  const placePosition = placeResponse.status === '1' ? parseAmapPosition(placeResponse.pois?.[0]?.location ?? '') : null
  if (placePosition) return placePosition

  const geocodeUrl = buildAmapGeocodeUrl(props.apiKey, name, props.city)
  if (!geocodeUrl) throw new Error('AMap geocoding input is incomplete')
  const geocodeResponse = await requestJson<AmapGeocodeResponse>(geocodeUrl)
  const geocodePosition = geocodeResponse.status === '1' ? parseAmapPosition(geocodeResponse.geocodes?.[0]?.location ?? '') : null
  if (!geocodePosition) throw new Error('AMap could not resolve the location')
  return geocodePosition
}

interface ResolvedRoute {
  points: AmapPosition[]
  distanceMeters: number
  durationSeconds: number
}

const emptyRoute: ResolvedRoute = { points: [], distanceMeters: 0, durationSeconds: 0 }

async function resolveRoute(origin: AmapPosition, destination: AmapPosition): Promise<ResolvedRoute> {
  const url = buildAmapDrivingUrl(props.apiKey, origin, destination)
  if (!url) return emptyRoute
  const response = await requestJson<AmapDrivingResponse>(url)
  const path = response.status === '1' ? response.route?.paths?.[0] : undefined
  const points = path?.steps?.flatMap((step) => step.polyline.split(';')) ?? []
  return {
    points: points.map(parseAmapPosition).filter((point): point is AmapPosition => point !== null),
    distanceMeters: Number(path?.distance) || 0,
    durationSeconds: Number(path?.duration) || 0,
  }
}

function resolveMapElement(): HTMLElement | null {
  const value = mapContainer.value
  if (!value) return null
  if (value instanceof HTMLElement) return value
  return value.$el instanceof HTMLElement ? value.$el : null
}

// 高德 JS 地图要求容器是真正的 <div>；uni-app 的 <view> 在 H5 渲染为 <uni-view>，直接传入会报
// "Map container div not exist" 并退回瓦片地图。因此在容器里放一个铺满的 <div> 专供高德使用，重复加载时复用。
function amapHostOf(element: HTMLElement): HTMLDivElement {
  const existing = element.querySelector<HTMLDivElement>(':scope > div.amap-sdk-host')
  if (existing) return existing
  const host = document.createElement('div')
  host.className = 'amap-sdk-host'
  host.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;'
  element.appendChild(host)
  return host
}

function loadAmap(apiKey: string, securityJsCode: string): Promise<AmapNamespace> {
  if (typeof window === 'undefined' || typeof document === 'undefined') return Promise.reject(new Error('AMap JS API is only available in a browser'))
  const amapWindow = window as unknown as { AMap?: AmapNamespace; _AMapSecurityConfig?: { securityJsCode: string } }
  if (amapWindow.AMap) return Promise.resolve(amapWindow.AMap)
  if (amapLoader) return amapLoader

  if (securityJsCode.trim()) amapWindow._AMapSecurityConfig = { securityJsCode: securityJsCode.trim() }
  amapLoader = new Promise((resolve, reject) => {
    const script = document.createElement('script')
    const timeout = window.setTimeout(() => {
      amapLoader = null
      reject(new Error('AMap JS API timed out'))
    }, 15000)
    const cleanup = (): void => {
      window.clearTimeout(timeout)
    }
    script.onload = (): void => {
      cleanup()
      if (amapWindow.AMap) resolve(amapWindow.AMap)
      else {
        amapLoader = null
        reject(new Error('AMap JS API did not initialise'))
      }
    }
    script.async = true
    script.onerror = () => {
      cleanup()
      amapLoader = null
      reject(new Error('AMap JS API could not be loaded'))
    }
    // WINSON-DEVIATION: The AMap browser SDK requires its Web JS key in the script URL.
    script.src = `https://webapi.amap.com/maps?v=2.0&key=${encodeURIComponent(apiKey)}`
    document.head.appendChild(script)
  })
  return amapLoader
}

function currentMarker(AMap: AmapNamespace, position: AmapPosition): AmapOverlay {
  return new AMap.Marker({ position, anchor: 'center', content: tripVehicleMarkerContent(), zIndex: 100 })
}

function leafletPosition(position: AmapPosition): [number, number] {
  return [position[1], position[0]]
}

function leafletMarker(position: AmapPosition, modifier: 'pickup' | 'dropoff' | 'current'): LeafletMarker {
  return L.marker(leafletPosition(position), {
    icon: L.divIcon({
      className: 'lms-leaflet-marker-shell',
      html: modifier === 'current' ? tripVehicleMarkerContent() : tripStopMarkerContent(modifier === 'pickup' ? 'pickup' : 'dropoff'),
      iconSize: modifier === 'current' ? [30, 54] : [30, 30],
      iconAnchor: modifier === 'current' ? [15, 27] : [15, 15]
    })
  })
}

function renderAmapTileMap(origin: AmapPosition, destination: AmapPosition, route: AmapPosition[]): void {
  const element = resolveMapElement()
  if (!element) throw new Error('AMap tile container is unavailable')
  liveMap?.destroy()
  liveMap = null
  loadedAmap = null
  fallbackMap?.remove()
  fallbackMap = L.map(element, {
    attributionControl: false,
    zoomControl: false,
    dragging: true,
    scrollWheelZoom: true,
    touchZoom: true,
    doubleClickZoom: true
  })
  L.tileLayer('https://webst0{s}.is.autonavi.com/appmaptile?style=7&x={x}&y={y}&z={z}', {
    subdomains: ['1', '2', '3', '4'],
    minZoom: 3,
    maxZoom: 18,
    crossOrigin: true
  }).addTo(fallbackMap)
  const routePath = (route.length > 1 ? route : [origin, destination]).map(leafletPosition)
  L.polyline(routePath, { color: TRIP_MAP_ROUTE_COLOR, opacity: 1, weight: 7, lineJoin: 'round', lineCap: 'round' }).addTo(fallbackMap)
  const approachPoints = cachedPositions.value?.approachPoints ?? []
  if (approachPoints.length > 1) L.polyline(approachPoints.map(leafletPosition), { color: '#1677ff', opacity: .95, weight: 5, dashArray: '8 8', lineJoin: 'round', lineCap: 'round' }).addTo(fallbackMap)
  leafletMarker(origin, 'pickup').addTo(fallbackMap)
  leafletMarker(destination, 'dropoff').addTo(fallbackMap)
  const currentPosition = cachedPositions.value?.currentPosition ?? null
  if (currentPosition) fallbackCurrentMarker = leafletMarker(currentPosition, 'current').addTo(fallbackMap)
  const bounds = L.latLngBounds(routePath)
  if (currentPosition) bounds.extend(leafletPosition(currentPosition))
  fallbackMap.fitBounds(bounds, fitTilePadding())
  window.setTimeout(() => fallbackMap?.invalidateSize(), 0)
  interactiveState.value = 'ready'
}

async function renderInteractiveMap(origin: AmapPosition, destination: AmapPosition, route: AmapPosition[], version: number): Promise<void> {
  interactiveState.value = 'loading'
  await nextTick()
  const element = resolveMapElement()
  if (!element) throw new Error('AMap container is unavailable')
  const AMap = await loadAmap(props.apiKey, props.securityJsCode)
  if (version !== loadVersion) return

  liveMap?.destroy()
  // Match the standard AMap base style used by staff trip tracking.
  liveMap = new AMap.Map(amapHostOf(element), {
    center: origin,
    zoom: 13,
    resizeEnable: true,
    dragEnable: true,
    zoomEnable: true,
    jogEnable: true,
    doubleClickZoom: true,
    scrollWheel: true,
    touchZoom: true,
    viewMode: '2D',
  })
  loadedAmap = AMap

  const routePath = route.length > 1 ? route : [origin, destination]
  const routeLine = new AMap.Polyline({
    path: routePath,
    strokeColor: TRIP_MAP_ROUTE_COLOR,
    strokeOpacity: 1,
    strokeWeight: 7,
    strokeStyle: 'solid',
    lineJoin: 'round',
    lineCap: 'round',
    showDir: true,
    zIndex: 40
  })
  const approachPoints = cachedPositions.value?.approachPoints ?? []
  const approachLine = approachPoints.length > 1 ? new AMap.Polyline({ path: approachPoints, strokeColor: '#1677ff', strokeOpacity: .95, strokeWeight: 5, strokeStyle: 'dashed', lineJoin: 'round', lineCap: 'round', zIndex: 45 }) : null
  const pickupMarker = new AMap.Marker({ position: origin, anchor: 'center', content: tripStopMarkerContent('pickup'), zIndex: 90 })
  const dropoffMarker = new AMap.Marker({ position: destination, anchor: 'center', content: tripStopMarkerContent('dropoff'), zIndex: 90 })
  const routeOverlays = [routeLine, pickupMarker, dropoffMarker]
  liveMap.add(approachLine ? [...routeOverlays, approachLine] : routeOverlays)
  const currentPosition = cachedPositions.value?.currentPosition ?? null
  if (currentPosition) liveMap.add([currentMarker(AMap, currentPosition)])
  liveMap.setFitView(routeOverlays, false, fitMapPadding(), 17)
  interactiveState.value = 'ready'
}

function refreshStaticImage(): void {
  const cached = cachedPositions.value
  if (!cached) return
  mapImageUrl.value = buildAmapStaticMapUrl({
    apiKey: props.apiKey,
    origin: cached.origin,
    destination: cached.destination,
    currentPosition: cached.currentPosition,
    path: cached.routePoints,
    size: props.isInteractive ? '600*1024' : '750*420',
    zoom: staticZoom.value || undefined,
  }) ?? mapImageUrl.value
}

// Runs independently of the initial map render so a slow or blocked geolocation call
// never keeps the map from appearing. Adds the current-position marker and address
// once GPS resolves, and refreshes the metrics with the distance to pickup.
async function locateCurrentPosition(origin: AmapPosition, version: number): Promise<void> {
  isLocating.value = true
  const currentLocation = await getCurrentPosition()
  if (version !== loadVersion) { isLocating.value = false; return }
  if (!currentLocation) {
    isLocating.value = false
    hasCurrentPosition.value = false
    return
  }
  const position = currentLocation.position
  const [address, approach] = await Promise.all([
    reverseGeocode(position).catch(() => null),
    resolveRoute(position, origin).catch(() => emptyRoute),
  ])
  if (version !== loadVersion) { isLocating.value = false; return }
  isLocating.value = false
  hasCurrentPosition.value = true
  currentAddressText.value = address ?? `${position[1].toFixed(5)}, ${position[0].toFixed(5)}`
  const cached = cachedPositions.value
  if (cached) {
    cachedPositions.value = { ...cached, currentPosition: position, approachPoints: approach.points }
    if (approach.points.length > 1 && liveMap && loadedAmap && interactiveState.value === 'ready') {
      liveMap.add([new loadedAmap.Polyline({ path: approach.points, strokeColor: '#1677ff', strokeOpacity: .95, strokeWeight: 5, strokeStyle: 'dashed', lineJoin: 'round', lineCap: 'round', zIndex: 45 })])
    } else if (approach.points.length > 1 && fallbackMap && interactiveState.value === 'ready') {
      L.polyline(approach.points.map(leafletPosition), { color: '#1677ff', opacity: .95, weight: 5, dashArray: '8 8', lineJoin: 'round', lineCap: 'round' }).addTo(fallbackMap)
    }
    if (liveMap && loadedAmap && interactiveState.value === 'ready') liveMap.add([currentMarker(loadedAmap, position)])
    else if (fallbackMap && interactiveState.value === 'ready') {
      fallbackCurrentMarker?.remove()
      fallbackCurrentMarker = leafletMarker(position, 'current').addTo(fallbackMap)
    }
    else refreshStaticImage()
  }
  emit('metrics', calculateAmapRouteMetrics({
    routeDistanceMeters: routeMetrics.distanceMeters,
    routeDurationSeconds: routeMetrics.durationSeconds,
    distanceToPickupMeters: approach.distanceMeters,
    approachRouteDurationSeconds: approach.durationSeconds,
    reportedSpeedMetersPerSecond: currentLocation.speedMetersPerSecond,
    hasCurrentPosition: true,
  }))
}

async function reverseGeocode(position: AmapPosition): Promise<string | null> {
  const url = buildAmapRegeoUrl(props.apiKey, position)
  if (!url) return null
  const response = await requestJson<AmapRegeoResponse>(url)
  return response.status === '1' ? response.regeocode?.formatted_address ?? null : null
}

async function loadMap(): Promise<void> {
  const version = ++loadVersion
  const apiKey = props.apiKey.trim()
  if (!apiKey) {
    state.value = 'unconfigured'
    interactiveState.value = 'idle'
    mapImageUrl.value = ''
    hasCurrentPosition.value = false
    isLocating.value = false
    currentAddressText.value = ''
    cachedPositions.value = null
    return
  }

  state.value = 'loading'
  try {
    const [origin, destination] = await Promise.all([
      resolvePosition(props.origin),
      resolvePosition(props.destination),
    ])
    const route = await resolveRoute(origin, destination).catch(() => emptyRoute)
    if (version !== loadVersion) return
    hasCurrentPosition.value = false
    isLocating.value = false
    currentAddressText.value = ''
    staticZoom.value = 0
    cachedPositions.value = { origin, destination, currentPosition: null, routePoints: route.points, approachPoints: [] }
    routeMetrics.distanceMeters = route.distanceMeters
    routeMetrics.durationSeconds = route.durationSeconds
    emit('metrics', calculateAmapRouteMetrics({
      routeDistanceMeters: route.distanceMeters,
      routeDurationSeconds: route.durationSeconds,
      hasCurrentPosition: false,
    }))
    const url = buildAmapStaticMapUrl({ apiKey, origin, destination, path: route.points, size: props.isInteractive ? '600*1024' : '750*420' })
    if (!url) throw new Error('AMap static-map URL could not be created')
    mapImageUrl.value = url
    state.value = 'ready'
    void locateCurrentPosition(origin, version)
    if (props.isInteractive) {
      try {
        await renderInteractiveMap(origin, destination, route.points, version)
      } catch (reason) {
        console.warn(JSON.stringify({ event: 'amapInteractiveSdkFallback', message: reason instanceof Error ? reason.message : 'Unknown AMap error' }))
        if (version === loadVersion) {
          try { renderAmapTileMap(origin, destination, route.points) }
          catch { interactiveState.value = 'error' }
        }
      }
    } else {
      interactiveState.value = 'idle'
    }
  } catch {
    if (version !== loadVersion) return
    mapImageUrl.value = ''
    hasCurrentPosition.value = false
    isLocating.value = false
    currentAddressText.value = ''
    interactiveState.value = 'error'
    state.value = 'error'
  }
}

function handleImageError(): void {
  state.value = 'error'
  mapImageUrl.value = ''
}

function zoomBy(direction: 1 | -1): void {
  if (liveMap && interactiveState.value === 'ready') {
    if (direction === 1) liveMap.zoomIn()
    else liveMap.zoomOut()
    return
  }
  if (fallbackMap && interactiveState.value === 'ready') {
    if (direction === 1) fallbackMap.zoomIn()
    else fallbackMap.zoomOut()
    return
  }
  const next = Math.min(18, Math.max(3, (staticZoom.value || 14) + direction))
  if (next === staticZoom.value) return
  staticZoom.value = next
  refreshStaticImage()
}

watch(() => [props.apiKey, props.securityJsCode, props.origin, props.destination, props.city, props.isInteractive], () => { void loadMap() }, { deep: true, immediate: true })
onBeforeUnmount(() => {
  loadVersion += 1
  liveMap?.destroy()
  liveMap = null
  loadedAmap = null
  fallbackMap?.remove()
  fallbackMap = null
  fallbackCurrentMarker = null
})
</script>

<template>
  <view class="amap-route-map">
    <view v-if="isInteractive" ref="mapContainer" class="amap-canvas" :aria-label='$t("Interactive AMap route from pickup to drop-off")' />
    <image v-else-if="mapImageUrl" class="amap-image" :src="mapImageUrl" mode="aspectFill" aria-label="AMap route from pickup to drop-off" @error="handleImageError" />
    <view v-if="interactiveState === 'ready'" class="map-controls"><button :aria-label='$t("Zoom in")' @tap.stop="zoomBy(1)">+</button><button :aria-label='$t("Zoom out")' @tap.stop="zoomBy(-1)">−</button></view>
    <view v-if="showAddressPanel && state === 'ready'" class="map-address-panel" :class="{ compact: isCompactPreview }" :aria-label='$t("Trip addresses")'>
      <button class="address-row" :disabled="interactiveState !== 'ready'" :aria-label='$t("Pickup Point")' @tap.stop="focusLocation('pickup')"><text class="address-dot pickup-dot">A</text><view class="address-text"><small>{{ $t("Pickup Point") }}</small><text>{{ panelPositions.pickup }}</text></view><text class="address-chevron">›</text></button>
      <button class="address-row" :disabled="interactiveState !== 'ready'" :aria-label='$t("Drop-Off Point")' @tap.stop="focusLocation('dropoff')"><text class="address-dot dropoff-dot">B</text><view class="address-text"><small>{{ $t("Drop-Off Point") }}</small><text>{{ panelPositions.dropoff }}</text></view><text class="address-chevron">›</text></button>
      <button v-if="hasCurrentPosition" class="address-row" :disabled="interactiveState !== 'ready'" :aria-label='$t("Current location")' @tap.stop="focusLocation('driver')"><text class="address-dot current-dot">➤</text><view class="address-text"><small>{{ $t("Current location") }}</small><text>{{ panelPositions.driver }}</text></view><text class="address-chevron">›</text></button>
    </view>
    <view v-if="state === 'loading' || interactiveState === 'loading'" class="map-message">{{ $t("Loading AMap…") }}</view>
    <view v-else-if="!mapImageUrl && (state === 'unconfigured' || state === 'error')" class="map-error"><text>{{ mapDescription }}</text></view>
    <!-- #ifndef H5 -->
    <view v-if="interactiveState === 'error'" class="map-error interactive-map-error"><text>{{ $t("Interactive AMap is unavailable") }}</text></view>
    <!-- #endif -->
  </view>
</template>

<style scoped lang="scss">
.amap-route-map,.amap-image,.amap-canvas{display:block;width:100%;height:100%}.amap-route-map{position:relative;overflow:hidden;background:#eef2f3;isolation:isolate}.amap-canvas{position:relative;z-index:0;cursor:grab;touch-action:none}.amap-canvas:active{cursor:grabbing}.map-message{position:absolute;right:10px;bottom:10px;z-index:4;padding:6px 9px;border-radius:5px;background:rgba(255,255,255,.94);color:#697386;font-size:11px}.map-error{display:flex;width:100%;height:100%;align-items:center;justify-content:center;color:#697386;font-size:12px}.interactive-map-error{position:absolute;inset:0;z-index:2;background:#eef2f3}.map-controls{position:absolute;right:12px;top:12px;z-index:4;display:flex;gap:5px;flex-direction:column}.map-controls button{display:flex;width:40px;height:40px;margin:0;padding:0;align-items:center;justify-content:center;border-radius:7px;background:#fff;color:#101820;box-shadow:0 2px 8px rgba(16,24,32,.14);font-size:24px;line-height:normal}.map-controls button::after{display:none}.map-address-panel{position:absolute;left:12px;bottom:12px;z-index:3;display:flex;max-width:calc(100% - 110px);padding:8px 10px;gap:7px;border-radius:8px;background:rgba(255,255,255,.94);box-shadow:0 2px 8px rgba(16,24,32,.14);flex-direction:column;pointer-events:none}.address-row{display:flex;align-items:center;gap:7px}.address-dot{display:flex;width:14px;height:14px;flex:0 0 14px;align-items:center;justify-content:center;border-radius:50%;color:#fff;font-size:8px}.pickup-dot{background:#f1ae2e}.dropoff-dot{background:#101820}.current-dot{background:#1677ff}.address-text{display:flex;min-width:0;gap:1px;flex-direction:column}.address-text small{color:#9299a5;font-size:9px}.address-text text{overflow:hidden;color:#101820;font-size:11px;line-height:1.3;text-overflow:ellipsis;white-space:nowrap}
:deep(.lms-leaflet-marker-shell){border:0;background:transparent}

/* Match the staff tracking map while retaining each app's own GPS and route data. */
.map-controls{top:12px;right:12px;gap:0;overflow:hidden;border-radius:8px;background:#fff;box-shadow:0 2px 10px rgba(16,24,32,.16)}
.map-controls button{width:34px;height:34px;border-radius:0;box-shadow:none;font-size:20px}
.map-controls button + button{border-top:1px solid #eceef4}
.map-address-panel{left:12px;bottom:12px;z-index:5;display:flex;width:262px;max-width:calc(100% - 24px);padding:4px 0;gap:0;border-radius:10px;background:#fff;box-shadow:0 6px 22px rgba(16,24,32,.18);pointer-events:auto}
.address-row{display:flex;width:100%;min-height:40px;margin:0;padding:8px 12px;align-items:center;justify-content:flex-start;gap:10px;border-radius:0;background:transparent;text-align:left;line-height:normal;pointer-events:auto}
.address-row + .address-row{border-top:1px solid #eef0f4}
.address-row::after{display:none}
.address-row[disabled]{opacity:1}
.address-dot{width:26px;height:26px;flex:0 0 26px;font-size:12px;font-weight:700}
.address-text{min-width:0;flex:1;gap:2px}
.address-text small{color:#6b7285;font-size:10px}
.address-text text{color:#1d2333;font-size:12px}
.address-chevron{flex:0 0 auto;color:#4b5266;font-size:18px}
.map-address-panel.compact{left:8px;bottom:8px;width:180px;max-width:calc(100% - 16px);padding:2px 0;border-radius:7px;box-shadow:0 3px 12px rgba(16,24,32,.16)}
.compact .address-row{min-height:28px;padding:4px 7px;gap:6px}
.compact .address-dot{width:18px;height:18px;flex-basis:18px;font-size:9px}
.compact .address-text{gap:0}
.compact .address-text small{font-size:9px;line-height:1}
.compact .address-text text{font-size:10px;line-height:1.2}
.compact .address-chevron{display:none}
:deep(.track-vehicle){display:block;width:20px;height:36px;filter:drop-shadow(0 2px 4px rgba(0,0,0,.4))}
:deep(.track-vehicle svg){display:block}
:deep(.track-stop){display:grid;width:26px;height:26px;place-items:center;border:2px solid #fff;border-radius:50%;box-shadow:0 1px 4px rgba(0,0,0,.35);color:#fff;font-size:12px;font-weight:650}
:deep(.track-stop-pickup){background:#f1ae2e}
:deep(.track-stop-dropoff){background:#101820}
</style>
