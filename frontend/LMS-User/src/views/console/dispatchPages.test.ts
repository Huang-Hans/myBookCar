import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  coupons: vi.fn(), couponSummary: vi.fn(), vehicles: vi.fn(), vehicleSummary: vi.fn(), shiftBoard: vi.fn(),
  properties: vi.fn(), listDrivers: vi.fn(),
}))
vi.mock('../../api/dispatch', () => ({ dispatchApi: mocks }))
vi.mock('../../api/lms', () => ({ lms: { properties: mocks.properties, listDrivers: mocks.listDrivers } }))
vi.mock('../../stores/fleet', () => ({ macauDayKey: () => '2026-10-19' }))

import ConsoleCouponsPage from './ConsoleCouponsPage.vue'
import ConsoleVehiclesPage from './ConsoleVehiclesPage.vue'
import ConsoleRosterPage from './ConsoleRosterPage.vue'

describe('staff Dispatch pages', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.properties.mockResolvedValue([{ propertyId: 'COD', name: 'City of Dreams' }])
    mocks.listDrivers.mockResolvedValue({ data: [] })
    mocks.coupons.mockResolvedValue({ data: [], meta: { pagination: { currentPage: 1, pageSize: 20, totalItems: 0, totalPages: 0 } } })
    mocks.couponSummary.mockResolvedValue({ active: 0, issuedQuantity: 0, redeemedCount: 0, upcoming: 0 })
    mocks.vehicles.mockResolvedValue({ data: [], meta: { pagination: { currentPage: 1, pageSize: 20, totalItems: 0, totalPages: 0 } } })
    mocks.vehicleSummary.mockResolvedValue({ available: 0, onTrip: 0, maintenance: 0, outOfService: 0 })
    mocks.shiftBoard.mockResolvedValue({ timeZone: 'Asia/Macau', businessDate: '2026-10-19', propertyId: null, periods: [] })
  })

  it('renders live coupon data and surfaces a service failure', async () => {
    mocks.coupons.mockResolvedValueOnce({ data: [{ couponId: 'c_1', code: 'WELCOME100', name: 'Welcome', memberId: 'm_1', type: 'FREE_TRIP', faceAmount: 1, scope: 'ALL_PROPERTIES', propertyId: null, vehicleRestriction: null, validFrom: '2026-10-01', validTo: '2026-12-31', issuedQuantity: 10, redeemedCount: 2, status: 'ACTIVE', version: 1 }] })
    const page = mount(ConsoleCouponsPage)
    await flushPromises()
    expect(page.text()).toContain('WELCOME100')
    expect(mocks.coupons).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 20 }))
    page.unmount()
    mocks.coupons.mockRejectedValueOnce(new Error('Coupon service unavailable'))
    const failed = mount(ConsoleCouponsPage)
    await flushPromises()
    expect(failed.text()).toContain('Coupon service unavailable')
    failed.unmount()
  })

  it('renders live vehicle data and surfaces a service failure', async () => {
    mocks.vehicles.mockResolvedValueOnce({ data: [{ vehicleId: 'v_1', plate: 'MP-28-61', model: 'Alphard', grade: 'LUXURY_MPV', propertyId: 'COD', seats: 6, mileageKm: 100, status: 'AVAILABLE', driverId: null, driverName: null, version: 1 }] })
    const page = mount(ConsoleVehiclesPage)
    await flushPromises()
    expect(page.text()).toContain('MP-28-61')
    expect(mocks.vehicles).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 20 }))
    page.unmount()
    mocks.vehicles.mockRejectedValueOnce(new Error('Vehicle service unavailable'))
    const failed = mount(ConsoleVehiclesPage)
    await flushPromises()
    expect(failed.text()).toContain('Vehicle service unavailable')
    failed.unmount()
  })

  it('renders shifts from the board and surfaces a service failure', async () => {
    mocks.shiftBoard.mockResolvedValueOnce({ timeZone: 'Asia/Macau', businessDate: '2026-10-19', propertyId: null, periods: [{ period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, count: 1, shifts: [{ shiftId: 's_1', businessDate: '2026-10-19', period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, driverId: 'd_1', driverName: 'Alex Chen', propertyId: 'COD', vehiclePlate: null, status: 'ON_DUTY', role: 'PREFERRED', attendance: null, badges: [], version: 1 }] }] })
    const page = mount(ConsoleRosterPage)
    await flushPromises()
    expect(page.text()).toContain('Alex Chen')
    expect(mocks.shiftBoard).toHaveBeenCalledWith('2026-10-19', undefined)
    page.unmount()
    mocks.shiftBoard.mockRejectedValueOnce(new Error('Shift service unavailable'))
    const failed = mount(ConsoleRosterPage)
    await flushPromises()
    expect(failed.text()).toContain('Shift service unavailable')
    failed.unmount()
  })

  it('shows a monthly roster and loads historical dates when navigating', async () => {
    mocks.shiftBoard.mockImplementation(async (date: string) => ({ timeZone: 'Asia/Macau', businessDate: date, propertyId: null, periods: date === '2026-10-19' ? [{ period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, count: 1, shifts: [{ shiftId: 's_1', businessDate: date, period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, driverId: 'd_1', driverName: 'Alex Chen', propertyId: 'COD', vehiclePlate: null, status: 'ON_DUTY', role: 'PREFERRED', attendance: null, badges: [], version: 1 }] }] : [] }))
    const page = mount(ConsoleRosterPage)
    await flushPromises()
    expect(page.findAll('.roster-calendar-day')).toHaveLength(31)
    expect(page.find('.roster-calendar-day[aria-label="2026-10-19"]').text()).toContain('Alex Chen')
    await page.find('[aria-label="Previous month"]').trigger('click')
    await flushPromises()
    expect(page.findAll('.roster-calendar-day')).toHaveLength(30)
    expect(mocks.shiftBoard).toHaveBeenCalledWith('2026-09-01', undefined)
    await page.find('.roster-calendar-day[aria-label="2026-09-12"]').trigger('click')
    expect(page.find('.roster-selected-heading').text()).toContain('12')
    page.unmount()
  })

  it('loads roster choices using permitted page sizes and follows pagination', async () => {
    const firstPage = { currentPage: 1, pageSize: 50, totalItems: 51, totalPages: 2 }
    const secondPage = { currentPage: 2, pageSize: 50, totalItems: 51, totalPages: 2 }
    mocks.listDrivers
      .mockResolvedValueOnce({ data: [{ driverId: 'd_1', name: 'First driver', status: 'ACTIVE', propertyId: 'COD' }], meta: { pagination: firstPage } })
      .mockResolvedValueOnce({ data: [{ driverId: 'd_2', name: 'Second driver', status: 'ACTIVE', propertyId: 'COD' }], meta: { pagination: secondPage } })
    mocks.vehicles
      .mockResolvedValueOnce({ data: [{ vehicleId: 'v_1', plate: 'MP-11-11', status: 'AVAILABLE', propertyId: 'COD' }], meta: { pagination: firstPage } })
      .mockResolvedValueOnce({ data: [{ vehicleId: 'v_2', plate: 'MP-22-22', status: 'AVAILABLE', propertyId: 'COD' }], meta: { pagination: secondPage } })
    const page = mount(ConsoleRosterPage)
    await flushPromises()
    expect(mocks.listDrivers.mock.calls.map((call) => call[0])).toEqual([{ page: 1, pageSize: 50 }, { page: 2, pageSize: 50 }])
    expect(mocks.vehicles.mock.calls.map((call) => call[0])).toEqual([{ page: 1, pageSize: 50 }, { page: 2, pageSize: 50 }])
    page.unmount()
  })

  it.each([
    ['coupons', ConsoleCouponsPage],
    ['vehicles', ConsoleVehiclesPage],
    ['roster', ConsoleRosterPage],
  ])('keeps the %s creation dialog open on backdrop interaction and closes with the header button', async (_name, component) => {
    const page = mount(component)
    await flushPromises()
    await page.find('.panel-heading .primary-button').trigger('click')
    expect(page.find('.modal-backdrop .dialog').exists()).toBe(true)

    await page.find('.modal-backdrop').trigger('wheel')
    await page.find('.modal-backdrop').trigger('click')
    expect(page.find('.modal-backdrop .dialog').exists()).toBe(true)

    await page.find('.dialog-header .icon-button').trigger('click')
    expect(page.find('.modal-backdrop').exists()).toBe(false)
    page.unmount()
  })
})
