import type { Trip } from './staff'
import { locale, t } from '../i18n'

const labels: Record<string, string> = {
  PENDING: '待確認', CONFIRMED: '未派車', DRIVER_ASSIGNED: '已指派', DRIVER_EN_ROUTE: '司機前往中', ARRIVED_AT_PICKUP: '司機已抵達', IN_PROGRESS: '行程中', COMPLETED: '已完成', CANCELLED: '已取消',
  AWAITING_RESPONSE: '等待司機回覆', NEEDS_REDISPATCH: '需重新派車', PENDING_CONFIRMATION: '待確認',
}
export function statusLabel(status: string): string { return t(labels[status] ?? status.replaceAll('_', ' ')) }
export function formatDateTime(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(locale.value === 'en' ? 'en-MO' : 'zh-Hant-MO', { timeZone: 'Asia/Macau', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false }).format(date)
}
export function isActiveTrip(trip: Trip): boolean { return !['COMPLETED', 'CANCELLED'].includes(trip.status) }
export function toUtc(date: string, time: string): string | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^\d{2}:\d{2}$/.test(time)) return null
  const parsed = new Date(`${date}T${time}:00+08:00`)
  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString().replace('.000Z', 'Z')
}
