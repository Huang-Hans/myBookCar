export type TabKey = 'home' | 'bookings' | 'account'

export const TAB_REFRESH_KEY = 'lmsClientTabRefresh'

// Bottom-navigation taps carry a one-shot marker so the target page knows to refresh its data;
// plain page opens (back from a detail page or app resume) leave the marker unset and skip fetching.
export function markTabRefresh(tab: TabKey): void {
  try { uni.setStorageSync(TAB_REFRESH_KEY, tab) } catch { /* Storage unavailability never blocks navigation. */ }
}

export function consumeTabRefresh(tab: TabKey): boolean {
  let marker = ''
  try { marker = String(uni.getStorageSync(TAB_REFRESH_KEY) ?? '') } catch { return false }
  if (marker !== tab) return false
  try { uni.removeStorageSync(TAB_REFRESH_KEY) } catch { /* The marker expires after one use regardless. */ }
  return true
}
