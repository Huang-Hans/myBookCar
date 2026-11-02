export const TRIP_MAP_ROUTE_COLOR = '#16A34A'

const VEHICLE_SVG = '<svg viewBox="0 0 30 54" width="20" height="36" aria-hidden="true">'
  + '<defs><linearGradient id="tripMapVehicleBody" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#5a62e6"/><stop offset="1" stop-color="#2c3390"/></linearGradient></defs>'
  + '<path d="M15 2C22.5 2 25.5 8 25.5 17v21c0 9-3.2 14-10.5 14S4.5 47 4.5 38V17C4.5 8 7.5 2 15 2z" fill="url(#tripMapVehicleBody)" stroke="#fff" stroke-width="2"/>'
  + '<path d="M8.4 19c1.8-3.4 11.4-3.4 13.2 0l-1.5 6.5H9.9z" fill="#e3eeff"/>'
  + '<path d="M9.6 37h10.8l.9 4c-2.6 2-10.4 2-12.6 0z" fill="#e3eeff" opacity="0.85"/>'
  + '<rect x="9.8" y="27.5" width="10.4" height="8" rx="2" fill="#6a72f0" opacity="0.55"/>'
  + '<path d="M8.2 7.4c2-1.2 3.9-1.4 5.3-1.2M21.8 7.4c-2-1.2-3.9-1.4-5.3-1.2" stroke="#fff3b0" stroke-width="1.8" stroke-linecap="round" fill="none"/>'
  + '</svg>'

export function tripStopMarkerContent(kind: 'pickup' | 'dropoff'): string {
  return `<span class="track-stop track-stop-${kind}">${kind === 'pickup' ? 'A' : 'B'}</span>`
}

export function tripVehicleMarkerContent(headingDegrees?: number | null): string {
  const angle = headingDegrees == null || !Number.isFinite(headingDegrees)
    ? null
    : ((headingDegrees % 360) + 360) % 360
  const rotation = angle === null ? '' : ` style="transform:rotate(${angle}deg)"`
  return `<span class="track-vehicle"${rotation}>${VEHICLE_SVG}</span>`
}
