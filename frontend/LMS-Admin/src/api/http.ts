export interface ApiFieldError { field: string; message: string; code?: string | null }
export interface Pagination { currentPage: number; pageSize: number; totalItems: number; totalPages: number }
export interface TraceMeta { traceId: string; pagination?: Pagination | null }
export interface SuccessEnvelope<T> { code: number; message: string; data: T; meta?: TraceMeta | null; traceId: string }
export interface ErrorEnvelope { code: number; message: string; data?: null; errors: ApiFieldError[]; traceId: string }
export interface ServiceBannerResponse { serviceName: string; description: string }
export type ApiEnvelope<T> = SuccessEnvelope<T> | ErrorEnvelope

export class ApiRequestError extends Error {
  constructor(readonly status: number, readonly code: number, message: string, readonly traceId: string, readonly errors: ApiFieldError[]) { super(message); this.name = 'ApiRequestError' }
}
export function apiErrorMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    const fields = error.errors.map((item) => `${item.field}: ${item.message}`).join('; ')
    return `${error.message}${fields ? ` · ${fields}` : ''} · traceId: ${error.traceId}`
  }
  return error instanceof Error ? error.message : 'LMS request failed.'
}

export interface ApiClientOptions {
  baseUrl: string
  getAccessToken?: () => string | null
  refreshToken?: () => Promise<boolean>
  onAuthenticationRequired?: () => void
  onError?: (error: ApiRequestError) => void
  logTrace?: (traceId: string, code: number) => void
  fetchImpl?: typeof fetch
  sleep?: (milliseconds: number) => Promise<void>
}

function isErrorEnvelope<T>(envelope: ApiEnvelope<T>): envelope is ErrorEnvelope { return envelope.code >= 40000 }
function retryDelay(value: string | null): number {
  if (!value) return 1000
  const seconds = Number(value)
  if (Number.isFinite(seconds)) return Math.max(0, Math.min(seconds * 1000, 30000))
  const date = Date.parse(value)
  return Number.isNaN(date) ? 1000 : Math.max(0, Math.min(date - Date.now(), 30000))
}

export function createApiClient(options: ApiClientOptions) {
  const fetchImpl = options.fetchImpl ?? fetch
  const sleep = options.sleep ?? ((milliseconds: number) => new Promise<void>((resolve) => setTimeout(resolve, milliseconds)))
  async function requestEnvelope<T>(path: string, init: RequestInit = {}): Promise<SuccessEnvelope<T>> {
    let hasRefreshed = false
    let hasRateLimitRetried = false
    while (true) {
      const headers = new Headers(init.headers)
      headers.set('Accept', 'application/json')
      if (init.body !== undefined) headers.set('Content-Type', 'application/json; charset=utf-8')
      const token = options.getAccessToken?.()
      if (token) headers.set('Authorization', `Bearer ${token}`)
      const response = await fetchImpl(`${options.baseUrl}${path}`, { ...init, headers })
      const envelope = await response.json() as ApiEnvelope<T>
      options.logTrace?.(envelope.traceId, envelope.code)
      if (response.status === 401 && envelope.code === 40101 && !hasRefreshed && options.refreshToken) {
        hasRefreshed = true
        if (await options.refreshToken()) continue
      }
      if (response.status === 429 && envelope.code === 42900 && !hasRateLimitRetried) {
        hasRateLimitRetried = true
        await sleep(retryDelay(response.headers.get('Retry-After')))
        continue
      }
      if (!response.ok || isErrorEnvelope(envelope)) {
        const error = new ApiRequestError(response.status, envelope.code, envelope.message, envelope.traceId, isErrorEnvelope(envelope) ? envelope.errors ?? [] : [])
        if (envelope.code === 40100 || envelope.code === 40101) options.onAuthenticationRequired?.()
        options.onError?.(error)
        throw error
      }
      return envelope as SuccessEnvelope<T>
    }
  }
  async function request<T>(path: string, init: RequestInit = {}): Promise<T> { return (await requestEnvelope<T>(path, init)).data }
  return { request, requestEnvelope }
}
