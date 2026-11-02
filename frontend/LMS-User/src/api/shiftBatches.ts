import { api } from './session'
import type { CreateShiftBatchRequest, ShiftBatchPreviewView, ShiftBatchRequest, ShiftBatchView } from './contracts/shiftBatches'

const root = '/api/dispatch/staff/v1/shifts'

export const shiftBatchApi = {
  preview: (body: ShiftBatchRequest, signal?: AbortSignal) => api.request<ShiftBatchPreviewView>(`${root}/batch-previews`, {
    method: 'POST', body: JSON.stringify(body), signal,
  }),
  create: (body: CreateShiftBatchRequest, idempotencyKey: string) => api.request<ShiftBatchView>(`${root}/batches`, {
    method: 'POST', headers: { 'Idempotency-Key': idempotencyKey }, body: JSON.stringify(body),
  }),
}
