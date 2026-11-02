export interface DepartureDateOption { value: string; label: string }

const macauOffsetMs = 8 * 60 * 60 * 1000
const minuteMs = 60 * 1000

export function nextMacauDeparture(now: Date): { date: string; time: string } {
  const nextMinute = Math.ceil((now.getTime() + 1) / minuteMs) * minuteMs
  const value = new Date(nextMinute + macauOffsetMs).toISOString()
  return { date: value.slice(0, 10), time: value.slice(11, 16) }
}

export function departureDateOptions(startDate: string, locale: 'zh-Hant' | 'en', dayCount = 90): DepartureDateOption[] {
  const formatter = new Intl.DateTimeFormat(locale === 'en' ? 'en-MO' : 'zh-Hant-MO', {
    timeZone: 'UTC', weekday: 'short', month: 'short', day: 'numeric',
  })
  const start = Date.parse(`${startDate}T12:00:00Z`)
  if (!Number.isFinite(start) || !Number.isInteger(dayCount) || dayCount < 1) return []
  return Array.from({ length: dayCount }, (_, index) => {
    const day = new Date(start + index * 24 * 60 * minuteMs)
    return { value: day.toISOString().slice(0, 10), label: formatter.format(day) }
  })
}

export function availableDepartureHours(date: string, now: Date): string[] {
  const minimum = nextMacauDeparture(now)
  if (date < minimum.date) return []
  const first = date === minimum.date ? Number(minimum.time.slice(0, 2)) : 0
  return Array.from({ length: 24 - first }, (_, index) => String(first + index).padStart(2, '0'))
}

export function availableDepartureMinutes(date: string, hour: string, now: Date): string[] {
  const minimum = nextMacauDeparture(now)
  if (date < minimum.date) return []
  const first = date === minimum.date && hour === minimum.time.slice(0, 2) ? Number(minimum.time.slice(3, 5)) : 0
  return Array.from({ length: 60 - first }, (_, index) => String(first + index).padStart(2, '0'))
}

export function isFutureMacauDeparture(date: string, time: string, now: Date): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^\d{2}:\d{2}$/.test(time)) return false
  const parsed = new Date(`${date}T${time}:00+08:00`)
  return Number.isFinite(parsed.getTime())
    && new Date(parsed.getTime() + macauOffsetMs).toISOString().slice(0, 16) === `${date}T${time}`
    && parsed.getTime() > now.getTime()
}
