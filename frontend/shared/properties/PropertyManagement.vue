<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import type { PropertyDraft, PropertyPatch, PropertyRecord, PropertyService, PropertyStatus } from './contracts'
import { propertyTextFields } from './fields'

const props = defineProps<{
  service: PropertyService
  translate: (key: string) => string
  errorMessage: (error: unknown) => string
  canRead: boolean
  canWrite: boolean
  canCreate: boolean
  connected: boolean
  initialSearch?: string
  hideHeading?: boolean
}>()
const emit = defineEmits<{ saved: [] }>()
const t = (key: string): string => props.translate(key)
const rows = ref<PropertyRecord[]>([])
const totalItems = ref(0)
const page = ref(1)
const pageSize = ref(20)
const search = ref(props.initialSearch ?? '')
const status = ref('')
const loading = ref(false)
const readingId = ref('')
const error = ref('')
const success = ref('')
const dialog = ref<'view' | 'create' | 'edit' | 'delete' | null>(null)
const original = ref<PropertyRecord | null>(null)
const draft = reactive<PropertyDraft>(emptyDraft())
const saving = ref(false)
const saveError = ref('')
const fieldErrors = ref<Record<string, string>>({})
const form = ref<HTMLFormElement | null>(null)
const dialogPanel = ref<HTMLElement | null>(null)
const totalPages = computed(() => Math.max(1, Math.ceil(totalItems.value / pageSize.value)))
const visiblePages = computed(() => {
  const first = Math.max(1, Math.min(page.value - 2, totalPages.value - 4))
  return Array.from({ length: Math.min(5, totalPages.value) }, (_, index) => first + index)
})
const isViewing = computed(() => dialog.value === 'view')
const dialogTitle = computed(() => dialog.value === 'create' ? 'New property' : dialog.value === 'delete' ? 'Delete property' : isViewing.value ? 'Property details' : 'Edit property')
let listRequest = 0
let detailRequest = 0
let timer: ReturnType<typeof setTimeout> | undefined
let previousFocus: HTMLElement | null = null
let previousOverflow = ''
let creationKey = ''
let creationPayload = ''

function emptyDraft(): PropertyDraft {
  return { propertyId: '', name: '', address: '', contactPerson: '', contactPhone: '', assignedFleetCount: 0,
    operatingHours: '', serviceLevel: '', minimumLeadTimeHours: 0, cancellationPolicyReference: '',
    membershipEligibility: '', vipOverrideRule: '', isActive: true }
}
async function load(): Promise<void> {
  const request = ++listRequest
  loading.value = false
  if (!props.connected || !props.canRead) { rows.value = []; totalItems.value = 0; return }
  loading.value = true; error.value = ''
  try {
    const result = await props.service.list({ page: page.value, pageSize: pageSize.value,
      search: search.value.trim() || undefined, status: status.value ? status.value as PropertyStatus : undefined })
    if (request !== listRequest) return
    rows.value = result.data; totalItems.value = result.meta?.pagination?.totalItems ?? result.data.length
  } catch (cause) { if (request === listRequest) { rows.value = []; error.value = props.errorMessage(cause) } }
  finally { if (request === listRequest) loading.value = false }
}
function scheduleLoad(): void {
  clearTimeout(timer); listRequest += 1
  timer = setTimeout(() => { void load() }, 200)
}
function resetFilters(): void { search.value = ''; status.value = ''; page.value = 1 }
async function open(mode: 'view' | 'create' | 'edit' | 'delete', row?: PropertyRecord): Promise<void> {
  const request = ++detailRequest
  readingId.value = row?.propertyId ?? ''
  previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  error.value = ''; saveError.value = ''; fieldErrors.value = {}
  try {
    const value = row ? await props.service.detail(row.propertyId) : null
    if (request !== detailRequest) return
    original.value = value; Object.assign(draft, emptyDraft())
    if (value) {
      for (const key of Object.keys(emptyDraft()) as (keyof PropertyDraft)[]) Object.assign(draft, { [key]: value[key] })
    }
    creationKey = ''; creationPayload = ''
    dialog.value = mode
  } catch (cause) { if (request === detailRequest) error.value = props.errorMessage(cause) }
  finally { if (request === detailRequest) readingId.value = '' }
}
function close(): void { if (!saving.value) dialog.value = null }
function validate(): boolean {
  const issues: Record<string, string> = {}
  if (!/^[A-Z0-9][A-Z0-9_-]{0,31}$/.test(draft.propertyId)) issues.propertyId = t('Use 1 to 32 uppercase letters, digits, underscores or hyphens')
  if (!draft.name.trim()) issues.name = t('Is required and must not be blank')
  for (const key of ['assignedFleetCount', 'minimumLeadTimeHours'] as const) {
    if (!Number.isInteger(draft[key]) || draft[key] < 0 || draft[key] > 2147483647) issues[key] = t('Enter a non-negative whole number.')
  }
  fieldErrors.value = issues
  return Object.keys(issues).length === 0 && (form.value?.reportValidity() ?? true)
}
function normalizedDraft(): PropertyDraft {
  const result = { ...draft, propertyId: draft.propertyId.trim().toUpperCase() }
  for (const field of propertyTextFields) result[field.key] = result[field.key].trim()
  return result
}
function changes(next: PropertyDraft): PropertyPatch {
  const result: PropertyPatch = {}
  for (const key of [...propertyTextFields.map(field => field.key), 'assignedFleetCount', 'minimumLeadTimeHours', 'isActive'] as const) {
    if (next[key] !== original.value?.[key]) Object.assign(result, { [key]: next[key] })
  }
  return result
}
async function persist(): Promise<void> {
  const value = normalizedDraft()
  if (dialog.value !== 'create') {
    const patch = changes(value)
    if (Object.keys(patch).length) await props.service.update(value.propertyId, patch, original.value!.version)
    return
  }
  const payload = JSON.stringify(value)
  if (!creationKey || creationPayload !== payload) { creationKey = newKey(); creationPayload = payload }
  await props.service.create(value, creationKey)
}
async function remove(): Promise<void> {
  if (saving.value || !original.value) return
  saving.value = true; saveError.value = ''
  try {
    await props.service.remove(original.value.propertyId, original.value.version)
    success.value = t('Property deleted.'); dialog.value = null
    emit('saved'); await load()
  } catch (cause) { showSaveError(cause) }
  finally { saving.value = false }
}
function newKey(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID()
  return Array.from(crypto.getRandomValues(new Uint8Array(16)), value => value.toString(16).padStart(2, '0')).join('')
}
function showSaveError(cause: unknown): void {
  saveError.value = props.errorMessage(cause)
  if (!(cause instanceof Object) || !('errors' in cause) || !Array.isArray(cause.errors)) return
  for (const issue of cause.errors as unknown[]) {
    if (issue && typeof issue === 'object' && 'field' in issue && 'message' in issue
      && typeof issue.field === 'string' && typeof issue.message === 'string') fieldErrors.value[issue.field] = t(issue.message)
  }
}
async function save(): Promise<void> {
  if (saving.value || isViewing.value || !validate()) return
  saving.value = true; saveError.value = ''
  const message = dialog.value === 'create' ? 'Property created.' : 'Property updated.'
  try {
    await persist(); success.value = t(message); dialog.value = null
    emit('saved'); await load()
  } catch (cause) { showSaveError(cause) }
  finally { saving.value = false }
}
function keyboard(event: KeyboardEvent): void {
  if (event.key === 'Escape') { event.preventDefault(); close(); return }
  if (event.key !== 'Tab') return
  const targets = Array.from(dialogPanel.value?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled)') ?? [])
  const first = targets[0]; const last = targets.at(-1)
  if (!first || !last) return
  if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
  if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
}
watch([search, status], () => { page.value = 1; scheduleLoad() })
watch([page, pageSize], scheduleLoad)
watch(() => props.initialSearch, value => { if (value !== undefined) search.value = value })
watch(() => [props.canRead, props.connected], () => { void load() }, { immediate: true })
watch(dialog, async (value, before) => {
  if (value) {
    if (!before) { previousOverflow = document.body.style.overflow; document.body.style.overflow = 'hidden' }
    await nextTick(); dialogPanel.value?.focus()
  } else { document.body.style.overflow = previousOverflow; previousFocus?.focus() }
})
onBeforeUnmount(() => {
  listRequest += 1; detailRequest += 1; clearTimeout(timer)
  if (dialog.value) document.body.style.overflow = previousOverflow
})
</script>

<template>
  <div class="property-management">
    <header v-if="!hideHeading" class="page-heading"><h1>{{ t('Property Management') }}</h1><p>{{ t('Maintain property details, contacts and service settings.') }}</p></header>
    <p v-if="success" class="success-banner" role="status">{{ success }}</p>
    <p v-if="error" class="form-error" role="alert">{{ error }} <button class="text-button" @click="load">{{ t('Retry') }}</button></p>
    <p v-if="!connected" class="panel property-notice">{{ t('Property management requires a connection to LMS.') }}</p>
    <p v-else-if="!canRead" class="panel property-notice">{{ t('You do not have permission to view properties.') }}</p>
    <section v-else class="panel">
      <div class="panel-heading">
        <h2>{{ t('Property list') }} <span class="count-badge">{{ totalItems }}</span></h2>
        <button v-if="canWrite && canCreate" class="primary-button" @click="open('create')">{{ t('New property') }}</button>
      </div>
      <div class="history-filters property-filters">
        <label class="field-label expand-field">{{ t('Property code / name') }}<input v-model="search" :placeholder="t('Search property code, name or contact')" /></label>
        <label class="field-label">{{ t('Status') }}<select v-model="status"><option value="">{{ t('All') }}</option><option value="active">{{ t('Enabled') }}</option><option value="inactive">{{ t('Disabled') }}</option><option value="deleted">{{ t('Deleted') }}</option></select></label>
        <button class="secondary-button" @click="resetFilters">{{ t('Reset') }}</button>
      </div>
      <div class="table-scroll"><table class="data-table console-table" :aria-label="t('Property list')"><thead><tr><th scope="col">{{ t('Property code / name') }}</th><th scope="col">{{ t('Address') }}</th><th scope="col">{{ t('Contact person') }}</th><th scope="col">{{ t('Contact phone') }}</th><th scope="col">{{ t('Operating hours') }}</th><th scope="col">{{ t('Status') }}</th><th scope="col"><span class="visually-hidden">{{ t('Actions') }}</span></th></tr></thead>
        <tbody><tr v-for="row in rows" :key="row.propertyId">
          <td><strong>{{ row.name }}</strong><small>{{ row.propertyId }}</small></td><td>{{ row.address || '—' }}</td><td>{{ row.contactPerson || '—' }}</td><td>{{ row.contactPhone || '—' }}</td><td>{{ row.operatingHours || '—' }}</td>
          <td><span class="status-tag" :class="row.isActive && !row.isDeleted ? 'active' : 'off-duty'">{{ t(row.isDeleted ? 'Deleted' : row.isActive ? 'Enabled' : 'Disabled') }}</span></td>
          <td class="table-actions"><button :disabled="!!readingId" @click="open('view', row)">{{ t('VIEW →') }}</button><button v-if="canWrite && !row.isDeleted" :disabled="!!readingId" @click="open('edit', row)">{{ t('Edit') }}</button><button v-if="canWrite && !row.isDeleted" :disabled="!!readingId" @click="open('delete', row)">{{ t('Delete') }}</button></td>
        </tr><tr v-if="loading || rows.length === 0"><td colspan="7" class="empty-cell">{{ t(loading ? 'Loading' : 'No matching properties.') }}</td></tr></tbody>
      </table></div>
      <div class="data-pager property-pager"><span>{{ totalItems === 0 ? 0 : (page - 1) * pageSize + 1 }}–{{ Math.min(page * pageSize, totalItems) }} {{ t('of') }} {{ totalItems }} {{ t('records') }}</span>
        <div class="pager-controls"><select v-model.number="pageSize" :aria-label="t('Page size')" @change="page = 1"><option :value="10">{{ t('10 / page') }}</option><option :value="20">{{ t('20 / page') }}</option><option :value="50">{{ t('50 / page') }}</option></select>
          <button :disabled="page <= 1 || loading" :aria-label="t('Previous page')" @click="page--">‹</button><button v-for="number in visiblePages" :key="number" :class="{ selected: number === page }" :aria-current="number === page ? 'page' : undefined" :disabled="loading" @click="page = number">{{ number }}</button><button :disabled="page >= totalPages || loading" :aria-label="t('Next page')" @click="page++">›</button></div>
      </div>
    </section>
    <Teleport to="body"><div v-if="dialog" class="modal-backdrop" @click.self="close">
      <section ref="dialogPanel" class="dialog property-dialog" :class="{ 'small-dialog': dialog === 'delete' }" role="dialog" aria-modal="true" tabindex="-1" :aria-label="t(dialogTitle)" @keydown="keyboard">
        <header class="dialog-header"><h2>{{ t(dialogTitle) }}</h2><button type="button" class="icon-button" :disabled="saving" :aria-label="t('Close')" @click="close"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="m18 6-12 12M6 6l12 12" /></svg></button></header>
        <template v-if="dialog === 'delete'">
          <div class="dialog-body"><p><strong>{{ original?.name }}</strong> · {{ original?.propertyId }}</p><p>{{ t('Delete this property? Deletion is allowed only when no business records reference it. History will be retained.') }}</p><p v-if="saveError" class="form-error" role="alert">{{ saveError }}</p></div>
          <footer class="dialog-footer"><button class="secondary-button" :disabled="saving" @click="close">{{ t('Cancel') }}</button><button class="danger-button" :disabled="saving" @click="remove">{{ t(saving ? 'Deleting' : 'Delete') }}</button></footer>
        </template>
        <form v-else ref="form" @submit.prevent="save">
          <div class="dialog-body"><fieldset :disabled="saving || isViewing" class="property-form">
            <label class="field-label">{{ t('Property code') }} *<input v-model="draft.propertyId" required maxlength="32" pattern="[A-Z0-9][A-Z0-9_-]{0,31}" :disabled="dialog !== 'create'" @input="draft.propertyId = draft.propertyId.toUpperCase()" /><small v-if="fieldErrors.propertyId" class="form-error">{{ fieldErrors.propertyId }}</small></label>
            <label class="field-label">{{ t('Status') }}<select v-model="draft.isActive"><option :value="true">{{ t('Enabled') }}</option><option :value="false">{{ t('Disabled') }}</option></select><small v-if="fieldErrors.isActive" class="form-error">{{ fieldErrors.isActive }}</small></label>
            <label v-for="field in propertyTextFields" :key="field.key" class="field-label">{{ t(field.label) }}{{ 'required' in field ? ' *' : '' }}<input v-model="draft[field.key]" :required="'required' in field" /><small v-if="fieldErrors[field.key]" class="form-error">{{ fieldErrors[field.key] }}</small></label>
            <label class="field-label">{{ t('Assigned fleet count') }}<input v-model.number="draft.assignedFleetCount" type="number" min="0" max="2147483647" step="1" required /><small v-if="fieldErrors.assignedFleetCount" class="form-error">{{ fieldErrors.assignedFleetCount }}</small></label>
            <label class="field-label">{{ t('Minimum lead time (hours)') }}<input v-model.number="draft.minimumLeadTimeHours" type="number" min="0" max="2147483647" step="1" required /><small v-if="fieldErrors.minimumLeadTimeHours" class="form-error">{{ fieldErrors.minimumLeadTimeHours }}</small></label>
          </fieldset><p v-if="isViewing" class="muted">{{ t('Daily quota') }}: {{ original?.dailyQuota ?? '—' }}</p><p v-if="saveError" class="form-error" role="alert">{{ saveError }}</p></div>
          <footer class="dialog-footer"><button type="button" class="secondary-button" :disabled="saving" @click="close">{{ t(isViewing ? 'Close' : 'Cancel') }}</button><button v-if="!isViewing" class="primary-button" type="submit" :disabled="saving || !canWrite">{{ t(saving ? 'Saving' : 'Save') }}</button></footer>
        </form>
      </section>
    </div></Teleport>
  </div>
</template>

<style scoped>
.property-filters .expand-field { min-width:200px }
.property-notice { padding:24px }
.visually-hidden { position:absolute; width:1px; height:1px; overflow:hidden; clip-path:inset(50%); white-space:nowrap }
.property-dialog { display:flex; flex-direction:column; overflow:hidden }
.property-dialog .dialog-header,.property-dialog .dialog-footer { flex-shrink:0 }
.property-dialog form { display:flex; flex-direction:column; min-height:0 }
.property-dialog .dialog-body { overflow:auto; min-height:0 }
.property-form { border:0; margin:0; padding:0; display:grid; grid-template-columns:1fr 1fr; gap:16px }
.property-form .field-label { min-width:0 }
.property-form input,.property-form select { width:100%; box-sizing:border-box }
small.form-error { display:block; margin-top:4px }
@media(max-width:640px) { .property-form { grid-template-columns:1fr } .property-filters .field-label { flex:1; min-width:0; width:100% } .property-filters .expand-field { flex-basis:100% } .property-pager { flex-wrap:wrap; gap:10px } .property-pager .pager-controls { flex-wrap:wrap } }
</style>
