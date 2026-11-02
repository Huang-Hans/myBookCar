/** Convert a Macau calendar date to the inclusive UTC start or exclusive next-day end. */
export function macauDateBoundary(date: string, nextDay: boolean): string {
  const value = new Date(`${date}T00:00:00+08:00`)
  if (nextDay) value.setUTCDate(value.getUTCDate() + 1)
  return value.toISOString().replace('.000Z', 'Z')
}
