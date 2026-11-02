import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  properties: vi.fn(), listDispatchOrderPage: vi.fn(), dispatchOrderSummaries: vi.fn(),
  dispatchOrder: vi.fn(), listDispatchOffers: vi.fn(), createDispatchOffer: vi.fn(), shiftBoard: vi.fn(),
}))
vi.mock('../../api/session', () => ({ isDemoMode: false }))
vi.mock('../../api/lms', () => ({
  lms: mocks,
  mapTrip: () => ({ propertyName: 'City of Dreams', status: 'Confirmed' }),
}))
vi.mock('../../api/dispatch', () => ({ dispatchApi: { shiftBoard: mocks.shiftBoard } }))
vi.mock('../../api/notifications', () => ({
  subscribeBookingCreated: () => () => undefined,
  subscribeStaffNotificationConnected: () => () => undefined,
}))
vi.mock('vue-router', () => ({ useRoute: () => ({ query: {} }) }))

import ConsoleDispatchPage from './ConsoleDispatchPage.vue'

const order = {
  tripId: 'trip_1', tripReference: 'TR-20261022-1', propertyId: 'COD', channel: 'SELF_SERVICE',
  guest: { name: 'Wei Chen', roomNumber: '2718', tierCode: 'GOLD', memberId: 'MB-31050' },
  route: { pickupName: 'City of Dreams', dropoffName: 'Macau Airport', isCrossBorder: false },
  pickupAt: '2026-10-21T18:00:00Z', serviceType: 'ONE_WAY', vehicleClass: 'LUXURY_MPV',
  passengers: 3, luggage: 0, orderStatus: 'CONFIRMED', dispatchStatus: 'PENDING',
  createdAt: '2026-10-20T10:00:00Z', canDispatch: true, version: 2,
}

describe('dispatch order driver selection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.properties.mockResolvedValue([{ propertyId: 'COD', name: 'City of Dreams' }])
    mocks.listDispatchOrderPage.mockResolvedValue({ data: [order], meta: { pagination: { currentPage: 1, pageSize: 10, totalItems: 1, totalPages: 1 } } })
    mocks.dispatchOrderSummaries.mockResolvedValue({ total: 1, counts: {} })
    mocks.dispatchOrder.mockResolvedValue(order)
    mocks.listDispatchOffers.mockResolvedValue({ data: [] })
    mocks.shiftBoard.mockResolvedValue({ timeZone: 'Asia/Macau', businessDate: '2026-10-22', propertyId: 'COD', periods: [{ period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, count: 1, shifts: [{ shiftId: 'shift_1', businessDate: '2026-10-22', period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, driverId: 'driver_1', driverName: 'Alex Chen', propertyId: 'COD', vehiclePlate: 'MP-28-61', status: 'ON_DUTY', role: 'PREFERRED', attendance: 'ON_TIME', badges: [], version: 1 }] }] })
  })

  it('loads shift-board drivers for the trip date and property', async () => {
    const page = mount(ConsoleDispatchPage)
    await flushPromises()
    await page.find('.dispatch-action').trigger('click')
    await flushPromises()
    expect(mocks.shiftBoard).toHaveBeenCalledWith('2026-10-22', 'COD')
    expect(page.find('.dispatch-field select').text()).toContain('Alex Chen')
    expect(page.find('.dispatch-field select').text()).toContain('MP-28-61')
    expect(page.find('.dispatch-drawer footer .primary-button').attributes('disabled')).toBeUndefined()
    page.unmount()
  })

  it('shows a shift-board failure without enabling invitation', async () => {
    mocks.shiftBoard.mockRejectedValueOnce(new Error('Shift service unavailable'))
    const page = mount(ConsoleDispatchPage)
    await flushPromises()
    await page.find('.dispatch-action').trigger('click')
    await flushPromises()
    expect(page.find('.dispatch-drawer .form-error').text()).toContain('Shift service unavailable')
    expect(page.find('.dispatch-drawer footer .primary-button').attributes('disabled')).toBeDefined()
    page.unmount()
  })
})
