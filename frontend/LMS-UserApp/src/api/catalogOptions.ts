import type { ApiEnvelope } from './http'

export interface CatalogOption { id: string; label: string; detail?: string }

export function filterCatalogOptions(options: readonly CatalogOption[], search: string): CatalogOption[] {
  const term = search.trim().toLocaleLowerCase()
  if (!term) return [...options]
  return options.filter((option) => `${option.label} ${option.detail ?? ''}`.toLocaleLowerCase().includes(term))
}

export async function loadCatalogOptions<T>(fetchPage: (page: number) => Promise<ApiEnvelope<T[]>>): Promise<T[]> {
  const first = await fetchPage(1)
  const items = [...first.data]
  const totalPages = first.meta?.pagination?.totalPages ?? 1
  for (let page = 2; page <= totalPages; page++) {
    const response = await fetchPage(page)
    items.push(...response.data)
  }
  return items
}
