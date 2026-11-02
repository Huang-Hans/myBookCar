import { describe, expect, it } from 'vitest'

import { maskPhoneForDisplay, presentMembership } from './accountPresentation'

describe('account presentation', () => {
  it('shows trusted account membership values including a real zero balance', () => {
    expect(presentMembership({ memberId: 'M-1008', tierCode: 'GOLD', pointsBalance: 0 })).toEqual({
      memberId: 'M-1008',
      tierCode: 'GOLD',
      pointsBalance: '0',
      hasMembershipData: true,
    })
  })

  it('falls back to the customer summary without inventing a points balance', () => {
    expect(presentMembership({ memberId: '', tierCode: '', pointsBalance: null }, { memberId: 'M-2001', tierCode: 'SILVER' })).toEqual({
      memberId: 'M-2001',
      tierCode: 'SILVER',
      pointsBalance: '—',
      hasMembershipData: true,
    })
  })

  it('labels missing membership data instead of displaying a fake member tier and zero points', () => {
    expect(presentMembership({ memberId: ' ', tierCode: '' })).toEqual({
      memberId: '—',
      tierCode: 'Membership unavailable',
      pointsBalance: '—',
      hasMembershipData: false,
    })
  })

  it('masks an unexpectedly unmasked phone number and preserves an already masked value', () => {
    expect(maskPhoneForDisplay('13700000000')).toBe('13700***0000')
    expect(maskPhoneForDisplay('+853 6123 4829')).toBe('+853 6***4829')
    expect(maskPhoneForDisplay('+853 6***4829')).toBe('+853 6***4829')
  })
})
