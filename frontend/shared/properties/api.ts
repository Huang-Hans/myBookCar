import type { PropertyDraft, PropertyPage, PropertyPatch, PropertyQuery, PropertyRecord, PropertyService } from './contracts'

interface PropertyClient {
  request<T>(path: string, init?: RequestInit): Promise<T>
  requestEnvelope<T>(path: string, init?: RequestInit): Promise<{ data: T; meta?: { pagination?: { totalItems: number; totalPages?: number } | null } | null }>
}
const path = '/api/platform/staff/v1/properties'
const wireStatus = (status: string): string => status.toUpperCase()
export async function loadPropertyDirectory(client: PropertyClient): Promise<PropertyRecord[]> {
  const values: PropertyRecord[] = []
  for (let page = 1; ; page += 1) {
    const result = await client.requestEnvelope<PropertyRecord[]>(`${path}?page=${page}&pageSize=50&status=ACTIVE`)
    values.push(...result.data)
    if (page >= (result.meta?.pagination?.totalPages ?? page)) return values
  }
}

export function createPropertyService(client: PropertyClient): PropertyService {
  return {
    list: (query: PropertyQuery): Promise<PropertyPage> => {
      const parameters = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize) })
      if (query.search) parameters.set('search', query.search)
      if (query.status) parameters.set('status', wireStatus(query.status))
      return client.requestEnvelope<PropertyRecord[]>(`${path}?${parameters}`)
    },
    detail: (id: string) => client.request<PropertyRecord>(`${path}/${encodeURIComponent(id)}`),
    create: (draft: PropertyDraft, key: string) => client.request<PropertyRecord>(path, {
      method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify(draft),
    }),
    update: (id: string, changes: PropertyPatch, version: number) => client.request<PropertyRecord>(`${path}/${encodeURIComponent(id)}`, {
      method: 'PATCH', headers: { 'X-Expected-Version': String(version) }, body: JSON.stringify(changes),
    }),
    remove: async (id: string, version: number): Promise<void> => {
      await client.request<boolean>(`${path}/${encodeURIComponent(id)}`, {
        method: 'DELETE', headers: { 'X-Expected-Version': String(version) },
      })
    },
  }
}
