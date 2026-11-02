export type BookingFilter = 'In Progress' | 'Completed' | 'Cancelled'

export function bookingEmptyTitle(filter: BookingFilter): string {
  return `No ${filter} Journeys`
}
