import { ref } from 'vue'
import { createApiClient } from './http'
import { apiBaseUrl } from './baseUrl'

interface Tokens { accessToken: string; refreshToken?: string | null }
export interface CurrentUser { userId: string; name: string; corporateEmail: string; roleCode: string; hasAllPropertyAccess: boolean; propertyId?: string | null; permissions: string[] }

const key = 'lmsStaffSession'
function readTokens(): Tokens | null {
  try {
    const value: unknown = JSON.parse(sessionStorage.getItem(key) ?? 'null')
    if (value && typeof value === 'object' && 'accessToken' in value && typeof value.accessToken === 'string') return value as Tokens
  } catch { /* An unavailable or malformed session is treated as signed out. */ }
  return null
}
const tokens = ref<Tokens | null>(readTokens())
export const currentUser = ref<CurrentUser | null>(null)
export const isAuthenticated = ref(Boolean(tokens.value?.accessToken))
export const isDemoMode = import.meta.env.MODE === 'test' || import.meta.env.VITE_LMS_DEMO === 'true'
export const getStaffAccessToken = (): string => tokens.value?.accessToken ?? ''

export function isAccessTokenExpiring(token: string, now = Date.now()): boolean {
  const payload = token.split('.')[1]
  if (!payload) return false
  try {
    const decoded: unknown = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
    return typeof decoded === 'object' && decoded !== null && 'exp' in decoded && typeof decoded.exp === 'number'
      ? decoded.exp * 1000 <= now + 60_000
      : false
  } catch { return false }
}

export async function getStaffHubAccessToken(): Promise<string> {
  const token = getStaffAccessToken()
  if (!token || !tokens.value?.refreshToken || !isAccessTokenExpiring(token)) return token
  return refreshStaffAccessToken()
}

function saveTokens(value: Tokens | null): void {
  tokens.value = value
  isAuthenticated.value = Boolean(value?.accessToken)
  try { if (value) sessionStorage.setItem(key, JSON.stringify(value)); else sessionStorage.removeItem(key) } catch { /* In-memory tokens remain usable. */ }
}

let refreshPromise: Promise<boolean> | null = null
async function refreshStaffSession(): Promise<boolean> {
  if (refreshPromise) return refreshPromise
  refreshPromise = performStaffSessionRefresh().finally(() => { refreshPromise = null })
  return refreshPromise
}

async function performStaffSessionRefresh(): Promise<boolean> {
  const refreshToken = tokens.value?.refreshToken
  if (!refreshToken) return false
  try {
    const result = await createApiClient({ baseUrl: apiBaseUrl }).request<Tokens>('/api/v1/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) })
    saveTokens(result)
    return true
  } catch { saveTokens(null); currentUser.value = null; return false }
}
export async function refreshStaffAccessToken(): Promise<string> {
  if (await refreshStaffSession()) return getStaffAccessToken()
  saveTokens(null)
  currentUser.value = null
  if (typeof location !== 'undefined' && location.pathname !== '/login') location.assign('/login')
  return ''
}
export const api = createApiClient({
  baseUrl: apiBaseUrl,
  getAccessToken: () => tokens.value?.accessToken ?? null,
  refreshToken: refreshStaffSession,
  onAuthenticationRequired: () => { saveTokens(null); currentUser.value = null; if (!isDemoMode && location.pathname !== '/login') location.assign('/login') },
  logTrace: (traceId, code) => { if (code >= 40000) console.error('LMS API request failed', { traceId, code }) },
})

export async function login(username: string, password: string): Promise<CurrentUser> {
  const result = await api.request<Tokens>('/api/v1/login', { method: 'POST', body: JSON.stringify({ username, password }) })
  if (!result.accessToken) throw new Error('The identity provider did not return an access token.')
  saveTokens(result)
  try { return await loadCurrentUser() } catch (error) { saveTokens(null); throw error }
}
export async function loadCurrentUser(): Promise<CurrentUser> {
  const user = await api.request<CurrentUser>('/api/platform/staff/v1/current-user')
  currentUser.value = user
  return user
}
export function logout(): void { saveTokens(null); currentUser.value = null }
