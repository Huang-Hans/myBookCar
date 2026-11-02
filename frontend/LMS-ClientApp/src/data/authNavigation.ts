const DEFAULT_AUTHENTICATED_ROUTE = '/pages/home/index'
const LOGIN_ROUTE = '/pages/login/index'

function decodeRoute(value: string): string {
  try {
    return decodeURIComponent(value)
  } catch {
    return value
  }
}

export function resolvePostLoginRoute(value: unknown): string {
  if (typeof value !== 'string') return DEFAULT_AUTHENTICATED_ROUTE

  const route = decodeRoute(value.trim())
  if (!/^\/pages\/[a-z0-9-]+\/index(?:\?[^#]*)?$/.test(route)) return DEFAULT_AUTHENTICATED_ROUTE
  if (route === LOGIN_ROUTE || route.startsWith(`${LOGIN_ROUTE}?`)) return DEFAULT_AUTHENTICATED_ROUTE
  return route
}

export function buildLoginRoute(currentRoute: string): string {
  const redirect = resolvePostLoginRoute(currentRoute)
  if (redirect === DEFAULT_AUTHENTICATED_ROUTE && currentRoute !== DEFAULT_AUTHENTICATED_ROUTE) return LOGIN_ROUTE
  return `${LOGIN_ROUTE}?redirect=${encodeURIComponent(redirect)}`
}
