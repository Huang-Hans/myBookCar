export function shiftMonth(monthKey: string, offset: number): string {
  const [year = 0, month = 1] = monthKey.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1 + offset, 1)).toISOString().slice(0, 7)
}

export function monthDates(monthKey: string): string[] {
  const [year = 0, month = 1] = monthKey.split('-').map(Number)
  const count = new Date(Date.UTC(year, month, 0)).getUTCDate()
  return Array.from({ length: count }, (_, index) =>
    new Date(Date.UTC(year, month - 1, index + 1)).toISOString().slice(0, 10))
}

export function mondayOffset(dateKey: string): number {
  const [year = 0, month = 1, day = 1] = dateKey.split('-').map(Number)
  return (new Date(Date.UTC(year, month - 1, day)).getUTCDay() + 6) % 7
}
