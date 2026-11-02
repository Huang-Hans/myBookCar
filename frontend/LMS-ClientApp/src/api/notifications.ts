import { getCustomerAccessToken, refreshCustomerAccessToken } from './http'
import { NotificationHubClient, type NotificationConnectionHandler, type NotificationHandler } from './notificationHub'
import { apiBaseUrl } from './baseUrl'
import { TripLocationSubscription, type TripLocationState } from '../../../shared/tripLocations/subscription'

const hubOrigin = apiBaseUrl || (typeof window !== 'undefined' ? window.location.origin : 'https://melco1.weprogram.site')
const hubUrl = `${hubOrigin}/api/booking/customer/v1/notifications`
const hub = new NotificationHubClient(hubUrl, getCustomerAccessToken, refreshCustomerAccessToken)

export function subscribeCustomerNotifications(handler: NotificationHandler): () => void {
  return hub.subscribe(handler)
}

export function onCustomerNotificationsConnected(handler: NotificationConnectionHandler): () => void {
  return hub.onConnected(handler)
}

export async function startCustomerNotificationHub(): Promise<void> {
  if (!getCustomerAccessToken()) return
  await hub.start()
}

export const stopCustomerNotificationHub = (): Promise<void> => hub.stop()

export const watchCustomerTripLocation = (tripId: string, onState: (state: TripLocationState) => void): TripLocationSubscription =>
  new TripLocationSubscription(hub, tripId, onState)
