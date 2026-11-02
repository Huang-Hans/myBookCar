import { apiBaseUrl } from './baseUrl'
import { driverNativeBridge, refreshNativeSession, type DriverNativeBridge } from './nativeBridge'

export interface ApiErrorItem { field: string; message: string; code?: string }
export interface Pagination { currentPage: number; pageSize: number; totalItems: number; totalPages: number }
export interface ApiEnvelope<T> { code: number; message: string; data: T; meta?: { traceId: string; pagination?: Pagination }; traceId: string }
export interface AppSession { accessToken: string; tokenType: 'Bearer'; expiresIn: number; refreshToken: string; refreshExpiresIn: number; principalType: 'DRIVER' }

export class ApiRequestError extends Error {
  constructor(message: string, readonly statusCode: number, readonly code: number, readonly traceId: string, readonly errors: ApiErrorItem[] = []) {
    super(message); this.name = 'ApiRequestError'
  }
}

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
type Headers = Record<string, string>
interface RequestOptions { method?: HttpMethod; body?: unknown; headers?: Headers; isAnonymous?: boolean; canRetry?: boolean; canManageSession?: boolean }
const SESSION_KEY = 'lmsDriverSession'
let refreshPromise: Promise<AppSession> | null = null

// In the APK shell the session lives in native storage: it survives process restarts and is shared with the
// background location service. The browser keeps sessionStorage.
function readStoredSession(): unknown {
  const bridge = driverNativeBridge()
  if (bridge) return JSON.parse(bridge.getSession() || 'null')
  if (isNativeAppRuntime()) {
    const persisted = uni.getStorageSync(SESSION_KEY)
    if (persisted) return persisted
    if (typeof sessionStorage !== 'undefined') {
      const legacy = sessionStorage.getItem(SESSION_KEY)
      if (legacy) {
        const parsed: unknown = JSON.parse(legacy)
        uni.setStorageSync(SESSION_KEY, parsed)
        sessionStorage.removeItem(SESSION_KEY)
        return parsed
      }
    }
    return null
  }
  return typeof sessionStorage === 'undefined' ? uni.getStorageSync(SESSION_KEY) : JSON.parse(sessionStorage.getItem(SESSION_KEY) ?? 'null')
}
function isNativeAppRuntime(): boolean {
  return typeof (globalThis as typeof globalThis & { plus?: unknown }).plus !== 'undefined'
}
function readSession(): AppSession | null {
  let value: unknown
  try { value = readStoredSession() }
  catch { value = null }
  if (!value || typeof value !== 'object') return null
  const session = value as Partial<AppSession>
  return typeof session.accessToken === 'string' && typeof session.refreshToken === 'string' ? session as AppSession : null
}
export const hasDriverSession = (): boolean => readSession() !== null
export const getDriverAccessToken = (): string => readSession()?.accessToken ?? ''
export const saveDriverSession = (session: AppSession): void => {
  const bridge = driverNativeBridge()
  if (bridge) bridge.saveSession(JSON.stringify(session))
  else if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.setStorageSync(SESSION_KEY, session); else sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
}
export const clearDriverSession = (): void => {
  const bridge = driverNativeBridge()
  if (bridge) bridge.clearSession()
  else if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.removeStorageSync(SESSION_KEY); else sessionStorage.removeItem(SESSION_KEY)
  if (isNativeAppRuntime() && typeof sessionStorage !== 'undefined') sessionStorage.removeItem(SESSION_KEY)
}
export const createIdempotencyKey = (prefix: string): string => `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 12)}`
const createTraceId = (): string => `tr_${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`
export function buildQuery(values: Record<string, string | number | boolean | undefined>): string {
  const value = Object.entries(values).filter((entry): entry is [string, string | number | boolean] => entry[1] !== undefined).map(([key, item]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(item))}`).join('&')
  return value ? `?${value}` : ''
}
const wait = (milliseconds: number): Promise<void> => new Promise((resolve) => setTimeout(resolve, milliseconds))
function normalizeHeaders(value: unknown): Headers {
  return value && typeof value === 'object' ? Object.fromEntries(Object.entries(value as Record<string, unknown>).map(([key, item]) => [key.toLowerCase(), String(item)])) : {}
}
async function rawRequest<T>(path: string, options: RequestOptions = {}): Promise<{ statusCode: number; headers: Headers; envelope: ApiEnvelope<T> }> {
  const headers: Headers = { Accept: 'application/json', 'Content-Type': 'application/json; charset=utf-8', 'X-Trace-Id': createTraceId(), ...options.headers }
  const session = readSession()
  if (!options.isAnonymous && session) headers.Authorization = `Bearer ${session.accessToken}`
  return new Promise((resolve, reject) => uni.request({
    url: `${apiBaseUrl}${path}`, method: options.method ?? 'GET', data: options.body, header: headers,
    success(response) {
      if (response.statusCode === 204) {
        const responseHeaders = normalizeHeaders(response.header)
        resolve({ statusCode: 204, headers: responseHeaders, envelope: { code: 20000, message: 'success', data: null as T, traceId: responseHeaders['x-trace-id'] ?? 'unavailable' } })
        return
      }
      if (!response.data || typeof response.data !== 'object') { reject(new ApiRequestError('The server returned an invalid response.', response.statusCode, 50000, 'unavailable')); return }
      resolve({ statusCode: response.statusCode, headers: normalizeHeaders(response.header), envelope: response.data as ApiEnvelope<T> })
    },
    fail(error) { reject(new ApiRequestError(error.errMsg || 'Network request failed.', 0, 50000, 'unavailable')) },
  } as unknown as UniApp.RequestOptions))
}
const toApiError = (statusCode: number, envelope: ApiEnvelope<unknown>): ApiRequestError => new ApiRequestError(envelope.message || 'Request failed.', statusCode, envelope.code, envelope.traceId, (envelope as unknown as { errors?: ApiErrorItem[] }).errors ?? [])
async function refreshSession(): Promise<AppSession> {
  if (refreshPromise) return refreshPromise
  const session = readSession()
  if (!session) throw new ApiRequestError('Your session has expired.', 401, 40100, 'unavailable')
  refreshPromise = (async () => {
    const bridge = driverNativeBridge()
    if (bridge) return refreshThroughNative(bridge, session.refreshToken)
    const response = await rawRequest<AppSession>('/api/platform/driver/v1/token-refreshes', { method: 'POST', body: { refreshToken: session.refreshToken }, isAnonymous: true, canRetry: false })
    if (response.statusCode < 200 || response.statusCode >= 300 || response.envelope.code >= 40000) throw toApiError(response.statusCode, response.envelope)
    saveDriverSession(response.envelope.data); return response.envelope.data
  })().finally(() => { refreshPromise = null })
  return refreshPromise
}
// Native refresh shares one lock with the location service and has already stored the new session.
async function refreshThroughNative(bridge: DriverNativeBridge, staleRefreshToken: string): Promise<AppSession> {
  const result = await refreshNativeSession(bridge, staleRefreshToken)
  const session = result.session as Partial<AppSession> | undefined
  if (!result.ok || typeof session?.accessToken !== 'string' || typeof session.refreshToken !== 'string') {
    throw new ApiRequestError(result.message || 'Your session has expired.', result.statusCode, result.code, result.traceId)
  }
  return session as AppSession
}
export async function refreshDriverAccessToken(): Promise<string> {
  try { return (await refreshSession()).accessToken }
  catch (error) { clearDriverSession(); uni.reLaunch({ url: '/pages/login/index' }); throw error }
}
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<ApiEnvelope<T>> {
  const canRetry = options.canRetry !== false
  let response = await rawRequest<T>(path, options)
  if (!options.isAnonymous && options.canManageSession !== false && canRetry && response.envelope.code === 40101) {
    try { await refreshSession(); response = await rawRequest<T>(path, { ...options, canRetry: false }) }
    catch (error) { clearDriverSession(); uni.reLaunch({ url: '/pages/login/index' }); throw error }
  }
  if (canRetry && (response.statusCode === 429 || response.envelope.code === 42900)) {
    await wait(Math.min(5, Math.max(1, Number(response.headers['retry-after']) || 1)) * 1000)
    response = await rawRequest<T>(path, { ...options, canRetry: false })
  }
  if (response.statusCode < 200 || response.statusCode >= 300 || response.envelope.code >= 40000) {
    const error = toApiError(response.statusCode, response.envelope)
    console.error(JSON.stringify({ event: 'apiRequestFailed', code: error.code, traceId: error.traceId, endpoint: path }))
    if (!options.isAnonymous && options.canManageSession !== false && error.code === 40100) { clearDriverSession(); uni.reLaunch({ url: '/pages/login/index' }) }
    throw error
  }
  return response.envelope
}
export async function loginDriver(phone: string, password: string): Promise<AppSession> {
  const response = await apiRequest<AppSession>('/api/platform/driver/v1/logins', { method: 'POST', body: { phone, password }, isAnonymous: true })
  saveDriverSession(response.data); return response.data
}
export async function logoutDriver(): Promise<void> {
  const session = readSession()
  try { if (session) await apiRequest<null>('/api/platform/driver/v1/logouts', { method: 'POST', body: { refreshToken: session.refreshToken }, isAnonymous: true, canRetry: false }) }
  finally { clearDriverSession() }
}
