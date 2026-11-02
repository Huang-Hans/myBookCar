import type { CustomerLocation } from '@/api/customer'

export function defaultDropoffLocation(
  locations: readonly CustomerLocation[], pickupLocationId: string, serviceId: string,
): CustomerLocation | undefined {
  if (serviceId !== 'cross-border') return locations[1]
  const pickup = locations.find((location) => location.locationId === pickupLocationId)
  if (!pickup) return undefined
  return locations.find((location) => location.cityCode !== pickup.cityCode)
}
