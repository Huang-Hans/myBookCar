import type { DriverTripDetail, DriverTripSummary } from '@/api/driver'
import { localeTag, t } from '@/i18n/locale'
import type { DriverOrder } from './driverOrders'

export type DriverHistoryCard = Pick<DriverOrder, 'id' | 'displayId' | 'service' | 'status' | 'date' | 'pickup' | 'dropoff' | 'accepted' | 'started' | 'ended'>

function formatDateTime(value?: string): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString(localeTag())
}

export function mapDriverTripSummary(item: DriverTripSummary): DriverHistoryCard {
  return {
    id: item.tripId,
    displayId: item.tripReference,
    service: item.route.isCrossBorder ? 'HK–Macau Transfer' : item.serviceType.replace(/_/g, ' '),
    status: item.status === 'CANCELLED' ? 'Cancelled' : 'Completed',
    date: formatDateTime(item.closedAt ?? item.pickupAt),
    pickup: item.route.pickupName,
    dropoff: item.route.dropoffName,
    accepted: formatDateTime(item.acceptedAt),
    started: formatDateTime(item.pickedUpAt),
    ended: formatDateTime(item.completedAt),
  }
}

export function mapDriverTripDetail(item: DriverTripDetail): DriverOrder {
  const summary = mapDriverTripSummary(item)
  const parts = item.passenger.name.trim().split(/\s+/).filter(Boolean)
  return {
    ...summary,
    accepted: formatDateTime(item.fulfillment.acceptedAt ?? item.acceptedAt),
    started: formatDateTime(item.fulfillment.pickedUpAt ?? item.pickedUpAt),
    ended: formatDateTime(item.fulfillment.completedAt ?? item.completedAt),
    pickupAddress: '',
    dropoffAddress: '',
    customerInitials: parts.slice(0, 2).map((part) => part.charAt(0).toUpperCase()).join('') || '—',
    customerName: item.passenger.name,
    customerPhone: item.passenger.phoneMasked ?? '—',
    passengers: item.passengers,
    specialInstructions: item.specialRequests ?? '—',
    vehicleRequirements: `${item.vehicleClass === 'BUSINESS_SEDAN' ? t('Standard Sedan') : item.vehicleClass.replace(/_/g, ' ')} · ${item.passengers} Passengers`,
    customerServiceNote: item.cancellation?.reasonCode ?? '—',
  }
}
