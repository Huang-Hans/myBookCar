export interface AmapConfig {
  key: string
}

export async function loadAmapConfig(): Promise<AmapConfig> {
  const response = await fetch('/config/amap.json', { cache: 'no-store' })
  if (!response.ok) throw new Error('Amap config is missing.')
  const body = await response.json() as Partial<AmapConfig>
  return { key: body.key?.trim() ?? '' }
}
