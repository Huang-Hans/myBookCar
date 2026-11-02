import { describe, expect, it } from 'vitest'
import type { CustomerLocation } from '@/api/customer'
import { defaultDropoffLocation } from './locationDefaults'

function location(locationId: string, cityCode: CustomerLocation['cityCode']): CustomerLocation {
  return { locationId, cityCode, name: locationId, address: '', region: '', categoryCode: '', operatingHours: '' }
}

const locations = [location('macau-first', 'MACAU'), location('macau-second', 'MACAU'), location('hong-kong', 'HONG_KONG')]

describe('default trip destination', () => {
  it('uses the second catalog address for local services', () => {
    expect(defaultDropoffLocation(locations, 'macau-first', 'limousine')?.locationId).toBe('macau-second')
  })

  it('uses an address in the other city for cross-border service', () => {
    expect(defaultDropoffLocation(locations, 'macau-first', 'cross-border')?.locationId).toBe('hong-kong')
    expect(defaultDropoffLocation(locations, 'hong-kong', 'cross-border')?.locationId).toBe('macau-first')
  })

  it('does not silently choose a same-city destination if the opposite city is unavailable', () => {
    expect(defaultDropoffLocation(locations.slice(0, 2), 'macau-first', 'cross-border')).toBeUndefined()
  })
})
