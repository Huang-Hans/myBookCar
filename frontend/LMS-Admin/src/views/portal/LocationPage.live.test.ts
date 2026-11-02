import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('../../api/session', () => ({ isDemoMode: false, api: mocks }))
import LocationPage from './LocationPage.vue'
import { useLocale } from '../../i18n'

const location = {
  locationId: 'loc_001', name: 'City of Dreams Lobby', address: 'Cotai', coordinates: { latitude: 22.149, longitude: 113.566 },
  region: 'COTAI', categoryCode: 'LC-004', contactPerson: 'Concierge desk', contactPhone: '+853 8868 8800',
  operatingHours: '24 hours', geofence: { isEnabled: true, radiusMeters: 100 }, isActive: true, notes: 'Pickup entrance', sortOrder: 1, version: 3,
}

async function page() {
  const pinia = createPinia()
  setActivePinia(pinia)
  const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/portal/locations', component: LocationPage }] })
  await router.push('/portal/locations')
  await router.isReady()
  const wrapper = mount(LocationPage, { global: { plugins: [pinia, router] } })
  await flushPromises()
  return wrapper
}

describe('live Portal locations', () => {
  beforeEach(() => {
    useLocale().setLocale('en')
    mocks.request.mockReset()
    mocks.requestEnvelope.mockReset()
    mocks.request.mockImplementation(async (path: string) => path.includes('location-categories') ? [{ categoryCode: 'LC-004', name: 'Hotel', isActive: true }] : location)
    mocks.requestEnvelope.mockResolvedValue({ data: [location], meta: { pagination: { totalItems: 1, totalPages: 1 } } })
  })

  it('renders Figma table data and opens the right-side location details', async () => {
    const wrapper = await page()
    expect(mocks.requestEnvelope).toHaveBeenCalledWith('/api/platform/staff/v1/locations?page=1&pageSize=10&sort=region%2CsortOrder%2ClocationId', { signal: expect.any(AbortSignal) })
    expect(wrapper.findAll('.location-table thead th')).toHaveLength(11)
    expect(wrapper.get('.location-table tbody').text()).toContain('Hotel')
    await wrapper.get('.location-table .table-actions button:first-child').trigger('click')
    await flushPromises()
    expect(mocks.request).toHaveBeenCalledWith('/api/platform/staff/v1/locations/loc_001')
    expect(wrapper.get('.location-details-overlay [role="dialog"]').attributes('aria-label')).toBe('City of Dreams Lobby')
    expect(wrapper.get('.location-details-overlay dl').text()).toContain('Pickup entrance')
    wrapper.unmount()
  })

  it('PATCHes only edited fields with the location version', async () => {
    const wrapper = await page()
    await wrapper.get('.location-table .table-actions button:nth-child(2)').trigger('click')
    await flushPromises()
    await wrapper.get('.location-dialog .location-form-grid input').setValue('Updated Lobby')
    await wrapper.get('.location-dialog .dialog-footer .primary-button').trigger('click')
    await flushPromises()
    const patchCall = mocks.request.mock.calls.find((call: unknown[]) => call[0] === '/api/platform/staff/v1/locations/loc_001' && (call[1] as RequestInit | undefined)?.method === 'PATCH') as [string, RequestInit] | undefined
    expect(patchCall).toBeDefined()
    expect(new Headers(patchCall?.[1].headers).get('X-Expected-Version')).toBe('3')
    expect(JSON.parse(String(patchCall?.[1].body))).toEqual({ name: 'Updated Lobby' })
    wrapper.unmount()
  })

  it('validates the Figma add form and creates a location with the documented payload', async () => {
    const wrapper = await page()
    await wrapper.get('.location-panel .primary-button').trigger('click')
    await wrapper.get('.location-dialog .dialog-footer .primary-button').trigger('click')
    expect(wrapper.get('.location-dialog .form-error').text()).toContain('Complete all required location fields.')
    const inputs = wrapper.findAll('.location-form-grid input')
    await inputs[0]!.setValue('New pickup')
    await inputs[1]!.setValue('Cotai entrance')
    await inputs[2]!.setValue('22.1500')
    await inputs[3]!.setValue('113.5600')
    await inputs[4]!.setValue('Concierge desk')
    await inputs[5]!.setValue('+853 8868 8888')
    await inputs[6]!.setValue('24 hours')
    await inputs[7]!.setValue('100')
    await wrapper.get('.location-dialog .dialog-footer .primary-button').trigger('click')
    await flushPromises()
    const createCall = mocks.request.mock.calls.find((call: unknown[]) => call[0] === '/api/platform/staff/v1/locations' && (call[1] as RequestInit | undefined)?.method === 'POST') as [string, RequestInit] | undefined
    expect(createCall).toBeDefined()
    expect(new Headers(createCall?.[1].headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(createCall?.[1].body))).toMatchObject({ name: 'New pickup', region: 'MACAU_PENINSULA', categoryCode: 'LC-004', coordinates: { latitude: 22.15, longitude: 113.56 }, geofence: { isEnabled: true, radiusMeters: 100 } })
    wrapper.unmount()
  })

  it('updates the geofence switch through a partial PATCH', async () => {
    const wrapper = await page()
    await wrapper.get('.location-table .location-toggle input').setValue(false)
    await flushPromises()
    const patchCall = mocks.request.mock.calls.find((call: unknown[]) => call[0] === '/api/platform/staff/v1/locations/loc_001' && (call[1] as RequestInit | undefined)?.method === 'PATCH') as [string, RequestInit] | undefined
    expect(JSON.parse(String(patchCall?.[1].body))).toEqual({ geofence: { isEnabled: false } })
    wrapper.unmount()
  })
})
