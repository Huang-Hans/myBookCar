import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { EventIdCache, isNotificationMessageV1, type NotificationMessageV1 } from './notificationContract'
import { isTripLocationMessage, type TripLocationMessage } from '../../../shared/tripLocations/contracts'
import { showSystemNotification } from '../../../shared/systemNotifications'

export type NotificationHandler = (message: NotificationMessageV1) => void | Promise<void>
export type NotificationConnectionHandler = () => void | Promise<void>
const isAuthenticationFailure = (error?: Error): boolean => Boolean(error && /(401|403|unauthori[sz]ed|forbidden)/i.test(error.message))

export class NotificationHubClient {
  private readonly connection: HubConnection
  private readonly eventIds = new EventIdCache()
  private readonly handlers = new Set<NotificationHandler>()
  private readonly connectedHandlers = new Set<NotificationConnectionHandler>()
  private readonly tripLocationHandlers = new Set<(message: TripLocationMessage) => void>()
  private startPromise: Promise<void> | null = null
  private retryTimer: ReturnType<typeof setTimeout> | null = null
  private shouldRun = false
  private shouldRefreshAccessToken = false

  constructor(url: string, accessTokenFactory: () => string | Promise<string>, refreshAccessTokenFactory?: () => string | Promise<string>) {
    this.connection = new HubConnectionBuilder()
      .withUrl(url, { withCredentials: false, accessTokenFactory: async () => {
        if (this.shouldRefreshAccessToken && refreshAccessTokenFactory) {
          this.shouldRefreshAccessToken = false
          return refreshAccessTokenFactory()
        }
        return accessTokenFactory()
      } })
      .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build()
    this.connection.on('notificationReceived', (value: unknown) => { void this.receive(value) })
    this.connection.on('tripLocationUpdated', (value: unknown) => {
      if (!isTripLocationMessage(value)) return
      for (const handler of this.tripLocationHandlers) handler(value)
    })
    this.connection.onreconnecting((error) => { if (isAuthenticationFailure(error)) this.shouldRefreshAccessToken = true })
    this.connection.onreconnected(() => { void this.notifyConnected() })
    this.connection.onclose((error) => { if (isAuthenticationFailure(error)) this.shouldRefreshAccessToken = true; if (this.shouldRun) this.scheduleRetry() })
  }

  subscribe(handler: NotificationHandler): () => void {
    this.handlers.add(handler)
    return () => { this.handlers.delete(handler) }
  }

  onConnected(handler: NotificationConnectionHandler): () => void {
    this.connectedHandlers.add(handler)
    return () => { this.connectedHandlers.delete(handler) }
  }

  get isConnected(): boolean { return this.connection.state === HubConnectionState.Connected }

  subscribeTripLocations(handler: (message: TripLocationMessage) => void): () => void {
    this.tripLocationHandlers.add(handler)
    return () => { this.tripLocationHandlers.delete(handler) }
  }

  async reconnect(): Promise<void> {
    this.shouldRefreshAccessToken = true
    await this.stop()
    await this.start()
  }

  async start(): Promise<void> {
    this.shouldRun = true
    if (this.connection.state === HubConnectionState.Connected || this.connection.state === HubConnectionState.Reconnecting) return
    if (this.startPromise) return this.startPromise
    this.clearRetry()
    this.startPromise = this.connection.start()
      .then(() => this.notifyConnected())
      .catch((error: unknown) => {
        if (isAuthenticationFailure(error instanceof Error ? error : undefined)) this.shouldRefreshAccessToken = true
        console.error(JSON.stringify({ event: 'notificationHubConnectionFailed', message: error instanceof Error ? error.message : 'Unknown SignalR error' }))
        this.scheduleRetry()
      })
      .finally(() => { this.startPromise = null })
    return this.startPromise
  }

  async stop(): Promise<void> {
    this.shouldRun = false
    this.clearRetry()
    if (this.connection.state !== HubConnectionState.Disconnected) await this.connection.stop()
  }

  async invoke<T>(methodName: string, argument: unknown): Promise<T> {
    await this.start()
    if (this.connection.state !== HubConnectionState.Connected) throw new Error('The notification connection is unavailable.')
    return this.connection.invoke<T>(methodName, argument)
  }

  private async receive(value: unknown): Promise<void> {
    if (!isNotificationMessageV1(value)) {
      console.error(JSON.stringify({ event: 'invalidNotificationMessage', code: 50000 }))
      return
    }
    if (!this.eventIds.addIfNew(value.data.eventId)) return
    showSystemNotification('LMS', value.data.notification.summary, { eventId: value.data.eventId, eventType: value.data.eventType, targetId: value.data.notification.targetId })
    for (const handler of this.handlers) {
      try { await handler(value) }
      catch (error) { console.error(JSON.stringify({ event: 'notificationHandlerFailed', traceId: value.traceId, message: error instanceof Error ? error.message : 'Unknown handler error' })) }
    }
  }

  private async notifyConnected(): Promise<void> {
    for (const handler of this.connectedHandlers) {
      try { await handler() }
      catch (error) { console.error(JSON.stringify({ event: 'notificationRecoveryFailed', message: error instanceof Error ? error.message : 'Unknown recovery error' })) }
    }
  }

  private scheduleRetry(): void {
    if (!this.shouldRun || this.retryTimer) return
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null
      void this.start()
    }, 30_000)
  }

  private clearRetry(): void {
    if (!this.retryTimer) return
    clearTimeout(this.retryTimer)
    this.retryTimer = null
  }
}
