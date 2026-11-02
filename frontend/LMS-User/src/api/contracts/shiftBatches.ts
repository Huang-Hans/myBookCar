import type { ShiftPeriod, ShiftRole, ShiftStatus } from '../dispatch'

export interface ShiftBatchRequest {
  propertyId?: string
  driverIds: string[]
  from: string
  to: string
  weekdays: number[]
  period: ShiftPeriod
  status: ShiftStatus
  role: ShiftRole
}

export interface CreateShiftBatchRequest extends ShiftBatchRequest { previewHash: string }

export interface ShiftBatchItemView {
  businessDate: string
  driverId: string
  driverName: string
  isExisting: boolean
  period?: ShiftPeriod | null
  status?: ShiftStatus | null
  role?: ShiftRole | null
  shiftId?: string | null
  propertyId?: string | null
}

export interface ShiftBatchPreviewView {
  previewHash: string
  driverCount: number
  dateCount: number
  createCount: number
  existingCount: number
  canSubmit: boolean
  items: ShiftBatchItemView[]
}

export interface ShiftBatchView {
  batchId: string
  createdAt: string
  createdCount: number
  skippedCount: number
  items: ShiftBatchItemView[]
}
