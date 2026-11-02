import { isTripLocationSnapshot, type TripLocation, type TripLocationMessage, type TripLocationResult, type TripLocationSnapshot } from './contracts'

export interface TripLocationConnection {
  readonly isConnected: boolean
  invoke<T>(method: string, argument: unknown): Promise<T>
  reconnect(): Promise<void>
  onConnected(handler: () => void): () => void
  subscribeTripLocations(handler: (message: TripLocationMessage) => void): () => void
}

export interface TripLocationState extends TripLocationSnapshot {
  errorCode: number | null
  hasEnded: boolean
}

const subscriptions = new WeakMap<TripLocationConnection, Map<string, number>>()

export class TripLocationSubscription {
  private active = true
  private requestVersion = 0
  private pushVersion = 0
  private lastSeq = 0
  private location: TripLocation | null = null
  private isSharing = false
  private hasEnded = false
  private retryTimer: ReturnType<typeof setTimeout> | null = null
  private readonly unsubscribe: Array<() => void>

  constructor(private readonly connection: TripLocationConnection, private readonly tripId: string,
    private readonly onState: (state: TripLocationState) => void) {
    const counts = subscriptions.get(connection) ?? new Map<string, number>()
    counts.set(tripId, (counts.get(tripId) ?? 0) + 1)
    subscriptions.set(connection, counts)
    this.unsubscribe = [connection.subscribeTripLocations((message) => this.receive(message)),
      connection.onConnected(() => { void this.refresh() })]
    this.publish(null, false)
    void this.refresh()
  }

  dispose(): void {
    if (!this.active) return
    this.active = false
    this.clearRetry()
    this.unsubscribe.forEach((unsubscribe) => unsubscribe())
    const counts = subscriptions.get(this.connection)
    const remaining = (counts?.get(this.tripId) ?? 1) - 1
    if (remaining > 0) counts?.set(this.tripId, remaining)
    else counts?.delete(this.tripId)
    void this.leave()
  }

  private async refresh(): Promise<void> {
    if (!this.active) return
    this.clearRetry()
    const version = ++this.requestVersion
    const pushVersion = this.pushVersion
    try {
      const result = await this.connection.invoke<TripLocationResult<TripLocationSnapshot>>('WatchTrip', this.tripId)
      if (!this.active) { await this.leave(); return }
      if (version !== this.requestVersion) return
      if (result.code !== 20000) { this.handleFailure(result.code); return }
      if (!isTripLocationSnapshot(result.data) || result.data.tripId !== this.tripId) { this.handleFailure(50300); return }
      if (pushVersion !== this.pushVersion) return
      this.applySnapshot(result.data)
    } catch {
      if (this.active && version === this.requestVersion) this.scheduleRetry()
    }
  }

  private applySnapshot(snapshot: TripLocationSnapshot): void {
    if (!snapshot.isSharing) { this.publish(null, false); return }
    if (!snapshot.location) { if (this.lastSeq === 0) this.publish(null, true); return }
    this.acceptLocation(snapshot.location)
  }

  private receive(message: TripLocationMessage): void {
    if (!this.active || message.data.tripId !== this.tripId) return
    if (message.data.eventType === 'TRIP_LOCATION_ENDED') {
      this.pushVersion += 1
      this.hasEnded = true
      this.publish(null, false)
      this.dispose()
      return
    }
    if (message.data.location && message.data.location.seq > this.lastSeq) {
      this.pushVersion += 1
      this.acceptLocation(message.data.location)
    }
  }

  private acceptLocation(location: TripLocation): void {
    if (location.seq <= this.lastSeq) return
    this.lastSeq = location.seq
    this.publish(location, true)
  }

  private publish(location: TripLocation | null, isSharing: boolean, errorCode: number | null = null): void {
    this.location = location
    this.isSharing = isSharing
    this.onState({ tripId: this.tripId, isSharing, location, errorCode, hasEnded: this.hasEnded })
  }

  private handleFailure(code: number): void {
    if ([40001, 40300, 40400].includes(code)) { this.publish(null, false, code); this.dispose(); return }
    this.publish(this.location, this.isSharing, code)
    if (code === 40100 || code === 40101) void this.connection.reconnect().catch(() => this.scheduleRetry())
    this.scheduleRetry()
  }

  private scheduleRetry(): void {
    if (!this.active || this.retryTimer) return
    this.retryTimer = setTimeout(() => { this.retryTimer = null; void this.refresh() }, 5_000)
  }

  private clearRetry(): void {
    if (this.retryTimer) clearTimeout(this.retryTimer)
    this.retryTimer = null
  }

  private async leave(): Promise<void> {
    if (!this.connection.isConnected || subscriptions.get(this.connection)?.has(this.tripId)) return
    try { await this.connection.invoke('UnwatchTrip', this.tripId) }
    catch { /* A disconnected connection no longer holds the server-side subscription. */ }
  }
}
