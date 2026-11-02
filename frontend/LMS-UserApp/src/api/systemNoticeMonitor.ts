import { getSession } from './http'
import { staffApi } from './staff'
import { showSystemNotification } from '../../../shared/systemNotifications'

const knownIds = new Set<string>()
let timer: ReturnType<typeof setInterval> | undefined
let initialized = false
let requestInFlight = false

async function checkForNotices(): Promise<void> {
  if (!getSession() || requestInFlight) return
  requestInFlight = true
  try {
    const result = await staffApi.notices(false, 1)
    const notices = result.data
    if (!initialized) {
      for (const notice of notices) knownIds.add(notice.notificationId)
      initialized = true
      return
    }
    for (const notice of [...notices].reverse()) {
      if (knownIds.has(notice.notificationId)) continue
      knownIds.add(notice.notificationId)
      if (!notice.isRead) {
        showSystemNotification('LMS Operations', notice.summary, { notificationId: notice.notificationId, tripReference: notice.tripReference ?? null })
      }
    }
    while (knownIds.size > 500) {
      const oldest = knownIds.values().next().value as string | undefined
      if (!oldest) break
      knownIds.delete(oldest)
    }
  } catch {
    // The in-app notification center handles visible request errors; background polling is best-effort.
  } finally {
    requestInFlight = false
  }
}

export function startSystemNoticeMonitor(): void {
  if (timer) return
  void checkForNotices()
  timer = setInterval(() => { void checkForNotices() }, 30_000)
}

export function stopSystemNoticeMonitor(): void {
  if (timer) clearInterval(timer)
  timer = undefined
}
