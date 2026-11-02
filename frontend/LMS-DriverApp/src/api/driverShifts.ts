import { driverApi, type DriverShift } from './driver'

export interface DriverShiftDay { businessDate: string; shifts: DriverShift[] }

export function macauBusinessDate(now: Date): string {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Macau', year: 'numeric', month: '2-digit', day: '2-digit',
  }).formatToParts(now)
  const value = (type: string): string => parts.find((part) => part.type === type)?.value ?? ''
  return `${value('year')}-${value('month')}-${value('day')}`
}

export function nextBusinessDates(today: string): string[] {
  const start = new Date(`${today}T12:00:00Z`)
  return Array.from({ length: 7 }, (_, offset) => {
    const date = new Date(start)
    date.setUTCDate(start.getUTCDate() + offset)
    return date.toISOString().slice(0, 10)
  })
}

export function groupDriverShifts(shifts: DriverShift[], today: string): DriverShiftDay[] {
  return nextBusinessDates(today).map((businessDate) => ({
    businessDate,
    shifts: shifts.filter((shift) => shift.businessDate === businessDate),
  }))
}

export async function fetchUpcomingDriverShifts(driverId: string, today: string): Promise<DriverShiftDay[]> {
  const shifts: DriverShift[] = []
  let page = 1
  while (true) {
    const response = await driverApi.listShiftsForDriver(driverId, page)
    const batch = Array.isArray(response.data) ? response.data : []
    shifts.push(...batch)
    const oldestDate = batch.map((shift) => shift.businessDate).filter((date): date is string => Boolean(date)).sort()[0]
    if (!batch.length || (oldestDate && oldestDate < today)) break
    const totalPages = response.meta?.pagination?.totalPages
    if (totalPages !== undefined ? page >= totalPages : batch.length < 50) break
    page += 1
  }
  return groupDriverShifts(shifts, today)
}
