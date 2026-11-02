import type { ShiftBatchRequest } from '../../api/contracts/shiftBatches'
import type { RosterBatchDraft } from './contracts/rosterBatchDraft'
import { monthDates } from './rosterCalendar'

export function emptyShiftBatch(month: string, propertyId: string): RosterBatchDraft {
  return { propertyId, driverIds: [], from: `${month}-01`, to: monthDates(month).at(-1) ?? `${month}-01`,
    weekdays: [1, 2, 3, 4, 5], period: '', status: 'ON_DUTY', role: 'PREFERRED' }
}

export function batchDateSpan(draft: RosterBatchDraft): number {
  const start = Date.parse(`${draft.from}T00:00:00Z`)
  const end = Date.parse(`${draft.to}T00:00:00Z`)
  return Math.round((end - start) / 86_400_000) + 1
}

export function batchDates(draft: RosterBatchDraft): string[] {
  const span = batchDateSpan(draft)
  if (!Number.isFinite(span) || span < 1 || span > 31) return []
  const start = Date.parse(`${draft.from}T00:00:00Z`)
  return Array.from({ length: span }, (_, index) => new Date(start + index * 86_400_000))
    .filter((date) => draft.weekdays.includes((date.getUTCDay() + 6) % 7 + 1))
    .map((date) => date.toISOString().slice(0, 10))
}

export function batchValidationMessage(draft: RosterBatchDraft): string {
  if (!draft.driverIds.length) return 'Select at least one driver.'
  if (!draft.period) return 'Select a shift before previewing.'
  return batchDateValidation(draft)
}

function batchDateValidation(draft: RosterBatchDraft): string {
  const span = batchDateSpan(draft)
  if (!Number.isFinite(span) || span < 1 || span > 31) return 'The date range must contain between 1 and 31 calendar days'
  const dates = batchDates(draft)
  if (!dates.length) return 'The selected weekdays do not match any date in the range'
  return dates.length * draft.driverIds.length > 1000 ? 'A batch must not contain more than 1000 driver-date combinations' : ''
}

export function copyBatchRules(draft: RosterBatchDraft): ShiftBatchRequest | null {
  if (!draft.period) return null
  return { ...draft, propertyId: draft.propertyId || undefined, period: draft.period, driverIds: [...draft.driverIds], weekdays: [...draft.weekdays] }
}
