import { describe, expect, it } from 'vitest'
import type { ShiftBoardView, ShiftView } from './staff'
import { shiftBoardCandidates } from './dispatchShiftDrivers'

const shift = (changes: Partial<ShiftView> = {}): ShiftView => ({
  shiftId: 'shift_1', businessDate: '2026-10-22', period: 'MORNING', startsAt: '07:00', endsAt: '15:00',
  endsNextDay: false, driverId: 'driver_1', driverName: 'Alex Chen', propertyId: 'COD',
  vehiclePlate: 'MP-28-61', vehicleGrade: 'BUSINESS_SEDAN', status: 'ON_DUTY', role: 'PREFERRED', attendance: 'ON_TIME', badges: [], version: 1,
  ...changes,
})

describe('dispatch shift-board candidates', () => {
  it('matches the console invite list by pickup shift, property, and requested vehicle class', () => {
    const board: ShiftBoardView = {
      timeZone: 'Asia/Macau', businessDate: '2026-10-22', propertyId: 'COD',
      periods: [
        { period: 'MORNING', startsAt: '07:00', endsAt: '15:00', endsNextDay: false, count: 4, shifts: [shift(), shift({ shiftId: 'shift_2', driverId: 'driver_2', status: 'REST' }), shift({ shiftId: 'shift_3', driverId: 'driver_3', propertyId: 'SC' }), shift({ shiftId: 'shift_7', driverId: 'driver_6', vehicleGrade: 'LUXURY_SEDAN' })] },
        { period: 'AFTERNOON', startsAt: '15:00', endsAt: '23:00', endsNextDay: false, count: 2, shifts: [shift({ shiftId: 'shift_4' }), shift({ shiftId: 'shift_5', driverId: 'driver_4', driverName: null, vehiclePlate: null, vehicleGrade: null })] },
        { period: 'NIGHT', startsAt: '23:00', endsAt: '07:00', endsNextDay: true, count: 1, shifts: [shift({ shiftId: 'shift_6', driverId: 'driver_5' })] },
      ],
    }
    expect(shiftBoardCandidates(board, 'COD', '2026-10-22T08:00:00+08:00', 'BUSINESS_SEDAN')).toEqual([
      { driverId: 'driver_1', name: 'Alex Chen', vehicleLabel: 'MP-28-61' },
    ])
    expect(shiftBoardCandidates(board, 'COD', '2026-10-22T22:00:00+08:00', 'BUSINESS_SEDAN')).toEqual([
      { driverId: 'driver_1', name: 'Alex Chen', vehicleLabel: 'MP-28-61' },
    ])
    expect(shiftBoardCandidates(board, 'COD', '2026-10-22T23:30:00+08:00', 'BUSINESS_SEDAN')).toEqual([
      { driverId: 'driver_5', name: 'Alex Chen', vehicleLabel: 'MP-28-61' },
    ])
  })
})
