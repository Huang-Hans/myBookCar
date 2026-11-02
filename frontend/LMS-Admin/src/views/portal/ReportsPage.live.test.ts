import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({ request: vi.fn(), requestEnvelope: vi.fn() }))
vi.mock('../../api/session', () => ({ isDemoMode: false, api: mocks, loadCurrentUser: vi.fn() }))
import { useLocale } from '../../i18n'
import ReportsPage from './ReportsPage.vue'

describe('live Portal reporting', () => {
  it('uses the daily summary route with Macau date boundaries and server pagination', async () => {
    useLocale().setLocale('en')
    mocks.request.mockReset()
    mocks.requestEnvelope.mockReset()
    mocks.request.mockImplementation(async (path: string) => path.startsWith('/api/platform/staff/v1/properties') ? [{ propertyId: 'COD', name: 'City of Dreams' }] : [])
    mocks.requestEnvelope.mockResolvedValue({ data: [{ propertyId: 'COD', bookings: 2, completed: 1, cancellations: 0, noShows: 1, updatedAt: '2026-10-12T08:00:00Z' }], meta: { pagination: { totalItems: 23 } } })
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({ history: createMemoryHistory(), routes: [{ path: '/portal/reports', component: ReportsPage }] })
    await router.push('/portal/reports')
    await router.isReady()
    const wrapper = mount(ReportsPage, { global: { plugins: [pinia, router] } })
    await flushPromises()
    await wrapper.get('.portal-filter select').setValue('City of Dreams')
    await wrapper.findAll('.portal-filter input[type="date"]')[0]!.setValue('2026-10-12')
    await wrapper.findAll('.portal-filter input[type="date"]')[1]!.setValue('2026-10-12')
    await wrapper.get('.report-card .primary-button').trigger('click')
    await flushPromises()
    const path = mocks.requestEnvelope.mock.calls[0]?.[0] as string
    const url = new URL(path, 'https://local.example')
    expect(url.pathname).toBe('/api/reporting/staff/v1/reports/daily-job-summaries')
    expect(Object.fromEntries(url.searchParams)).toMatchObject({ page: '1', pageSize: '10', propertyId: 'COD', from: '2026-10-11T16:00:00Z', to: '2026-10-12T16:00:00Z' })
    expect(wrapper.get('.report-preview .data-pager').text()).toContain('23')
    wrapper.unmount()
  })
})
