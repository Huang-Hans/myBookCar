<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { apiErrorMessage, type Pagination } from '../../api/http'
import { dispatchApi, type CreateCouponRequest, type PatchCouponRequest, type CouponScope, type CouponStatus, type CouponType, type CouponView, type CouponSummaryView } from '../../api/dispatch'
import { lms, type GuestSummaryView, type PropertyView } from '../../api/lms'
import { currentUser } from '../../api/session'
import type { CouponDraft } from './contracts/couponDraft'

const { t } = useLocale()
const search = ref('')
const status = ref<CouponStatus | ''>('')
const kind = ref<CouponType | ''>('')
const page = ref(1)
const pagination = ref<Pagination | null>(null)
const coupons = ref<CouponView[]>([])
const summary = ref<CouponSummaryView>({ active: 0, issuedQuantity: 0, redeemedCount: 0, upcoming: 0 })
const properties = ref<PropertyView[]>([])
const members = ref<GuestSummaryView[]>([])
const isMembersLoading = ref(false)
const memberLoadError = ref('')
const memberOptions = computed(() => [...new Map(members.value.filter(member => member.memberId?.trim())
  .map(member => [member.memberId!.trim(), member])).values()])
const hasCurrentMember = computed(() => memberOptions.value.some(member => member.memberId === draft.value.memberId))
const selected = ref<CouponView | null>(null)
const editingCoupon = ref<CouponView | null>(null)
const deletingCoupon = ref<CouponView | null>(null)
const deleteError = ref('')
const successMessage = ref('')
const dialog = ref(false)
const draft = ref(emptyDraft())
const formError = ref('')
const loadError = ref('')
const isLoading = ref(false)
const isSaving = ref(false)
const isReading = ref(false)
const isBusy = computed(() => isSaving.value || isReading.value)
const canWrite = computed(() => currentUser.value?.permissions.includes('dispatch:write') ?? false)
const hasAllProperties = computed(() => currentUser.value?.hasAllPropertyAccess ?? false)
const dialogTitle = computed(() => editingCoupon.value ? 'Edit coupon' : 'New coupon')
let searchTimer: ReturnType<typeof setTimeout> | undefined
let requestNumber = 0
let memberRequestNumber = 0

const metrics = computed(() => [
  { label: 'In effect', value: summary.value.active, note: 'coupons' },
  { label: 'Issued', value: summary.value.issuedQuantity, note: 'quota' },
  { label: 'Redeemed', value: summary.value.redeemedCount, note: 'uses' },
  { label: 'Coming soon', value: summary.value.upcoming, note: 'scheduled' },
])

function emptyDraft(): CouponDraft {
  return { code: '', name: '', memberId: '', type: 'FREE_TRIP', faceAmount: '1', scope: 'ALL_PROPERTIES', propertyId: '', vehicleRestriction: '', validFrom: '', validTo: '', issuedQuantity: '100' }
}
function typeLabel(type: CouponType): string { return new Map<CouponType, string>([['FREE_TRIP', 'Complimentary trip'], ['DISCOUNT', 'Percent off'], ['FARE_DEDUCTION', 'Fare credit']]).get(type) ?? type }
function statusLabel(value: CouponStatus): string { return new Map<CouponStatus, string>([['ACTIVE', 'Active'], ['UPCOMING', 'Coming soon'], ['EXHAUSTED', 'Exhausted'], ['DISABLED', 'Disabled'], ['EXPIRED', 'Expired']]).get(value) ?? value }
function scopeLabel(value: CouponScope): string { return new Map<CouponScope, string>([['ALL_PROPERTIES', 'All properties'], ['PROPERTY', 'Property'], ['VEHICLE', 'Vehicle']]).get(value) ?? value }
function propertyName(id: string | null): string { return properties.value.find((item) => item.propertyId === id)?.name ?? id ?? '' }
function couponScope(coupon: CouponView): string { return coupon.scope === 'PROPERTY' ? propertyName(coupon.propertyId) : coupon.scope === 'VEHICLE' ? coupon.vehicleRestriction ?? '' : scopeLabel(coupon.scope) }
function faceLabel(coupon: CouponView): string { return coupon.type === 'FREE_TRIP' ? `${coupon.faceAmount} trip` : coupon.type === 'DISCOUNT' ? `${coupon.faceAmount}%` : `MOP ${coupon.faceAmount}` }
function tone(value: CouponStatus): string { return new Map<CouponStatus, string>([['ACTIVE', 'active'], ['UPCOMING', 'pending'], ['EXHAUSTED', 'off-duty'], ['DISABLED', 'off-duty'], ['EXPIRED', 'cancelled']]).get(value) ?? 'off-duty' }

async function load(): Promise<void> {
  const current = ++requestNumber
  isLoading.value = true
  loadError.value = ''
  try {
    const [list, totals] = await Promise.all([
      dispatchApi.coupons({ page: page.value, pageSize: 20, search: search.value.trim() || undefined, type: kind.value || undefined, status: status.value || undefined }),
      dispatchApi.couponSummary(),
    ])
    if (current !== requestNumber) return
    coupons.value = list.data
    pagination.value = list.meta?.pagination ?? null
    summary.value = totals
  } catch (cause) { if (current === requestNumber) loadError.value = apiErrorMessage(cause) }
  finally { if (current === requestNumber) isLoading.value = false }
}
async function loadMemberOptions(): Promise<void> {
  const current = ++memberRequestNumber
  isMembersLoading.value = true
  memberLoadError.value = ''
  try {
    const items: GuestSummaryView[] = []
    let memberPage = 1
    let totalPages = 1
    while (memberPage <= totalPages) {
      const response = await lms.listGuestPage({ page: memberPage, pageSize: 50 })
      if (current !== memberRequestNumber) return
      items.push(...response.data)
      totalPages = response.meta?.pagination?.totalPages ?? 1
      memberPage++
    }
    members.value = items
  } catch (cause) { if (current === memberRequestNumber) memberLoadError.value = apiErrorMessage(cause) }
  finally { if (current === memberRequestNumber) isMembersLoading.value = false }
}
watch(dialog, (isOpen) => {
  members.value = []; memberLoadError.value = ''
  if (isOpen) { void loadMemberOptions(); return }
  memberRequestNumber++; isMembersLoading.value = false
})
function scheduleLoad(): void {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => { void load() }, 250)
}
watch([search, status, kind], () => { page.value = 1; scheduleLoad() })
function changePage(next: number): void { page.value = next; void load() }
function resetFilters(): void { search.value = ''; status.value = ''; kind.value = '' }
function canManageCoupon(coupon: CouponView): boolean {
  return canWrite.value && (coupon.scope === 'PROPERTY' || hasAllProperties.value)
}
function openCreate(): void {
  if (!canWrite.value || isBusy.value) return
  editingCoupon.value = null
  draft.value = emptyDraft()
  if (!hasAllProperties.value) {
    draft.value.scope = 'PROPERTY'
    draft.value.propertyId = currentUser.value?.propertyId ?? ''
  }
  formError.value = ''; successMessage.value = ''; dialog.value = true
}
async function openCoupon(couponId: string): Promise<void> {
  if (isBusy.value) return
  isReading.value = true
  loadError.value = ''
  try { selected.value = await dispatchApi.coupon(couponId) }
  catch (cause) { loadError.value = apiErrorMessage(cause) }
  finally { isReading.value = false }
}
function draftFrom(coupon: CouponView): CouponDraft {
  return {
    code: coupon.code, name: coupon.name, memberId: coupon.memberId, type: coupon.type,
    faceAmount: String(coupon.faceAmount), scope: coupon.scope, propertyId: coupon.propertyId ?? '',
    vehicleRestriction: coupon.vehicleRestriction ?? '', validFrom: coupon.validFrom,
    validTo: coupon.validTo, issuedQuantity: String(coupon.issuedQuantity),
  }
}
async function openEdit(coupon: CouponView): Promise<void> {
  if (!canManageCoupon(coupon) || isBusy.value) return
  isReading.value = true
  loadError.value = ''
  try {
    const detail = await dispatchApi.coupon(coupon.couponId)
    if (!canManageCoupon(detail)) return
    editingCoupon.value = detail
    draft.value = draftFrom(detail)
    selected.value = null; formError.value = ''; successMessage.value = ''; dialog.value = true
  } catch (cause) { loadError.value = apiErrorMessage(cause) }
  finally { isReading.value = false }
}
function closeEditor(): void { if (!isSaving.value) dialog.value = false }
function requiredError(value: CouponDraft): string {
  if (![value.code, value.name, value.memberId, value.validFrom, value.validTo].every((field) => field.trim())) return 'Complete the required coupon fields.'
  return value.validTo < value.validFrom ? 'The end date must be on or after the start date.' : ''
}
function isPositiveInt32(value: number): boolean {
  return Number.isInteger(value) && value >= 1 && value <= 2147483647
}
function faceError(value: CouponDraft): string {
  const faceAmount = Number(value.faceAmount)
  if (!isPositiveInt32(faceAmount)) return 'Enter a positive whole-number coupon value.'
  return value.type === 'DISCOUNT' && faceAmount > 100 ? 'The discount must be between 1 and 100.' : ''
}
function quotaError(value: CouponDraft): string {
  const issuedQuantity = Number(value.issuedQuantity)
  if (!isPositiveInt32(issuedQuantity)) return 'Enter a positive whole-number quota.'
  return issuedQuantity < (editingCoupon.value?.redeemedCount ?? 0) ? 'Quota cannot be lower than the redeemed count.' : ''
}
function scopeError(value: CouponDraft): string {
  return value.scope === 'PROPERTY' && !value.propertyId ? 'Select a property for this coupon.' : ''
}
function couponBody(value: CouponDraft): CreateCouponRequest {
  return {
    code: value.code.trim().toUpperCase(), name: value.name.trim(), memberId: value.memberId.trim(),
    type: value.type, faceAmount: Number(value.faceAmount), scope: value.scope,
    validFrom: value.validFrom, validTo: value.validTo, issuedQuantity: Number(value.issuedQuantity),
    ...(value.scope === 'PROPERTY' ? { propertyId: value.propertyId } : {}),
    ...(value.scope === 'VEHICLE' ? { vehicleRestriction: 'MAYBACH' } : {}),
  }
}
async function persistCoupon(): Promise<void> {
  const body = couponBody(draft.value)
  const original = editingCoupon.value
  if (!original) { await dispatchApi.createCoupon(body); return }
  const fields: Record<string, unknown> = { ...original }
  const changes: PatchCouponRequest = Object.fromEntries(Object.entries(body).filter(([key, value]) => fields[key] !== value))
  if (Object.keys(changes).length > 0) await dispatchApi.updateCoupon(original.couponId, changes, original.version)
}
async function saveCoupon(): Promise<void> {
  if (!canWrite.value || isBusy.value || isMembersLoading.value) return
  formError.value = [requiredError(draft.value), faceError(draft.value), quotaError(draft.value), scopeError(draft.value)].find(Boolean) ?? ''
  if (formError.value) return
  isSaving.value = true
  try {
    await persistCoupon()
    successMessage.value = editingCoupon.value ? 'Coupon updated.' : 'Coupon created.'
    dialog.value = false
    if (!editingCoupon.value) page.value = 1
    await load()
  } catch (cause) { formError.value = apiErrorMessage(cause) }
  finally { isSaving.value = false }
}
function openDelete(coupon: CouponView): void {
  if (!canManageCoupon(coupon) || isBusy.value) return
  deletingCoupon.value = coupon; deleteError.value = ''; successMessage.value = ''
}
function closeDelete(): void { if (!isSaving.value) deletingCoupon.value = null }
async function deleteCoupon(): Promise<void> {
  const coupon = deletingCoupon.value
  if (!coupon || !canManageCoupon(coupon) || isBusy.value) return
  isSaving.value = true
  deleteError.value = ''
  try {
    await dispatchApi.removeCoupon(coupon.couponId)
    deletingCoupon.value = null; selected.value = null; successMessage.value = 'Coupon deleted.'
    if (coupons.value.length === 1 && page.value > 1) page.value--
    await load()
  } catch (cause) { deleteError.value = apiErrorMessage(cause) }
  finally { isSaving.value = false }
}
onMounted(() => { void load(); void lms.properties().then((items) => { properties.value = items }).catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) }) })
onUnmounted(() => { memberRequestNumber++; if (searchTimer) clearTimeout(searchTimer) })
</script>

<template>
  <p v-if="successMessage" class="panel-note" role="status">{{ t(successMessage) }}</p>
  <div class="ops-metrics">
    <article v-for="metric in metrics" :key="metric.label" class="ops-metric"><span>{{ t(metric.label) }}</span><strong>{{ metric.value }}</strong><small>{{ t(metric.note) }}</small></article>
  </div>
  <section class="panel">
    <div class="panel-heading"><h2>{{ t('Coupon list') }} <span class="count-badge">{{ pagination?.totalItems ?? coupons.length }}</span></h2><button v-if="canWrite" class="primary-button" :disabled="isBusy" @click="openCreate">{{ t('New coupon') }}</button></div>
    <div class="history-filters">
      <label class="field-label expand-field">{{ t('CODE / NAME') }}<input v-model="search" :placeholder="t('Search coupon code or name')" /></label>
      <label class="field-label">{{ t('TYPE') }}<select v-model="kind"><option value="">{{ t('All types') }}</option><option value="FREE_TRIP">{{ t('Complimentary trip') }}</option><option value="FARE_DEDUCTION">{{ t('Fare credit') }}</option><option value="DISCOUNT">{{ t('Percent off') }}</option></select></label>
      <label class="field-label">{{ t('STATUS') }}<select v-model="status"><option value="">{{ t('All') }}</option><option value="ACTIVE">{{ t('Active') }}</option><option value="UPCOMING">{{ t('Coming soon') }}</option><option value="EXHAUSTED">{{ t('Exhausted') }}</option><option value="DISABLED">{{ t('Disabled') }}</option><option value="EXPIRED">{{ t('Expired') }}</option></select></label>
      <button class="secondary-button" @click="resetFilters">{{ t('RESET') }}</button>
    </div>
    <p v-if="loadError" class="form-error" role="alert">{{ loadError }}</p>
    <div class="table-scroll"><table class="data-table console-table" :aria-label="t('Coupon Management')"><thead><tr><th scope="col">{{ t('CODE / NAME') }}</th><th scope="col">{{ t('TYPE') }}</th><th scope="col">{{ t('VALUE') }}</th><th scope="col">{{ t('SCOPE') }}</th><th scope="col">{{ t('VALIDITY') }}</th><th scope="col">{{ t('REDEEMED') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col"></th></tr></thead><tbody>
      <tr v-for="coupon in coupons" :key="coupon.couponId">
        <td><strong>{{ coupon.code }}</strong><small>{{ coupon.name }}</small></td><td>{{ t(typeLabel(coupon.type)) }}</td><td>{{ faceLabel(coupon) }}</td><td>{{ t(couponScope(coupon)) }}</td><td>{{ coupon.validFrom }}<small>{{ coupon.validTo }}</small></td><td>{{ coupon.redeemedCount }} / {{ coupon.issuedQuantity }}</td><td><span class="status-tag" :class="tone(coupon.status)">{{ t(statusLabel(coupon.status)) }}</span></td>
        <td class="table-actions"><button :disabled="isBusy" @click="openCoupon(coupon.couponId)">{{ t('VIEW →') }}</button><button v-if="canManageCoupon(coupon)" :disabled="isBusy" @click="openEdit(coupon)">{{ t('Edit') }}</button><button v-if="canManageCoupon(coupon)" class="danger-action" :disabled="isBusy" @click="openDelete(coupon)">{{ t('Delete') }}</button></td>
      </tr>
      <tr v-if="!isLoading && coupons.length === 0"><td colspan="8" class="empty-cell">{{ t('No matching coupons') }}</td></tr>
      <tr v-if="isLoading && coupons.length === 0"><td colspan="8" class="empty-cell">{{ t('Loading') }}</td></tr>
    </tbody></table></div>
    <div v-if="pagination && pagination.totalPages > 1" class="list-pager"><button :disabled="page <= 1" @click="changePage(page - 1)">‹</button><span>{{ page }} / {{ pagination.totalPages }}</span><button :disabled="page >= pagination.totalPages" @click="changePage(page + 1)">›</button></div>
  </section>
  <div v-if="selected" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Coupon details')"><header class="dialog-header"><h2>{{ selected.code }}</h2><button class="icon-button" :aria-label="t('Close')" :disabled="isBusy" @click="selected = null"><X :size="18" /></button></header><div class="dialog-body"><p><strong>{{ selected.name }}</strong></p><p>{{ t('Type') }}: {{ t(typeLabel(selected.type)) }} · {{ faceLabel(selected) }}</p><p>{{ t('Scope') }}: {{ t(couponScope(selected)) }}</p><p>{{ t('Validity') }}: {{ selected.validFrom }} – {{ selected.validTo }}</p><p>{{ t('Redeemed') }}: {{ selected.redeemedCount }} / {{ selected.issuedQuantity }}</p><p><span class="status-tag" :class="tone(selected.status)">{{ t(statusLabel(selected.status)) }}</span></p><p v-if="loadError" class="form-error" role="alert">{{ loadError }}</p></div>
    <footer class="dialog-footer"><button v-if="canManageCoupon(selected)" class="secondary-button" :disabled="isBusy" @click="openEdit(selected)">{{ t('Edit') }}</button><button v-if="canManageCoupon(selected)" class="secondary-button danger-action" :disabled="isBusy" @click="openDelete(selected)">{{ t('Delete') }}</button><button class="secondary-button" :disabled="isBusy" @click="selected = null">{{ t('CLOSE') }}</button></footer>
  </section></div>
  <div v-if="dialog" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t(dialogTitle)"><header class="dialog-header"><h2>{{ t(dialogTitle) }}</h2><button class="icon-button" :aria-label="t('Close')" :disabled="isSaving" @click="closeEditor"><X :size="18" /></button></header><div class="dialog-body"><fieldset class="coupon-fields" :disabled="isSaving">
    <label class="field-label">{{ t('Coupon code') }}<input v-model="draft.code" /></label><label class="field-label">{{ t('Name') }}<input v-model="draft.name" /></label>
    <label class="field-label">{{ t('Member ID') }}
      <select v-model="draft.memberId" :disabled="isMembersLoading" required>
        <option value="" disabled>{{ t(isMembersLoading ? 'Loading members' : 'Select a member') }}</option>
        <option v-if="draft.memberId && !hasCurrentMember" :value="draft.memberId">{{ draft.memberId }}</option>
        <option v-for="member in memberOptions" :key="member.memberId!" :value="member.memberId">{{ member.memberId }} · {{ member.name }}</option>
      </select>
      <small v-if="!isMembersLoading && !memberLoadError && memberOptions.length === 0" class="muted">{{ t('No matching members') }}</small>
    </label>
    <p v-if="memberLoadError" class="form-error" role="alert">{{ memberLoadError }} <button type="button" class="text-button" :disabled="isMembersLoading || isSaving" @click="loadMemberOptions">{{ t('Retry') }}</button></p>
    <label class="field-label">{{ t('Type') }}<select v-model="draft.type"><option value="FREE_TRIP">{{ t('Complimentary trip') }}</option><option value="FARE_DEDUCTION">{{ t('Fare credit') }}</option><option value="DISCOUNT">{{ t('Percent off') }}</option></select></label>
    <label class="field-label">{{ t('Value') }}<input v-model="draft.faceAmount" type="number" min="1" :max="draft.type === 'DISCOUNT' ? 100 : 2147483647" step="1" /></label>
    <label class="field-label">{{ t('Scope') }}<select v-model="draft.scope"><option v-if="hasAllProperties" value="ALL_PROPERTIES">{{ t('All properties') }}</option><option value="PROPERTY">{{ t('Property') }}</option><option v-if="hasAllProperties" value="VEHICLE">{{ t('Vehicle') }}</option></select></label>
    <label v-if="draft.scope === 'PROPERTY'" class="field-label">{{ t('Property') }}<select v-model="draft.propertyId"><option value="">{{ t('Select property') }}</option><option v-for="item in properties" :key="item.propertyId" :value="item.propertyId">{{ item.name }}</option></select></label>
    <p v-if="draft.scope === 'VEHICLE'" class="muted">MAYBACH</p>
    <label class="field-label">{{ t('From') }}<input v-model="draft.validFrom" type="date" /></label><label class="field-label">{{ t('To') }}<input v-model="draft.validTo" type="date" :min="draft.validFrom" /></label><label class="field-label">{{ t('Quota') }}<input v-model="draft.issuedQuantity" type="number" :min="Math.max(1, editingCoupon?.redeemedCount ?? 0)" max="2147483647" step="1" /></label>
    </fieldset><p v-if="formError" class="form-error" role="alert">{{ t(formError) }}</p>
  </div><footer class="dialog-footer"><button class="secondary-button" :disabled="isSaving" @click="closeEditor">{{ t('CLOSE') }}</button><button class="primary-button" :disabled="isSaving || isMembersLoading || !draft.memberId" @click="saveCoupon">{{ t(isSaving ? 'Saving' : 'Save') }}</button></footer></section></div>
  <div v-if="deletingCoupon" class="modal-backdrop">
    <section class="dialog small-dialog" role="alertdialog" aria-modal="true" aria-labelledby="coupon-delete-title" aria-describedby="coupon-delete-description">
      <header class="dialog-header"><h2 id="coupon-delete-title">{{ t('Delete coupon') }}</h2><button class="icon-button" :disabled="isSaving" :aria-label="t('Close')" @click="closeDelete"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ deletingCoupon.code }}</strong> · {{ deletingCoupon.name }}</p>
        <p id="coupon-delete-description">{{ t('Delete this coupon? The coupon will be removed and audit records will be retained.') }}</p>
        <p v-if="deleteError" class="form-error" role="alert">{{ t(deleteError) }}</p>
      </div>
      <footer class="dialog-footer"><button class="secondary-button" :disabled="isSaving" @click="closeDelete">{{ t('Cancel') }}</button><button class="primary-button danger-button" :disabled="isSaving" @click="deleteCoupon">{{ t(isSaving ? 'Deleting' : 'Delete') }}</button></footer>
    </section>
  </div>
</template>
<style scoped>
.coupon-fields { border:0; margin:0; padding:0; min-width:0; }
.danger-action { color:#b42318; }
.danger-button { background:#b42318; }
.list-pager { display:flex; justify-content:flex-end; align-items:center; gap:12px; padding:12px 16px; }
.list-pager button { min-width:32px; min-height:32px; border:1px solid #e1e4ec; background:#fff; color:#3f4cb8; }
.list-pager button:disabled { opacity:.4; }
</style>
