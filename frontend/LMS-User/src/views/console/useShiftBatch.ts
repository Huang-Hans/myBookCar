import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { shiftBatchApi } from '../../api/shiftBatches'
import type { ShiftBatchView } from '../../api/contracts/shiftBatches'
import type { ShiftPeriod } from '../../api/dispatch'
import type { RosterBatchState } from './contracts/rosterBatchState'
import { batchDates, batchValidationMessage, copyBatchRules, emptyShiftBatch } from './rosterBatchForm'

export function useShiftBatch(month: string, propertyId: string) {
  const state = createState(month, propertyId)
  const selectedDates = computed(() => batchDates(state.draft.value))
  watch(state.draft, () => invalidate(state), { deep: true, flush: 'sync' })
  watch([() => state.draft.value.period, () => state.draft.value.driverIds.slice()],
    () => { void refreshSelectionPreview(state) }, { flush: 'post' })
  onBeforeUnmount(() => { state.sequence += 1; state.controller?.abort() })
  return { draft: state.draft, preview: state.preview, error: state.error,
    isPreviewing: state.isPreviewing, isSubmitting: state.isSubmitting, selectedDates,
    loadPreview: () => loadPreview(state), selectPeriod: (period: ShiftPeriod) => selectPeriod(state, period),
    submit: () => submit(state) }
}

function selectPeriod(state: RosterBatchState, period: ShiftPeriod): void {
  if (state.isSubmitting.value) return
  state.draft.value.period = period
}

async function refreshSelectionPreview(state: RosterBatchState): Promise<void> {
  if (!state.draft.value.period || state.draft.value.driverIds.length === 0) return
  await loadPreview(state)
}

function createState(month: string, propertyId: string): RosterBatchState {
  return { draft: ref(emptyShiftBatch(month, propertyId)), preview: ref(null), error: ref(''),
    isPreviewing: ref(false), isSubmitting: ref(false), sequence: 0, controller: null, idempotencyKey: '' }
}

function invalidate(state: RosterBatchState): void {
  state.sequence += 1
  state.controller?.abort()
  state.preview.value = null
  state.isPreviewing.value = false
  state.idempotencyKey = ''
  state.error.value = ''
}

async function loadPreview(state: RosterBatchState): Promise<void> {
  if (state.isSubmitting.value) return
  invalidate(state)
  const validation = batchValidationMessage(state.draft.value)
  if (validation) { state.error.value = validation; return }
  await fetchPreview(state)
}

async function fetchPreview(state: RosterBatchState): Promise<void> {
  const request = copyBatchRules(state.draft.value)
  if (!request) return
  const current = state.sequence
  state.controller = new AbortController()
  state.isPreviewing.value = true
  try {
    const result = await shiftBatchApi.preview(request, state.controller.signal)
    if (current !== state.sequence) return
    state.preview.value = result
    state.idempotencyKey = crypto.randomUUID()
  } catch (cause) {
    if (current === state.sequence) state.error.value = apiErrorMessage(cause)
  } finally {
    if (current === state.sequence) state.isPreviewing.value = false
  }
}

function isDefinitiveRejection(cause: unknown): cause is ApiRequestError {
  return cause instanceof ApiRequestError && cause.status < 500 && cause.status !== 429
}

function submissionError(state: RosterBatchState, cause: unknown): void {
  if (cause instanceof ApiRequestError && cause.errors.some((issue) => issue.field === 'scheduling')) {
    invalidate(state)
    state.error.value = apiErrorMessage(cause)
    return
  }
  if (isDefinitiveRejection(cause)) {
    invalidate(state)
    state.error.value = cause.status === 409 ? 'The preview has changed. Preview the batch again before confirming.' : apiErrorMessage(cause)
    return
  }
  // Preserve the preview and key when the response is lost, so retry replays the same batch.
  state.error.value = 'The save result could not be confirmed. Retry with the same conditions to retrieve the result.'
}

async function submit(state: RosterBatchState): Promise<ShiftBatchView | null> {
  if (!state.preview.value?.canSubmit || state.isSubmitting.value) return null
  if (state.isPreviewing.value || !state.idempotencyKey) return null
  const request = copyBatchRules(state.draft.value)
  if (!request) return null
  state.isSubmitting.value = true
  state.error.value = ''
  try {
    return await shiftBatchApi.create({ ...request, previewHash: state.preview.value.previewHash }, state.idempotencyKey)
  } catch (cause) {
    submissionError(state, cause)
    return null
  } finally { state.isSubmitting.value = false }
}
