import { apiBaseUrl } from './baseUrl'

export interface ApiFieldError { field: string; message: string; code?: string }
export interface Pagination { currentPage: number; pageSize: number; totalItems: number; totalPages: number }
export interface ApiEnvelope<T> { code: number; message: string; data: T; meta?: { traceId: string; pagination?: Pagination }; traceId: string; errors?: ApiFieldError[] }
export interface StaffSession { accessToken: string; refreshToken?: string | null }
export interface CurrentUser { userId: string; name: string; corporateEmail: string; roleCode: string; hasAllPropertyAccess: boolean; propertyId?: string | null; permissions: string[] }
export class ApiRequestError extends Error {
  constructor(message: string, readonly statusCode: number, readonly code: number, readonly traceId: string, readonly errors: ApiFieldError[] = []) { super(message); this.name = 'ApiRequestError' }
}

const storageKey = 'lmsUserAppSession'
let refreshPromise: Promise<StaffSession> | null = null
let isRedirecting = false

export function getSession(): StaffSession | null {
  try {
    const value: unknown = readStoredSession()
    if (value && typeof value === 'object' && 'accessToken' in value && typeof value.accessToken === 'string') return value as StaffSession
  } catch { /* An unavailable or invalid session means signed out. */ }
  return null
}
function isNativeAppRuntime(): boolean {
  return typeof (globalThis as typeof globalThis & { plus?: unknown }).plus !== 'undefined'
}
function readStoredSession(): unknown {
  if (isNativeAppRuntime()) {
    const persisted = uni.getStorageSync(storageKey)
    if (persisted) return persisted
    if (typeof sessionStorage !== 'undefined') {
      const legacy = sessionStorage.getItem(storageKey)
      if (legacy) {
        const parsed: unknown = JSON.parse(legacy)
        uni.setStorageSync(storageKey, parsed)
        sessionStorage.removeItem(storageKey)
        return parsed
      }
    }
    return null
  }
  return typeof sessionStorage === 'undefined' ? uni.getStorageSync(storageKey) : JSON.parse(sessionStorage.getItem(storageKey) ?? 'null')
}
export function saveSession(value: StaffSession): void {
  isRedirecting = false
  if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.setStorageSync(storageKey, value)
  else sessionStorage.setItem(storageKey, JSON.stringify(value))
}
export function clearSession(): void {
  if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.removeStorageSync(storageKey)
  else sessionStorage.removeItem(storageKey)
  if (isNativeAppRuntime() && typeof sessionStorage !== 'undefined') sessionStorage.removeItem(storageKey)
}
export function requireSession(): boolean {
  if (getSession()) return true
  redirectToLogin()
  return false
}
function redirectToLogin(): void {
  if (isRedirecting) return
  isRedirecting = true
  uni.reLaunch({ url: '/pages/login/index', fail: () => { isRedirecting = false } })
}
export function query(values: Record<string, string | number | boolean | undefined>): string {
  const pairs = Object.entries(values).filter(([, value]) => value !== undefined && value !== '').map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
  return pairs.length ? `?${pairs.join('&')}` : ''
}
export function idempotencyKey(prefix: string): string { return `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 12)}` }
function traceId(): string { return `tr_${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}` }
function wait(milliseconds: number): Promise<void> { return new Promise((resolve) => setTimeout(resolve, milliseconds)) }

export interface RequestOptions { method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'; body?: unknown; headers?: Record<string, string>; isAnonymous?: boolean }
async function send<T>(path: string, options: RequestOptions): Promise<{ status: number; envelope: ApiEnvelope<T>; retryAfter: string | undefined }> {
  const requestTraceId = traceId()
  const headers: Record<string, string> = { Accept: 'application/json', 'Content-Type': 'application/json; charset=utf-8', 'X-Trace-Id': requestTraceId, ...options.headers }
  const session = getSession()
  if (!options.isAnonymous && session) headers.Authorization = `Bearer ${session.accessToken}`
  return new Promise((resolve, reject) => {
    uni.request({
      url: `${apiBaseUrl}${path}`, method: options.method ?? 'GET', data: options.body, header: headers,
      success(response) {
        const envelope = response.data as ApiEnvelope<T>
        if (!envelope || typeof envelope !== 'object' || typeof envelope.code !== 'number' || typeof envelope.traceId !== 'string') {
          reject(new ApiRequestError('Invalid server response', response.statusCode, 50000, requestTraceId)); return
        }
        const responseHeaders = response.header as Record<string, string> | undefined
        const retryAfter = Object.entries(responseHeaders ?? {}).find(([key]) => key.toLowerCase() === 'retry-after')?.[1]
        resolve({ status: response.statusCode, envelope, retryAfter })
      },
      fail(error) { reject(new ApiRequestError(error.errMsg || 'Network request failed', 0, 50000, requestTraceId)) },
    } as UniApp.RequestOptions)
  })
}
async function refreshSession(): Promise<void> {
  const token = getSession()?.refreshToken
  if (!token) throw new ApiRequestError('Session expired', 401, 40101, 'unavailable')
  if (!refreshPromise) refreshPromise = (async () => {
    const result = await send<StaffSession>('/api/v1/refresh', { method: 'POST', body: { refreshToken: token }, isAnonymous: true })
    if (result.status !== 200 || result.envelope.code !== 20000) throw new ApiRequestError(result.envelope.message, result.status, result.envelope.code, result.envelope.traceId)
    saveSession(result.envelope.data)
    return result.envelope.data
  })().finally(() => { refreshPromise = null })
  await refreshPromise
}
export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiEnvelope<T>> {
  let hasRefreshed = false
  let hasRetriedRateLimit = false
  while (true) {
    const { status, envelope, retryAfter } = await send<T>(path, options)
    if (status === 401 && envelope.code === 40101 && !options.isAnonymous && !hasRefreshed) {
      hasRefreshed = true
      try { await refreshSession(); continue } catch { clearSession(); redirectToLogin() }
    }
    if (status === 429 && envelope.code === 42900 && !hasRetriedRateLimit) {
      hasRetriedRateLimit = true
      await wait(Math.min(5000, Math.max(1000, (Number(retryAfter) || 1) * 1000)))
      continue
    }
    if (status < 200 || status >= 300 || envelope.code >= 40000) {
      if (!options.isAnonymous && (envelope.code === 40100 || envelope.code === 40101)) { clearSession(); redirectToLogin() }
      console.error(JSON.stringify({ event: 'apiRequestFailed', traceId: envelope.traceId, code: envelope.code, statusCode: status }))
      throw new ApiRequestError(envelope.message, status, envelope.code, envelope.traceId, envelope.errors ?? [])
    }
    return envelope
  }
}
export function displayError(error: unknown): string {
  if (error instanceof ApiRequestError) return `${error.message}${error.errors.length ? ` · ${error.errors.map((item) => `${item.field}: ${item.message}`).join('; ')}` : ''} · Trace ID: ${error.traceId}`
  return error instanceof Error ? error.message : 'Request failed'
}
