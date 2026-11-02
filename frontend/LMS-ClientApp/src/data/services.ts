import type { TravelService } from '@/types/booking'

export const travelServices: TravelService[] = [
  {
    id: 'business-sedan', title: 'Standard Sedan', description: 'Standard sedan transfer within the same city.',
    image: '/static/figma/limousine.webp', seats: 3, eta: 'Scheduled pickup'
  },
  {
    id: 'limousine', title: 'Limousine', description: 'Private rides in Macau',
    image: '/static/figma/limousine.webp', seats: 3, eta: 'Scheduled pickup'
  },
  {
    id: 'party', title: 'Party', description: 'Group and party transfers',
    image: '/static/figma/shuttle.webp', seats: 6, eta: 'Advance booking'
  },
  {
    id: 'cross-border', title: 'Cross-Border Transfer', description: 'Hong Kong–Macau rides',
    image: '/static/figma/mpv.webp', seats: 6, eta: 'Advance booking'
  }
]

export function findService(id?: string | null): TravelService {
  return travelServices.find((service) => service.id === id) ?? travelServices.find((service) => service.id === 'limousine')!
}
