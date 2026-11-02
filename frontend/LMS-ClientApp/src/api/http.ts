import { buildLoginRoute } from '../data/authNavigation'
import { apiBaseUrl } from './baseUrl'

export interface ApiErrorItem {
  field: string
  message: string
  code?: string
}

export interface Pagination {
  currentPage: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface ApiMeta {
  traceId: string
  pagination?: Pagination
}

export interface ApiEnvelope<T> {
  code: number
  message: string
  data: T
  meta?: ApiMeta
  traceId: string
}

export interface AppSession {
  accessToken: string
  tokenType: 'Bearer'
  expiresIn: number
  refreshToken: string
  refreshExpiresIn: number
  principalType: 'CUSTOMER'
}

export class ApiRequestError extends Error {
  constructor(
    message: string,
    readonly statusCode: number,
    readonly code: number,
    readonly traceId: string,
    readonly errors: ApiErrorItem[] = [],
  ) {
    super(message)
    this.name = 'ApiRequestError'
  }
}

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
type RequestHeaders = Record<string, string>

interface RequestOptions {
  method?: HttpMethod
  body?: unknown
  headers?: RequestHeaders
  isAnonymous?: boolean
  canRetry?: boolean
}

const SESSION_KEY = 'lmsCustomerSession'
let refreshPromise: Promise<AppSession> | null = null
let isRedirectingToLogin = false

function currentPageRoute(): string {
  const pages = getCurrentPages()
  const current = pages[pages.length - 1] as { route?: string; options?: Record<string, string | number | boolean> } | undefined
  if (!current?.route) return '/pages/home/index'
  const query = Object.entries(current.options ?? {})
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
    .join('&')
  return `/${current.route}${query ? `?${query}` : ''}`
}

function redirectToLogin(): void {
  if (isRedirectingToLogin) return
  isRedirectingToLogin = true
  uni.reLaunch({
    url: buildLoginRoute(currentPageRoute()),
    fail: () => { isRedirectingToLogin = false }
  })
}

function readSession(): AppSession | null {
  let value: unknown
  try {
    value = readStoredSession()
  } catch { value = null }
  if (!value || typeof value !== 'object') return null
  const session = value as Partial<AppSession>
  return typeof session.accessToken === 'string' && typeof session.refreshToken === 'string'
    ? session as AppSession
    : null
}

function isNativeAppRuntime(): boolean {
  return typeof (globalThis as typeof globalThis & { plus?: unknown }).plus !== 'undefined'
}

function readStoredSession(): unknown {
  if (isNativeAppRuntime()) {
    const persisted = uni.getStorageSync(SESSION_KEY)
    if (persisted) return persisted
    // Migrate sessions created by previous app versions that used the WebView's sessionStorage.
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

export function hasCustomerSession(): boolean {
  return readSession() !== null
}

export function getCustomerAccessToken(): string {
  return readSession()?.accessToken ?? ''
}

export function saveCustomerSession(session: AppSession): void {
  isRedirectingToLogin = false
  if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.setStorageSync(SESSION_KEY, session)
  else sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
}

export function clearCustomerSession(): void {
  if (isNativeAppRuntime() || typeof sessionStorage === 'undefined') uni.removeStorageSync(SESSION_KEY)
  else sessionStorage.removeItem(SESSION_KEY)
  if (isNativeAppRuntime() && typeof sessionStorage !== 'undefined') sessionStorage.removeItem(SESSION_KEY)
}

export function createIdempotencyKey(prefix: string): string {
  return `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 12)}`
}

function createTraceId(): string {
  return `tr_${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`
}

export function buildQuery(values: Record<string, string | number | boolean | undefined>): string {
  const query = Object.entries(values)
    .filter((entry): entry is [string, string | number | boolean] => entry[1] !== undefined)
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
    .join('&')
  return query ? `?${query}` : ''
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds))
}

function normalizedHeaders(headers: unknown): RequestHeaders {
  if (!headers || typeof headers !== 'object') return {}
  return Object.fromEntries(Object.entries(headers as Record<string, unknown>).map(([key, value]) => [key.toLowerCase(), String(value)]))
}

async function rawRequest<T>(path: string, options: RequestOptions = {}): Promise<{ statusCode: number; headers: RequestHeaders; envelope: ApiEnvelope<T> }> {
  const session = readSession()
  const headers: RequestHeaders = {
    Accept: 'application/json',
    'Content-Type': 'application/json; charset=utf-8',
    'X-Trace-Id': createTraceId(),
    ...options.headers,
  }
  if (!options.isAnonymous && session) headers.Authorization = `Bearer ${session.accessToken}`

  return new Promise((resolve, reject) => {
    uni.request({
      url: `${apiBaseUrl}${path}`,
      method: options.method ?? 'GET',
      data: options.body,
      header: headers,
      success(response) {
        if (response.statusCode === 204) {
          const responseHeaders = normalizedHeaders(response.header)
          resolve({ statusCode: 204, headers: responseHeaders, envelope: { code: 20000, message: 'success', data: null as T, traceId: responseHeaders['x-trace-id'] ?? 'unavailable' } })
          return
        }
        const envelope = response.data as ApiEnvelope<T>
        if (!envelope || typeof envelope !== 'object') {
          reject(new ApiRequestError('The server returned an invalid response.', response.statusCode, 50000, 'unavailable'))
          return
        }
        resolve({ statusCode: response.statusCode, headers: normalizedHeaders(response.header), envelope })
      },
      fail(error) {
        reject(new ApiRequestError(error.errMsg || 'Network request failed.', 0, 50000, 'unavailable'))
      },
    } as unknown as UniApp.RequestOptions)
  })
}

async function refreshSession(): Promise<AppSession> {
  if (refreshPromise) return refreshPromise
  const session = readSession()
  if (!session) throw new ApiRequestError('Your session has expired.', 401, 40100, 'unavailable')
  refreshPromise = (async () => {
    const response = await rawRequest<AppSession>('/api/platform/customer/v1/token-refreshes', {
      method: 'POST', body: { refreshToken: session.refreshToken }, isAnonymous: true, canRetry: false,
    })
    if (response.statusCode < 200 || response.statusCode >= 300 || response.envelope.code >= 40000) {
      throw toApiError(response.statusCode, response.envelope)
    }
    saveCustomerSession(response.envelope.data)
    return response.envelope.data
  })().finally(() => { refreshPromise = null })
  return refreshPromise
}

export async function refreshCustomerAccessToken(): Promise<string> {
  try { return (await refreshSession()).accessToken }
  catch (error) { clearCustomerSession(); redirectToLogin(); throw error }
}

function toApiError(statusCode: number, envelope: ApiEnvelope<unknown>): ApiRequestError {
  const candidate = envelope as unknown as { errors?: ApiErrorItem[] }
  return new ApiRequestError(envelope.message || 'Request failed.', statusCode, envelope.code, envelope.traceId, candidate.errors ?? [])
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<ApiEnvelope<T>> {
  const canRetry = options.canRetry !== false
  let response = await rawRequest<T>(path, options)
  if (!options.isAnonymous && canRetry && (response.statusCode === 401 || response.envelope.code === 40101)) {
    try {
      await refreshSession()
      response = await rawRequest<T>(path, { ...options, canRetry: false })
    } catch (error) {
      clearCustomerSession()
      redirectToLogin()
      throw error
    }
  }
  if (canRetry && (response.statusCode === 429 || response.envelope.code === 42900)) {
    const retryAfterSeconds = Math.min(5, Math.max(1, Number(response.headers['retry-after']) || 1))
    await delay(retryAfterSeconds * 1000)
    response = await rawRequest<T>(path, { ...options, canRetry: false })
  }
  if (response.statusCode < 200 || response.statusCode >= 300 || response.envelope.code >= 40000) {
    const error = toApiError(response.statusCode, response.envelope)
    console.error(JSON.stringify({ event: 'apiRequestFailed', code: error.code, traceId: error.traceId, endpoint: path }))
    if (!options.isAnonymous && error.code === 40100) {
      clearCustomerSession()
      redirectToLogin()
    }
    throw error
  }
  return response.envelope
}

export async function loginCustomer(phone: string, password: string): Promise<AppSession> {
  const envelope = await apiRequest<AppSession>('/api/platform/customer/v1/logins', {
    method: 'POST', body: { phone, password }, isAnonymous: true,
  })
  saveCustomerSession(envelope.data)
  return envelope.data
}

export async function logoutCustomer(): Promise<void> {
  const session = readSession()
  try {
    if (session) await apiRequest<null>('/api/platform/customer/v1/logouts', {
      method: 'POST', body: { refreshToken: session.refreshToken }, isAnonymous: true, canRetry: false,
    })
  } finally {
    clearCustomerSession()
  }
}
