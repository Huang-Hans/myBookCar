export interface DepartureDateOption {
  value: string
  label: string
}

function pad2(value: number): string {
  return String(value).padStart(2, '0')
}

export function localDateValue(date: Date): string {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`
}

export function buildDepartureDateOptions(startDate: Date, dayCount = 90): DepartureDateOption[] {
  if (!Number.isInteger(dayCount) || dayCount < 1) throw new Error('dayCount must be a positive integer.')
  const formatter = new Intl.DateTimeFormat('en-US', { weekday: 'short', month: 'short', day: 'numeric' })
  return Array.from({ length: dayCount }, (_, offset) => {
    const date = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate() + offset, 12)
    return { value: localDateValue(date), label: formatter.format(date) }
  })
}

export function timeParts(value: string): [number, number] {
  const match = /^(\d{2}):(\d{2})$/.exec(value)
  if (!match) return [0, 0]
  const hour = Number(match[1])
  const minute = Number(match[2])
  return [Math.min(23, Math.max(0, hour)), Math.min(59, Math.max(0, minute))]
}

export function nextSelectableDeparture(now: Date): { date: string; time: string } {
  const departure = new Date(now)
  departure.setSeconds(0, 0)
  if (departure.getTime() <= now.getTime()) departure.setMinutes(departure.getMinutes() + 1)
  return { date: localDateValue(departure), time: `${pad2(departure.getHours())}:${pad2(departure.getMinutes())}` }
}

export function isFutureDeparture(dateValue: string, timeValue: string, now: Date): boolean {
  const dateMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateValue)
  const timeMatch = /^(\d{2}):(\d{2})$/.exec(timeValue)
  if (!dateMatch || !timeMatch) return false
  const year = Number(dateMatch[1])
  const month = Number(dateMatch[2])
  const day = Number(dateMatch[3])
  const hour = Number(timeMatch[1])
  const minute = Number(timeMatch[2])
  const selected = new Date(year, month - 1, day, hour, minute)
  return selected.getFullYear() === year && selected.getMonth() === month - 1 && selected.getDate() === day
    && selected.getHours() === hour && selected.getMinutes() === minute && selected.getTime() > now.getTime()
}

export function availableDepartureHours(dateValue: string, now: Date): string[] {
  const minimum = nextSelectableDeparture(now)
  if (dateValue < minimum.date) return []
  const firstHour = dateValue === minimum.date ? Number(minimum.time.slice(0, 2)) : 0
  return Array.from({ length: 24 - firstHour }, (_, index) => pad2(firstHour + index))
}

export function availableDepartureMinutes(dateValue: string, hourValue: string, now: Date): string[] {
  const minimum = nextSelectableDeparture(now)
  if (dateValue < minimum.date) return []
  const firstMinute = dateValue === minimum.date && hourValue === minimum.time.slice(0, 2)
    ? Number(minimum.time.slice(3, 5)) : 0
  return Array.from({ length: 60 - firstMinute }, (_, index) => pad2(firstMinute + index))
}
