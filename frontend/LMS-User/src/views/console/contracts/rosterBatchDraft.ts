import type { ShiftBatchRequest } from '../../../api/contracts/shiftBatches'

export interface RosterBatchDraft extends Omit<ShiftBatchRequest, 'period'> {
  propertyId: string
  period: ShiftBatchRequest['period'] | ''
}
