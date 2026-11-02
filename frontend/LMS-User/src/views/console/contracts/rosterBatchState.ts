import type { Ref } from 'vue'
import type { ShiftBatchPreviewView } from '../../../api/contracts/shiftBatches'
import type { RosterBatchDraft } from './rosterBatchDraft'

export interface RosterBatchState {
  draft: Ref<RosterBatchDraft>
  preview: Ref<ShiftBatchPreviewView | null>
  error: Ref<string>
  isPreviewing: Ref<boolean>
  isSubmitting: Ref<boolean>
  sequence: number
  controller: AbortController | null
  idempotencyKey: string
}
