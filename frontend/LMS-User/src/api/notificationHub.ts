import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { EventIdCache, isNotificationMessageV1, type NotificationMessageV1 } from './notificationContract'
import { isTripLocationMessage, type TripLocationMessage } from '../../../shared/tripLocations/contracts'

export type NotificationHandler = (message: NotificationMessageV1) => void | Promise<void>
export type NotificationConnectionHandler = () => void | Promise<void>
const isAuthenticationFailure = (error?: Error): boolean => Boolean(error && /(401|403|unauthori[sz]ed|forbidden)/i.test(error.message))
const isUnauthorized = (error?: Error): boolean => Boolean(error && /(401|unauthori[sz]ed)/i.test(error.message))
const safeErrorMessage = (error?: Error): string => (error?.message ?? 'Unknown SignalR error')
  .replace(/access_token=[^&#\s]+/gi, 'access_token=[redacted]')
  .replace(/Bearer\s+[A-Za-z0-9._~+/-]+/gi, 'Bearer [redacted]')

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

  constructor(url: string, accessTokenFactory: () => string | Promise<string>, private readonly refreshAccessTokenFactory?: () => string | Promise<string>) {
    this.connection = new HubConnectionBuilder()
      .withUrl(url, { withCredentials: false, accessTokenFactory: async () => {
        if (this.shouldRefreshAccessToken && this.refreshAccessTokenFactory) {
          this.shouldRefreshAccessToken = false
          return this.refreshAccessTokenFactory()
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
    this.connection.onreconnecting((error) => {
      console.info(JSON.stringify({ event: 'notificationHubReconnecting', message: safeErrorMessage(error) }))
      if (isUnauthorized(error)) this.shouldRefreshAccessToken = true
    })
    this.connection.onreconnected(() => { void this.notifyConnected() })
    this.connection.onclose((error) => {
      console.info(JSON.stringify({ event: 'notificationHubClosed', message: safeErrorMessage(error) }))
      if (isUnauthorized(error)) this.shouldRefreshAccessToken = true
      else if (isAuthenticationFailure(error)) this.shouldRun = false
      // 认证失败（令牌过期/被收回）时用刷新后的令牌尽快重连；其余故障仍按常规间隔重试。
      if (this.shouldRun) this.scheduleRetry(this.shouldRefreshAccessToken ? 5_000 : 30_000)
    })
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
    this.startPromise = this.connect()
      .finally(() => { this.startPromise = null })
    return this.startPromise
  }

  private async connect(): Promise<void> {
    try {
      await this.connection.start()
      await this.notifyConnected()
      return
    } catch (cause) {
      const error = cause instanceof Error ? cause : undefined
      if (isUnauthorized(error) && this.refreshAccessTokenFactory) {
        this.shouldRefreshAccessToken = false
        try {
          const token = await this.refreshAccessTokenFactory()
          if (token && this.shouldRun) {
            await this.connection.start()
            await this.notifyConnected()
            return
          }
        } catch (retryCause) {
          console.error(JSON.stringify({ event: 'notificationHubAuthenticationFailed', message: safeErrorMessage(retryCause instanceof Error ? retryCause : undefined) }))
        }
        this.shouldRun = false
      } else if (isAuthenticationFailure(error)) {
        this.shouldRun = false
      } else {
        this.scheduleRetry()
      }
      console.error(JSON.stringify({ event: 'notificationHubConnectionFailed', message: safeErrorMessage(error) }))
    }
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
    for (const handler of this.handlers) {
      try { await handler(value) }
      catch (error) { console.error(JSON.stringify({ event: 'notificationHandlerFailed', traceId: value.traceId, message: safeErrorMessage(error instanceof Error ? error : undefined) })) }
    }
  }

  private async notifyConnected(): Promise<void> {
    console.info(JSON.stringify({ event: 'notificationHubConnected' }))
    for (const handler of this.connectedHandlers) {
      try { await handler() }
      catch (error) { console.error(JSON.stringify({ event: 'notificationRecoveryFailed', message: safeErrorMessage(error instanceof Error ? error : undefined) })) }
    }
  }

  private scheduleRetry(delayMs = 30_000): void {
    if (!this.shouldRun || this.retryTimer) return
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null
      void this.start()
    }, delayMs)
  }

  private clearRetry(): void {
    if (!this.retryTimer) return
    clearTimeout(this.retryTimer)
    this.retryTimer = null
  }
}
