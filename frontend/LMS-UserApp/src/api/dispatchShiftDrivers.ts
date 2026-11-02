import type { DispatchCandidate, ShiftBoardView } from './staff'

function localMinutes(value: string, timeZone: string): number | null {
  const clock = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(value)
  if (clock) return Number(clock[1]) * 60 + Number(clock[2])
  const instant = new Date(value)
  if (Number.isNaN(instant.getTime())) return null
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone, hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).formatToParts(instant)
  const hour = Number(parts.find((part) => part.type === 'hour')?.value)
  const minute = Number(parts.find((part) => part.type === 'minute')?.value)
  return Number.isInteger(hour) && Number.isInteger(minute) ? hour * 60 + minute : null
}

function periodMatchesPickup(board: ShiftBoardView, period: ShiftBoardView['periods'][number], pickupAt: string): boolean {
  const timeZone = board.timeZone || 'Asia/Macau'
  const pickupMinute = localMinutes(pickupAt, timeZone)
  const startMinute = localMinutes(period.startsAt, timeZone)
  const endMinute = localMinutes(period.endsAt, timeZone)
  if (pickupMinute === null || startMinute === null || endMinute === null) return false
  const wrapsMidnight = period.endsNextDay || endMinute <= startMinute
  return wrapsMidnight
    ? pickupMinute >= startMinute || pickupMinute < endMinute
    : pickupMinute >= startMinute && pickupMinute < endMinute
}

export function shiftBoardCandidates(board: ShiftBoardView, propertyId: string, pickupAt: string, vehicleClass: string): DispatchCandidate[] {
  const seen = new Set<string>()
  const candidates: DispatchCandidate[] = []
  for (const period of board.periods ?? []) {
    if (!periodMatchesPickup(board, period, pickupAt)) continue
    for (const shift of period.shifts ?? []) {
      if (shift.status !== 'ON_DUTY' || !shift.driverId || seen.has(shift.driverId)) continue
      if (shift.propertyId !== propertyId) continue
      // Keep mobile invitation candidates identical to the LMS-User console: only drivers
      // with an eligible vehicle of the trip's requested class may be invited.
      if (!shift.vehicleGrade || shift.vehicleGrade !== vehicleClass) continue
      seen.add(shift.driverId)
      candidates.push({
        driverId: shift.driverId,
        name: shift.driverName || shift.driverId,
        vehicleLabel: shift.vehiclePlate,
      })
    }
  }
  return candidates
}
