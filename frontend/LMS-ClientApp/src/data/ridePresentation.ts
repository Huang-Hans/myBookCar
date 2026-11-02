import type { ServiceId } from '@/types/booking'

export interface RidePresentation {
  id: 'eqs' | 's580' | 's560'
  name: string
  model: string
  image: string
  capacity: number
}

export const rides: readonly RidePresentation[] = [
  { id: 'eqs', name: 'Mercedes-Maybach', model: 'EQS SUV 680 4MATIC', image: '/static/figma/ride-eqs.webp', capacity: 4 },
  { id: 's580', name: 'Mercedes-Maybach', model: 'S580 4MATIC', image: '/static/figma/limousine.webp', capacity: 3 },
  { id: 's560', name: 'Mercedes-Maybach', model: 'S560 4MATIC', image: '/static/figma/ride-s560.png', capacity: 3 }
]

export function findRide(id: string): RidePresentation {
  return rides.find((ride) => ride.id === id) ?? rides[0]!
}

export function rideListIcon(serviceId: ServiceId): string {
  if (serviceId === 'business-sedan' || serviceId === 'limousine') return '/static/figma/limousine.webp'
  if (serviceId === 'party') return '/static/figma/shuttle.webp'
  if (serviceId === 'cross-border') return '/static/figma/mpv.webp'
  return rides[0]!.image
}

export function displayDeparture(value: string): string {
  return value === 'Now' ? 'Now' : value.replace(' ', ' · ')
}

export function vehicleClassLabel(vehicleClass: string): string {
  const labels: Record<string, string> = {
    BUSINESS_SEDAN: 'Standard Sedan', LUXURY_SEDAN: 'Luxury Sedan', LUXURY_MPV: 'Luxury MPV', MAYBACH: 'Maybach',
  }
  return labels[vehicleClass] ?? vehicleClass
}
