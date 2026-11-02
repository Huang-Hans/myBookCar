import { beforeEach, describe, expect, it, vi } from 'vitest'
import { driverApi } from './driver'
import { fetchUpcomingDriverShifts, groupDriverShifts, macauBusinessDate, nextBusinessDates } from './driverShifts'

vi.mock('./driver', () => ({ driverApi: { listShiftsForDriver: vi.fn() } }))

const listShiftsForDriver = vi.mocked(driverApi.listShiftsForDriver)

describe('driver shifts', () => {
  beforeEach(() => { listShiftsForDriver.mockReset() })

  it('uses the Macau business date and includes exactly seven consecutive dates', () => {
    expect(macauBusinessDate(new Date('2026-10-21T16:30:00Z'))).toBe('2026-10-22')
    expect(nextBusinessDates('2026-12-29')).toEqual([
      '2026-12-29', '2026-12-30', '2026-12-31', '2027-01-01',
      '2027-01-02', '2027-01-03', '2027-01-04',
    ])
  })

  it('groups only shifts within the requested seven-day window', () => {
    const days = groupDriverShifts([
      { shiftId: 'old', businessDate: '2026-10-21' },
      { shiftId: 'first', businessDate: '2026-10-22' },
      { shiftId: 'last', businessDate: '2026-10-28' },
      { shiftId: 'later', businessDate: '2026-10-29' },
    ], '2026-10-22')
    expect(days).toHaveLength(7)
    expect(days[0]?.shifts.map((shift) => shift.shiftId)).toEqual(['first'])
    expect(days[6]?.shifts.map((shift) => shift.shiftId)).toEqual(['last'])
    expect(days.flatMap((day) => day.shifts)).toHaveLength(2)
  })

  it('paginates until the past is reached and returns all upcoming shifts', async () => {
    listShiftsForDriver
      .mockResolvedValueOnce({ code: 20000, message: 'success', data: [{ shiftId: 'later', businessDate: '2026-10-29' }], traceId: 'tr_1', meta: { traceId: 'tr_1', pagination: { currentPage: 1, pageSize: 50, totalItems: 3, totalPages: 3 } } })
      .mockResolvedValueOnce({ code: 20000, message: 'success', data: [{ shiftId: 'today', businessDate: '2026-10-22' }], traceId: 'tr_2', meta: { traceId: 'tr_2', pagination: { currentPage: 2, pageSize: 50, totalItems: 3, totalPages: 3 } } })
      .mockResolvedValueOnce({ code: 20000, message: 'success', data: [{ shiftId: 'past', businessDate: '2026-10-21' }], traceId: 'tr_3', meta: { traceId: 'tr_3', pagination: { currentPage: 3, pageSize: 50, totalItems: 3, totalPages: 3 } } })
    const days = await fetchUpcomingDriverShifts('driver_01', '2026-10-22')
    expect(listShiftsForDriver).toHaveBeenCalledTimes(3)
    expect(listShiftsForDriver).toHaveBeenNthCalledWith(1, 'driver_01', 1)
    expect(listShiftsForDriver).toHaveBeenNthCalledWith(3, 'driver_01', 3)
    expect(days.flatMap((day) => day.shifts.map((shift) => shift.shiftId))).toEqual(['today'])
  })

  it('propagates an authorization failure rather than showing an empty schedule', async () => {
    listShiftsForDriver.mockImplementation(async () => { throw new Error('Forbidden') })
    let failure: unknown
    try { await fetchUpcomingDriverShifts('driver_01', '2026-10-22') } catch (error) { failure = error }
    expect(failure).toBeInstanceOf(Error)
    expect((failure as Error).message).toBe('Forbidden')
  })
})
