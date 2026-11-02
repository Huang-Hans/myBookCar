export type SignInMethod = 'Email' | 'Phone' | 'Melco Member ID'

export function validatePreviewSignIn(method: SignInMethod, identity: string, password: string): string | null {
  const value = identity.trim()
  if (!value || !password) return 'Enter your credentials to continue.'
  if (method === 'Email' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Enter a valid email address.'
  if (method === 'Phone' && !/^\+?[\d\s-]{6,20}$/.test(value)) return 'Enter a valid phone number.'
  return null
}

export function validatePreviewRegistration(fullName: string, phone: string, email: string): string | null {
  if (!fullName.trim() || !/^\+?[\d\s-]{6,20}$/.test(phone.trim()) || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
    return 'Enter your name, phone and a valid email address.'
  }
  return null
}
