import { describe, expect, it } from 'vitest'
import type { ShiftBoardView, ShiftView } from '../api/dispatch'
import { macauBusinessDate, shiftBoardDriverOptions } from './dispatchShiftDrivers'

const shift = (changes: Partial<ShiftView> = {}): ShiftView => ({
  shiftId: 'shift_1', businessDate: '2026-10-22', period: 'MORNING', startsAt: '07:00', endsAt: '15:00',
  endsNextDay: false, driverId: 'driver_1', driverName: 'Alex Chen', propertyId: 'COD',
  vehiclePlate: 'MP-28-61', status: 'ON_DUTY', role: 'PREFERRED', attendance: 'ON_TIME', badges: [], version: 1,
  ...changes,
})

describe('dispatch shift-board drivers', () => {
  it('uses the pickup date in Macau rather than the UTC calendar date', () => {
    expect(macauBusinessDate('2026-10-21T18:00:00Z')).toBe('2026-10-22')
    expect(() => macauBusinessDate('invalid')).toThrow('Trip pickup time is invalid.')
  })

  it('selects unique on-duty drivers at the order property across all periods', () => {
    const board: ShiftBoardView = {
      timeZone: 'Asia/Macau', businessDate: '2026-10-22', propertyId: 'COD',
      periods: [
        { period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, count: 3, shifts: [shift(), shift({ shiftId: 'shift_2', driverId: 'driver_2', status: 'REST' }), shift({ shiftId: 'shift_3', driverId: 'driver_3', propertyId: 'SC' })] },
        { period: 'AFTERNOON', startsAt: '15:00', endsAt: '23:00', endsNextDay: false, count: 2, shifts: [shift({ shiftId: 'shift_4' }), shift({ shiftId: 'shift_5', driverId: 'driver_4', driverName: null, vehiclePlate: null })] },
      ],
    }
    expect(shiftBoardDriverOptions(board, 'COD')).toEqual([
      { id: 'driver_1', name: 'Alex Chen', vehicle: '', plate: 'MP-28-61', availability: 'Available' },
      { id: 'driver_4', name: 'driver_4', vehicle: '', plate: '', availability: 'Available' },
    ])
  })
})
