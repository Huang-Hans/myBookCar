import type { ShiftBoardView } from '../api/dispatch'

export interface ShiftDriverOption {
  id: string
  name: string
  vehicle: string
  plate: string
  availability: string
}

export function macauBusinessDate(pickupAt: string): string {
  const instant = new Date(pickupAt)
  if (Number.isNaN(instant.getTime())) throw new Error('Trip pickup time is invalid.')
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Macau', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(instant)
  const part = (type: string): string => parts.find((item) => item.type === type)?.value ?? ''
  return `${part('year')}-${part('month')}-${part('day')}`
}

export function shiftBoardDriverOptions(board: ShiftBoardView, propertyId?: string, vehicleClass?: string): ShiftDriverOption[] {
  const seen = new Set<string>()
  const options: ShiftDriverOption[] = []
  for (const period of board.periods ?? []) {
    for (const shift of period.shifts ?? []) {
      if (shift.status !== 'ON_DUTY' || !shift.driverId || seen.has(shift.driverId)) continue
      if (propertyId && shift.propertyId !== propertyId) continue
      const grade = shift.vehicleGrade
      if (!grade || (vehicleClass && grade !== vehicleClass)) continue
      seen.add(shift.driverId)
      options.push({
        id: shift.driverId,
        name: shift.driverName || shift.driverId,
        vehicle: ({ BUSINESS_SEDAN: 'Standard Sedan', LUXURY_SEDAN: 'Luxury Sedan', LUXURY_MPV: 'Luxury MPV' } as Record<string, string>)[grade] ?? grade,
        plate: shift.vehiclePlate ?? '',
        availability: 'Available',
      })
    }
  }
  return options
}
