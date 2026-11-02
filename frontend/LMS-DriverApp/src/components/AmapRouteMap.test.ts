// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import AmapRouteMap from './AmapRouteMap.vue'
import { t } from '@/i18n/locale'
import homeSource from '../pages/home/index.vue?raw'

const requestMock = vi.fn()
const getLocationMock = vi.fn()

vi.stubGlobal('uni', { request: requestMock, getLocation: getLocationMock })

class MapStub {
  static instances: MapStub[] = []
  added: unknown[] = []
  center: unknown = null
  zoom = 0
  fitPadding: number[] | undefined
  constructor(public container: HTMLElement, public options: Record<string, unknown>) { MapStub.instances.push(this) }
  add(overlays: unknown[]): void { this.added.push(...overlays) }
  destroy(): void {}
  setFitView(_overlays: unknown[], _immediately?: boolean, padding?: number[]): void { this.fitPadding = padding }
  zoomIn(): void {}
  zoomOut(): void {}
  setCenter(position: unknown): void { this.center = position }
  setZoom(zoom: number): void { this.zoom = zoom }
}

class MarkerStub { constructor(public options: Record<string, unknown>) {} }
class PolylineStub { constructor(public options: Record<string, unknown>) {} }

const amapNamespace = { Map: MapStub, Marker: MarkerStub, Polyline: PolylineStub }
const amapWindow = window as unknown as { AMap?: unknown }

function mockAmapRequests(): void {
  requestMock.mockImplementation(({ url, success }: { url: string; success: (response: { statusCode: number; data: unknown }) => void }) => {
    if (url.includes('/place/text')) return success({ statusCode: 200, data: { status: '1', pois: [{ location: '113.566000,22.149000' }] } })
    if (url.includes('/direction/driving')) return success({ statusCode: 200, data: { status: '1', route: { paths: [{ distance: '4092', duration: '600', steps: [{ polyline: '113.566,22.149;113.554,22.139' }] }] } } })
    if (url.includes('/geocode/regeo')) return success({ statusCode: 200, data: { status: '1', regeocode: { formatted_address: '澳门特别行政区路氹填海区' } } })
    return success({ statusCode: 200, data: { status: '0' } })
  })
}

async function mountMap(): Promise<ReturnType<typeof mount<typeof AmapRouteMap>>> {
  const wrapper = mount(AmapRouteMap, {
    props: { apiKey: 'test-key', origin: 'City of Dreams', destination: 'Grand Hyatt Macau', isInteractive: true },
    global: { mocks: { $t: t } },
  })
  await flushPromises()
  return wrapper
}

describe('AMap route map', () => {
  beforeEach(() => {
    requestMock.mockReset()
    getLocationMock.mockReset()
    MapStub.instances = []
    mockAmapRequests()
    amapWindow.AMap = amapNamespace
  })

  afterEach(() => {
    vi.useRealTimers()
    document.querySelectorAll('script[src*="webapi.amap.com/maps"]').forEach((script) => script.remove())
  })

  it('renders the interactive map with trip addresses even while geolocation is still pending', async () => {
    vi.useFakeTimers()
    getLocationMock.mockImplementation(() => { /* Some WebViews never answer the permission prompt. */ })
    const wrapper = await mountMap()

    expect(MapStub.instances).toHaveLength(1)
    expect(wrapper.find('.map-address-panel').text()).toContain('City of Dreams')
    expect(wrapper.find('.map-address-panel').text()).toContain('Grand Hyatt Macau')
    expect(wrapper.find('.map-address-panel').text()).not.toContain('22.14900, 113.56600')
    expect(MapStub.instances[0]?.options).not.toHaveProperty('mapStyle')
    expect(wrapper.text()).not.toContain('Locating…')
    expect(wrapper.findAll('.map-address-panel .address-row')).toHaveLength(2)
    expect(wrapper.find('.map-address-panel').text()).toContain('Pickup Point')
    expect(wrapper.find('.map-address-panel').text()).toContain('Drop-Off Point')
    expect(wrapper.find('.map-address-panel').text()).not.toContain('Current location')
    expect(wrapper.findAll('.map-controls button')).toHaveLength(2)
    expect(wrapper.emitted('metrics')?.[0]?.[0]).toMatchObject({ hasCurrentPosition: false, etaMinutes: 0, distanceToPickupKm: 0 })

    await vi.advanceTimersByTimeAsync(10_000)
    await flushPromises()
    expect(wrapper.text()).not.toContain('Unavailable')
    expect(wrapper.text()).not.toContain('Locating…')
    wrapper.unmount()
  })

  it('shows a compact address panel on the home map without reserving the full panel padding', async () => {
    getLocationMock.mockImplementation(({ fail }: { fail: () => void }) => fail())
    const wrapper = mount(AmapRouteMap, {
      props: { apiKey: 'test-key', origin: 'City of Dreams', destination: 'Grand Hyatt Macau', isInteractive: true, isCompactPreview: true },
      global: { mocks: { $t: t } },
    })
    await flushPromises()

    expect(homeSource).toContain('is-compact-preview is-interactive')
    expect(homeSource).not.toContain(':show-address-panel="false"')
    expect(wrapper.find('.map-address-panel.compact').exists()).toBe(true)
    expect(wrapper.findAll('.map-address-panel .address-row')).toHaveLength(2)
    expect(wrapper.findAll('.map-controls button')).toHaveLength(2)
    expect(MapStub.instances[0]?.fitPadding).toEqual([24, 24, 24, 24])
    expect(MapStub.instances[0]?.added.filter((overlay) => overlay instanceof MarkerStub)).toHaveLength(2)
    wrapper.unmount()
  })

  it('adds the current-position marker, address and refreshed metrics once GPS resolves', async () => {
    getLocationMock.mockImplementation(({ success }: { success: (value: { longitude: number; latitude: number; speed: number }) => void }) => success({ longitude: 113.555, latitude: 22.145, speed: 8 }))
    const wrapper = await mountMap()
    await flushPromises()

    expect(wrapper.text()).toContain('22.14500, 113.55500')
    expect(wrapper.text()).not.toContain('Locating…')
    const map = MapStub.instances[0]!
    const markers = map.added.filter((overlay) => overlay instanceof MarkerStub).map((overlay) => (overlay as MarkerStub).options)
    expect(markers).toHaveLength(3)
    expect(markers[0]?.content).toContain('track-stop-pickup')
    expect(markers[1]?.content).toContain('track-stop-dropoff')
    expect(markers[2]).toMatchObject({ position: [113.555, 22.145] })
    expect(markers[2]?.content).toContain('track-vehicle')
    await wrapper.findAll('.map-address-panel .address-row')[2]?.trigger('tap')
    expect(map.center).toEqual([113.555, 22.145])
    expect(map.zoom).toBe(16)
    const metrics = wrapper.emitted('metrics')?.map((args) => args[0])
    expect(metrics).toHaveLength(2)
    expect(metrics?.[1]).toMatchObject({ hasCurrentPosition: true, distanceToPickupKm: 4.092, speedKmh: 28.8 })
    wrapper.unmount()
  })

  it('initialises the interactive SDK from the script load event without a JSONP callback', async () => {
    delete amapWindow.AMap
    getLocationMock.mockImplementation(({ fail }: { fail: () => void }) => fail())
    const wrapper = await mountMap()
    const script = document.querySelector<HTMLScriptElement>('script[src*="webapi.amap.com/maps"]')
    expect(script).not.toBeNull()
    expect(script?.src).not.toContain('callback=')

    amapWindow.AMap = amapNamespace
    script?.dispatchEvent(new Event('load'))
    await flushPromises()

    expect(MapStub.instances).toHaveLength(1)
    expect(wrapper.find('.amap-canvas').exists()).toBe(true)
    expect(wrapper.find('image').exists()).toBe(false)
    wrapper.unmount()
  })

  it('falls back to an interactive AMap tile layer when the SDK cannot initialise', async () => {
    vi.useFakeTimers()
    delete amapWindow.AMap
    getLocationMock.mockImplementation(({ fail }: { fail: () => void }) => fail())
    const wrapper = await mountMap()
    // Without the SDK the loader waits 15 seconds before degrading.
    await vi.advanceTimersByTimeAsync(16_000)
    await flushPromises()

    expect(MapStub.instances).toHaveLength(0)
    expect(wrapper.find('.amap-canvas').exists()).toBe(true)
    expect(wrapper.find('.leaflet-container').exists()).toBe(true)
    expect(wrapper.findAll('.track-stop')).toHaveLength(2)
    expect(wrapper.find('.track-stop-pickup').text()).toBe('A')
    expect(wrapper.find('.track-stop-dropoff').text()).toBe('B')
    expect(wrapper.find('image').exists()).toBe(false)
    expect(wrapper.findAll('.map-controls button')).toHaveLength(2)
    expect(wrapper.find('.map-address-panel').text()).toContain('City of Dreams')
    expect(wrapper.find('.map-address-panel').text()).toContain('Grand Hyatt Macau')
    expect(wrapper.find('.map-address-panel').text()).not.toContain('22.14900, 113.56600')
    wrapper.unmount()
  })
})
