import { getDriverAccessToken, refreshDriverAccessToken } from './http'
import { NotificationHubClient, type NotificationConnectionHandler, type NotificationHandler } from './notificationHub'
import { apiBaseUrl } from './baseUrl'

const hubOrigin = apiBaseUrl || (typeof window !== 'undefined' ? window.location.origin : 'https://melco1.weprogram.site')
const hubUrl = `${hubOrigin}/api/dispatch/driver/v1/notifications`
const hub = new NotificationHubClient(hubUrl, getDriverAccessToken, refreshDriverAccessToken)

export function subscribeDriverNotifications(handler: NotificationHandler): () => void {
  return hub.subscribe(handler)
}

export function onDriverNotificationsConnected(handler: NotificationConnectionHandler): () => void {
  return hub.onConnected(handler)
}

export async function startDriverNotificationHub(): Promise<void> {
  if (!getDriverAccessToken()) return
  await hub.start()
}

export const stopDriverNotificationHub = (): Promise<void> => hub.stop()
