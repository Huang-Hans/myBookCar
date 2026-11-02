import type { AmapPosition } from './amap'

const EARTH_RADIUS_METERS = 6_371_000
const ROUTE_AREA_RADIUS_METERS = 50_000

export function distanceMeters(from: AmapPosition, to: AmapPosition): number {
  const radians = (degrees: number): number => degrees * Math.PI / 180
  const latitudeDelta = radians(to[1] - from[1])
  const longitudeDelta = radians(to[0] - from[0])
  const value = Math.sin(latitudeDelta / 2) ** 2
    + Math.cos(radians(from[1])) * Math.cos(radians(to[1])) * Math.sin(longitudeDelta / 2) ** 2
  return 2 * EARTH_RADIUS_METERS * Math.asin(Math.min(1, Math.sqrt(value)))
}

export function isDriverNearRoute(driver: AmapPosition, origin: AmapPosition, destination: AmapPosition): boolean {
  return Math.min(distanceMeters(driver, origin), distanceMeters(driver, destination)) <= ROUTE_AREA_RADIUS_METERS
}
