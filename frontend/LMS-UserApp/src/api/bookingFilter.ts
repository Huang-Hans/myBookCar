export type BookingFilter = 'ALL' | 'ACTIVE' | 'COMPLETED' | 'CANCELLED'

export function bookingFilterFromValue(value: unknown): BookingFilter | null {
  return value === 'ALL' || value === 'ACTIVE' || value === 'COMPLETED' || value === 'CANCELLED' ? value : null
}

export function bookingFilterFromHash(hash: string): BookingFilter | null {
  const query = hash.split('?')[1]
  return query ? bookingFilterFromValue(new URLSearchParams(query).get('filter')) : null
}
