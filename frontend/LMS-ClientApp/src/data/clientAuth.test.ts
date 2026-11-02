import { describe, expect, it } from 'vitest'
import { validatePreviewRegistration, validatePreviewSignIn } from './clientAuth'

describe('client preview forms', () => {
  it('validates each sign-in identifier without persisting a password', () => {
    expect(validatePreviewSignIn('Email', 'bad', 'secret')).toBe('Enter a valid email address.')
    expect(validatePreviewSignIn('Phone', '+853 6123 4567', 'secret')).toBeNull()
    expect(validatePreviewSignIn('Melco Member ID', 'MC000184', '')).not.toBeNull()
  })

  it('requires complete registration contact fields', () => {
    expect(validatePreviewRegistration('Alex Morgan', '+853 6123 4567', 'alex@example.com')).toBeNull()
    expect(validatePreviewRegistration('', '1', 'bad')).not.toBeNull()
  })
})
