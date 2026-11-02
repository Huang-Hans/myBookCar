import { describe, expect, it, vi } from 'vitest'
import { filterCatalogOptions, loadCatalogOptions } from './catalogOptions'
import type { ApiEnvelope } from './http'

function page(data: string[], currentPage: number, totalPages: number): ApiEnvelope<string[]> {
  return { code: 20000, message: 'success', data, traceId: 'tr_catalog', meta: {
    traceId: 'tr_catalog', pagination: { currentPage, pageSize: 50, totalItems: 51, totalPages },
  } }
}

describe('booking catalog options', () => {
  it('collects all pages so later locations remain selectable', async () => {
    const fetchPage = vi.fn().mockResolvedValueOnce(page(['Altira Macau'], 1, 2)).mockResolvedValueOnce(page(['Hong Kong International Airport'], 2, 2))
    await expect(loadCatalogOptions(fetchPage)).resolves.toEqual(['Altira Macau', 'Hong Kong International Airport'])
    expect(fetchPage.mock.calls.map(([number]) => number)).toEqual([1, 2])
  })

  it('propagates catalog failures instead of presenting an empty selector', async () => {
    const fetchPage = vi.fn().mockRejectedValue(new Error('Catalog unavailable'))
    await expect(loadCatalogOptions(fetchPage)).rejects.toThrow('Catalog unavailable')
  })

  it('searches location names and details within the mobile option sheet', () => {
    const options = [
      { id: 'loc_1', label: 'Altira Macau', detail: 'MACAU · Taipa' },
      { id: 'loc_2', label: 'Hong Kong International Airport', detail: 'HONG_KONG · Airport' },
    ]
    expect(filterCatalogOptions(options, ' airport ')).toEqual([options[1]])
    expect(filterCatalogOptions(options, 'MACAU')).toEqual([options[0]])
    expect(filterCatalogOptions(options, 'missing')).toEqual([])
  })
})
