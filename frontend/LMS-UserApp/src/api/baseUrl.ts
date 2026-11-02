export function resolveApiBaseUrl(platform: string, protocol?: string): string {
  return platform === 'h5' && protocol !== 'file:' ? '' : 'https://melco1.weprogram.site'
}

declare const process: { env: { UNI_PLATFORM?: string } }
function currentPlatform(): string {
  try { return process.env.UNI_PLATFORM ?? 'h5' }
  catch { return 'h5' }
}

export const apiBaseUrl = resolveApiBaseUrl(currentPlatform(), typeof window === 'undefined' ? undefined : window.location.protocol)
