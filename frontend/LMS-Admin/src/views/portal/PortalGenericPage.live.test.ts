import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('../../api/session', () => ({ isDemoMode: false, api: mocks, loadCurrentUser: vi.fn() }))
import { useLocale } from '../../i18n'
import PortalGenericPage from './PortalGenericPage.vue'

describe('live Portal source routing', () => {
  beforeEach(() => { mocks.request.mockReset(); mocks.requestEnvelope.mockReset() })
  it('loads the documented travel registry, navigation group and detail endpoint', async () => {
    useLocale().setLocale('en')
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/portal/:section', component: PortalGenericPage }] })
    await router.push('/portal/travel')
    await router.isReady()
    mocks.requestEnvelope.mockImplementation(async (path: string) => path.includes('travel-time-indices')
      ? { data: [{ routeCategoryCode: 'AIRPORT', routeCategory: 'Airport transfer', bufferMinutes: 20, navigationProvider: 'GOOGLE_MAPS', isTrafficAwareEtaEnabled: true, applicationRule: 'Before pickup' }], meta: { pagination: { totalItems: 1 } } }
      : { data: [{ integrationCode: 'GOOGLE_MAPS', name: 'Google Maps', configuredStatus: 'CONFIGURED', purpose: 'Navigation', dataSource: 'External' }] })
    mocks.request.mockResolvedValue({ integrationCode: 'GOOGLE_MAPS', name: 'Google Maps', configuredStatus: 'CONFIGURED', purpose: 'Navigation', dataSource: 'External' })

    const wrapper = mount(PortalGenericPage, { props: { section: 'travel' }, global: { plugins: [pinia, router] } })
    await flushPromises()
    expect(mocks.requestEnvelope).toHaveBeenCalledWith('/api/platform/staff/v1/travel-time-indices?page=1&pageSize=10', { signal: expect.any(AbortSignal) })
    expect(wrapper.get('.portal-table tbody').text()).toContain('Airport transfer')

    await wrapper.findAll('.tabs button')[1]!.trigger('click')
    await flushPromises()
    expect(mocks.requestEnvelope).toHaveBeenLastCalledWith('/api/platform/staff/v1/integration-references?group=NAVIGATION', { signal: expect.any(AbortSignal) })
    await wrapper.get('.portal-table .table-actions button').trigger('click')
    await flushPromises()
    expect(mocks.request).toHaveBeenCalledWith('/api/platform/staff/v1/integration-references/GOOGLE_MAPS')
    wrapper.unmount()
  })

  it('creates a customer restriction using the Platform blacklist contract', async () => {
    useLocale().setLocale('en')
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/portal/:section', component: PortalGenericPage }] })
    await router.push('/portal/blacklist')
    await router.isReady()
    mocks.requestEnvelope.mockResolvedValue({ data: [], meta: { pagination: { totalItems: 0 } } })
    mocks.request.mockResolvedValue({ blacklistId: 'blacklist_1' })
    const wrapper = mount(PortalGenericPage, { props: { section: 'blacklist' }, global: { plugins: [pinia, router] } })
    await flushPromises()
    await wrapper.get('.panel-heading .primary-button').trigger('click')
    const inputs = wrapper.findAll('.dialog-body input')
    await inputs[0]!.setValue('Guest Example')
    await inputs[1]!.setValue('MB-123')
    await inputs[2]!.setValue('Reviewed restriction')
    await inputs[3]!.setValue('2026-10-12')
    await wrapper.get('.dialog-footer .primary-button').trigger('click')
    await flushPromises()
    const [path, init] = mocks.request.mock.calls[0] as [string, RequestInit]
    expect(path).toBe('/api/platform/staff/v1/customer-blacklists')
    expect(init.method).toBe('POST')
    expect(new Headers(init.headers).get('Idempotency-Key')).toBeTruthy()
    expect(JSON.parse(String(init.body))).toMatchObject({ customerName: 'Guest Example', memberId: 'MB-123', isActive: true })
    wrapper.unmount()
  })
})
