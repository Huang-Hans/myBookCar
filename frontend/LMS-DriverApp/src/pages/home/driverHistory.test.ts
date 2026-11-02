import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import homeSource from './index.vue?raw'

describe('driver history page', () => {
  it('uses the backend list for every filter and never displays demo orders', () => {
    const descriptor = parse(homeSource).descriptor
    const script = descriptor.scriptSetup?.content ?? ''
    const template = descriptor.template?.content ?? ''

    expect(script).toContain("driverApi.listTrips('HISTORY', status, page)")
    expect(script).toContain('response.data.map(mapDriverTripSummary)')
    expect(script).toContain('historyFilter.value = value\n  void loadHistory()')
    expect(script).not.toContain('historyOrders')
    expect(script).not.toContain('TR-DEMO-TRANSFER')
    expect(template).toContain('@tap="selectHistoryFilter(filter)"')
    expect(template).toContain("$t('No trips found.')")
  })
})
