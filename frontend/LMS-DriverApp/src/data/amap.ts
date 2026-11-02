export interface MapCoordinates {
  latitude: number
  longitude: number
}

export type MapLocation = string | MapCoordinates
export type AmapPosition = [number, number]

export interface AmapStaticMapOptions {
  apiKey: string
  origin: AmapPosition
  destination: AmapPosition
  currentPosition?: AmapPosition | null
  path?: AmapPosition[]
  size?: string
  zoom?: number
}

export interface AmapRouteMetrics {
  coveredKm: number
  distanceToPickupKm: number
  speedKmh: number
  etaMinutes: number
  hasCurrentPosition: boolean
}

export function calculateAmapRouteMetrics(input: {
  routeDistanceMeters: number
  routeDurationSeconds: number
  distanceToPickupMeters?: number
  approachRouteDurationSeconds?: number
  reportedSpeedMetersPerSecond?: number
  hasCurrentPosition: boolean
}): AmapRouteMetrics {
  const routeDistanceMeters = Math.max(0, input.routeDistanceMeters)
  const routeDurationSeconds = Math.max(0, input.routeDurationSeconds)
  const reportedSpeed = Math.max(0, input.reportedSpeedMetersPerSecond ?? 0)
  const estimatedSpeed = routeDurationSeconds > 0 ? routeDistanceMeters / routeDurationSeconds : 0
  const etaDurationSeconds = !input.hasCurrentPosition ? 0
    : input.approachRouteDurationSeconds !== undefined ? Math.max(0, input.approachRouteDurationSeconds) : routeDurationSeconds
  return {
    coveredKm: 0,
    distanceToPickupKm: Math.max(0, input.distanceToPickupMeters ?? 0) / 1_000,
    speedKmh: (reportedSpeed || estimatedSpeed) * 3.6,
    etaMinutes: etaDurationSeconds > 0 ? Math.max(1, Math.ceil(etaDurationSeconds / 60)) : 0,
    hasCurrentPosition: input.hasCurrentPosition,
  }
}

function validCoordinates(location: MapCoordinates): boolean {
  return Number.isFinite(location.latitude) && Number.isFinite(location.longitude)
}

export function toAmapPosition(location: MapLocation): AmapPosition | null {
  if (typeof location === 'string' || !validCoordinates(location)) return null
  return [location.longitude, location.latitude]
}

export function parseAmapPosition(value: string): AmapPosition | null {
  const [longitudeText, latitudeText] = value.split(',', 2)
  const longitude = Number(longitudeText)
  const latitude = Number(latitudeText)
  return Number.isFinite(longitude) && Number.isFinite(latitude) ? [longitude, latitude] : null
}

// H5 builds read the current position from navigator.geolocation, which reports WGS-84 coordinates,
// while AMap tiles and route shapes use GCJ-02. Without conversion the current-location marker lands
// a few hundred metres off in Macau. Native app / mini-program builds already return GCJ-02 for
// uni.getLocation({ type: 'gcj02' }), so the conversion is applied on H5 only.
const GCJ_A = 6378245.0
const GCJ_EE = 0.00669342162296594323

function isOutsideChina(longitude: number, latitude: number): boolean {
  return longitude < 72.004 || longitude > 137.8347 || latitude < 0.8293 || latitude > 55.8271
}

function transformLatitude(longitude: number, latitude: number): number {
  let value = -100.0 + 2.0 * longitude + 3.0 * latitude + 0.2 * latitude * latitude + 0.1 * longitude * latitude + 0.2 * Math.sqrt(Math.abs(longitude))
  value += (20.0 * Math.sin(6.0 * longitude * Math.PI) + 20.0 * Math.sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0
  value += (20.0 * Math.sin(latitude * Math.PI) + 40.0 * Math.sin(latitude / 3.0 * Math.PI)) * 2.0 / 3.0
  value += (160.0 * Math.sin(latitude / 12.0 * Math.PI) + 320 * Math.sin(latitude * Math.PI / 30.0)) * 2.0 / 3.0
  return value
}

function transformLongitude(longitude: number, latitude: number): number {
  let value = 300.0 + longitude + 2.0 * latitude + 0.1 * longitude * longitude + 0.1 * longitude * latitude + 0.1 * Math.sqrt(Math.abs(longitude))
  value += (20.0 * Math.sin(6.0 * longitude * Math.PI) + 20.0 * Math.sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0
  value += (20.0 * Math.sin(longitude * Math.PI) + 40.0 * Math.sin(longitude / 3.0 * Math.PI)) * 2.0 / 3.0
  value += (150.0 * Math.sin(longitude / 12.0 * Math.PI) + 300.0 * Math.sin(longitude / 30.0 * Math.PI)) * 2.0 / 3.0
  return value
}

export function wgs84ToGcj02(position: AmapPosition): AmapPosition {
  const [longitude, latitude] = position
  if (isOutsideChina(longitude, latitude)) return [longitude, latitude]
  let deltaLatitude = transformLatitude(longitude - 105.0, latitude - 35.0)
  let deltaLongitude = transformLongitude(longitude - 105.0, latitude - 35.0)
  const radianLatitude = latitude / 180.0 * Math.PI
  let magic = Math.sin(radianLatitude)
  magic = 1 - GCJ_EE * magic * magic
  const sqrtMagic = Math.sqrt(magic)
  deltaLatitude = deltaLatitude * 180.0 / ((GCJ_A * (1 - GCJ_EE)) / (magic * sqrtMagic) * Math.PI)
  deltaLongitude = deltaLongitude * 180.0 / (GCJ_A / sqrtMagic * Math.cos(radianLatitude) * Math.PI)
  return [longitude + deltaLongitude, latitude + deltaLatitude]
}

function serializePosition(position: AmapPosition): string {
  return `${position[0].toFixed(6)},${position[1].toFixed(6)}`
}

// WINSON-DEVIATION: AMap Web Service APIs require the browser key in the `key` query parameter.
export function buildAmapGeocodeUrl(apiKey: string, address: string, city = '澳门'): string | null {
  if (!apiKey.trim() || !address.trim()) return null
  const parameters = new URLSearchParams({ key: apiKey.trim(), address: address.trim(), city, output: 'json' })
  return `https://restapi.amap.com/v3/geocode/geo?${parameters.toString()}`
}

export function buildAmapRegeoUrl(apiKey: string, position: AmapPosition): string | null {
  if (!apiKey.trim()) return null
  const parameters = new URLSearchParams({ key: apiKey.trim(), location: serializePosition(position), extensions: 'base', output: 'json' })
  return `https://restapi.amap.com/v3/geocode/regeo?${parameters.toString()}`
}

export function buildAmapPlaceSearchUrl(apiKey: string, keywords: string, city = '澳门'): string | null {
  if (!apiKey.trim() || !keywords.trim()) return null
  const parameters = new URLSearchParams({ key: apiKey.trim(), keywords: keywords.trim(), city, citylimit: 'false', offset: '5', page: '1', extensions: 'base', output: 'json' })
  return `https://restapi.amap.com/v3/place/text?${parameters.toString()}`
}

export function buildAmapDrivingUrl(apiKey: string, origin: AmapPosition, destination: AmapPosition): string | null {
  if (!apiKey.trim()) return null
  const parameters = new URLSearchParams({
    key: apiKey.trim(),
    origin: serializePosition(origin),
    destination: serializePosition(destination),
    strategy: '0',
    extensions: 'base',
    output: 'json'
  })
  return `https://restapi.amap.com/v3/direction/driving?${parameters.toString()}`
}

export function sampleAmapPath(path: AmapPosition[], maximumPoints = 80): AmapPosition[] {
  if (path.length <= maximumPoints) return path
  const sampled: AmapPosition[] = []
  for (let index = 0; index < maximumPoints; index += 1) {
    const sourceIndex = Math.round(index * (path.length - 1) / (maximumPoints - 1))
    const point = path[sourceIndex]
    if (point) sampled.push(point)
  }
  return sampled
}

export function buildAmapStaticMapUrl(options: AmapStaticMapOptions): string | null {
  const apiKey = options.apiKey.trim()
  if (!apiKey) return null
  const origin = serializePosition(options.origin)
  const destination = serializePosition(options.destination)
  const parameters = new URLSearchParams({
    key: apiKey,
    size: options.size ?? '750*420',
    scale: '2',
    traffic: '1',
    markers: `mid,0xF1AE2E,A:${origin}|mid,0x101820,B:${destination}`
  })
  if (options.zoom) parameters.set('zoom', String(options.zoom))
  if (options.currentPosition) {
    parameters.set('markers', `${parameters.get('markers')}|mid,0x1677FF,C:${serializePosition(options.currentPosition)}`)
  }
  const path = sampleAmapPath(options.path ?? [])
  if (path.length > 1) parameters.set('paths', `7,0x16A34A,1,,:${path.map(serializePosition).join(';')}`)
  return `https://restapi.amap.com/v3/staticmap?${parameters.toString()}`
}
