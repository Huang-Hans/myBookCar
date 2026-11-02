export interface MembershipSource {
  memberId?: string | null
  tierCode?: string | null
  pointsBalance?: number | null
}

export interface MembershipPresentation {
  memberId: string
  tierCode: string
  pointsBalance: string
  hasMembershipData: boolean
}

function nonBlank(value?: string | null): string | null {
  const normalized = value?.trim()
  return normalized ? normalized : null
}

export function presentMembership(account?: MembershipSource | null, fallback?: MembershipSource | null): MembershipPresentation {
  const memberId = nonBlank(account?.memberId) ?? nonBlank(fallback?.memberId)
  const tierCode = nonBlank(account?.tierCode) ?? nonBlank(fallback?.tierCode)
  const points = account?.pointsBalance
  const hasPoints = typeof points === 'number' && Number.isFinite(points)
  return {
    memberId: memberId ?? '—',
    tierCode: tierCode ?? 'Membership unavailable',
    pointsBalance: hasPoints ? points.toLocaleString() : '—',
    hasMembershipData: Boolean(memberId || tierCode || hasPoints),
  }
}

export function maskPhoneForDisplay(value?: string | null): string {
  const phone = value?.trim()
  if (!phone) return '—'
  if (phone.includes('*')) return phone
  if (phone.length < 8) return '***'
  const separator = phone.indexOf(' ')
  const preferredHeadLength = separator >= 0 && separator <= 6 && separator + 1 < phone.length ? separator + 2 : 5
  const headLength = Math.min(phone.length - 5, preferredHeadLength)
  return `${phone.slice(0, headLength)}***${phone.slice(-4)}`
}
