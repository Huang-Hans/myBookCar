<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { propertyNames } from '../../data/demo'
import DataPager from '../../components/DataPager.vue'
import { isDemoMode } from '../../api/session'
import { lms } from '../../api/lms'
import type { CrossBorderApprovalView } from '../../api/lms'
import { ApiRequestError } from '../../api/http'
import { macauDateBoundary } from '../../api/dates'
const { t } = useLocale()

interface BorderApproval { approvalId: string; tripReference?: string; guestLabel: string; tier: string; propertyName: string; routeName: string; pickupAt: string; sla: string; documents: string; status: 'Pending' | 'Approved' | 'Rejected'; decisionComment: string; version?: number }
const tiers = ['Diamond', 'VIP', 'Platinum', 'Gold']
const route = useRoute()
const rows = ref<BorderApproval[]>(isDemoMode ? Array.from({ length: 8 }, (_, index) => ({
  approvalId: `XB-20260624-${81 + index}`, guestLabel: `Guest ${String.fromCharCode(65 + index)}`, tier: tiers[index % tiers.length] ?? 'Gold', propertyName: propertyNames[index % propertyNames.length] ?? propertyNames[0], routeName: index % 2 === 0 ? 'Hong Kong Airport → City of Dreams' : 'Studio City → Hong Kong Airport', pickupAt: `2026-06-24T${index < 4 ? '11' : '12'}:${index % 2 ? '30' : '00'}:00Z`, sla: index < 3 ? '01h 42m' : '04h 20m', documents: index === 3 ? 'Vehicle permit missing' : '4 / 4 verified', status: 'Pending' as const, decisionComment: '',
})) : [])
function mapRow(value: CrossBorderApprovalView): BorderApproval { const properties: Record<string, string> = Object.fromEntries([['COD', propertyNames[0]], ['SC', propertyNames[1]], ['ALT', propertyNames[2]], ['GH', propertyNames[3]]]); return { approvalId: value.approvalId, tripReference: value.tripReference, guestLabel: value.guest.name, tier: value.guest.tierCode, propertyName: propertyOptions.value.find((item) => item.propertyId === value.propertyId)?.name ?? properties[value.propertyId] ?? value.propertyId, routeName: `${value.route.pickupName ?? ''} → ${value.route.dropoffName ?? ''}`, pickupAt: value.pickupAt, sla: value.slaDeadlineAt ?? '—', documents: value.documents.length === 4 && value.documents.every((document) => document.isVerified) ? '4 / 4 verified' : 'Documents missing', status: value.status === 'APPROVED' ? 'Approved' : value.status === 'REJECTED' ? 'Rejected' : 'Pending', decisionComment: value.decision?.comments ?? '', version: value.version } }
const propertyFilter = ref('')
const statusFilter = ref('')
const from = ref('')
const to = ref('')
const propertyOptions = ref<{ propertyId: string; name: string }[]>([])
const availableProperties = computed(() => isDemoMode ? propertyNames : propertyOptions.value.map((item) => item.name))
const totalItems = ref(0)
const pendingCount = ref(0)
const approvalChain = ref<string[]>([])
const refreshKey = ref(0)
const selected = ref<string[]>([])
const reviewing = ref<BorderApproval | null>(null)
const decisionComment = ref('')
const reviewError = ref('')
const isBulkReviewOpen = ref(false)
const bulkComment = ref('')
const bulkError = ref('')
const page = ref(1)
const pageSize = ref(10)
watch([propertyFilter, statusFilter, from, to, () => route.query.q], () => { page.value = 1 })
watch([propertyFilter, statusFilter, from, to, page, pageSize, () => route.query.q, refreshKey], async (_values, _previous, onCleanup) => {
  if (isDemoMode) return
  const controller = new AbortController()
  onCleanup(() => controller.abort())
  const query = { page: page.value, pageSize: pageSize.value, search: String(route.query.q ?? ''), propertyId: propertyOptions.value.find((item) => item.name === propertyFilter.value)?.propertyId ?? propertyFilter.value, status: statusFilter.value ? statusFilter.value.toUpperCase() : undefined, from: from.value ? macauDateBoundary(from.value, false) : undefined, to: to.value ? macauDateBoundary(to.value, true) : undefined }
  try {
    const envelope = await lms.listApprovals(query, { signal: controller.signal })
    if (controller.signal.aborted) return
    rows.value = envelope.data.map(mapRow)
    totalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data.length
    reviewError.value = ''
  } catch (cause) { if (!controller.signal.aborted) reviewError.value = cause instanceof Error ? cause.message : 'Unable to load approvals.' }
  try {
    const summary = await lms.approvalSummary(query)
    if (!controller.signal.aborted) { pendingCount.value = summary.pendingCount; approvalChain.value = summary.approvalChain }
  } catch { /* The queue remains available when its summary is unavailable. */ }
}, { immediate: true })
if (!isDemoMode) void lms.properties().then((values) => { propertyOptions.value = values.map((value) => ({ propertyId: value.propertyId, name: value.name })); refreshKey.value++ }).catch((cause) => { reviewError.value = cause instanceof Error ? cause.message : 'Unable to load properties.' })
const filtered = computed(() => isDemoMode ? rows.value.filter((row) => (!propertyFilter.value || row.propertyName === propertyFilter.value) && (!statusFilter.value || row.status === statusFilter.value) && (!from.value || row.pickupAt.slice(0, 10) >= from.value) && (!to.value || row.pickupAt.slice(0, 10) <= to.value) && (!route.query.q || `${row.approvalId} ${row.guestLabel} ${row.routeName}`.toLowerCase().includes(String(route.query.q).toLowerCase()))) : rows.value)
const visible = computed(() => isDemoMode ? filtered.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : filtered.value)
function toggle(id: string): void { selected.value = selected.value.includes(id) ? selected.value.filter((item) => item !== id) : [...selected.value, id] }
async function openReview(row: BorderApproval): Promise<void> {
  reviewing.value = row; decisionComment.value = row.decisionComment; reviewError.value = ''
  if (!isDemoMode) {
    try { reviewing.value = mapRow(await lms.approval(row.approvalId)) }
    catch (cause) { reviewError.value = cause instanceof Error ? cause.message : 'Unable to load approval details.' }
  }
}
async function review(status: BorderApproval['status']): Promise<void> {
  if (!reviewing.value) return
  if (status === 'Rejected' && !decisionComment.value.trim()) { reviewError.value = 'A rejection comment is required.'; return }
  if (status === 'Approved' && reviewing.value.documents !== '4 / 4 verified') { reviewError.value = 'All required documents must be verified before approval.'; return }
  if (!isDemoMode) {
    if (!decisionComment.value.trim()) { reviewError.value = 'A decision comment is required.'; return }
    try { await lms.decideApproval(reviewing.value.approvalId, reviewing.value.version ?? 0, status === 'Approved' ? 'APPROVED' : 'REJECTED', decisionComment.value.trim()); reviewing.value = null; refreshKey.value++ }
    catch (cause) { reviewError.value = cause instanceof ApiRequestError ? `${cause.message} · traceId: ${cause.traceId}` : cause instanceof Error ? cause.message : 'Unable to decide approval.' }
    return
  }
  reviewing.value.status = status
  reviewing.value.decisionComment = decisionComment.value.trim()
  reviewing.value = null
}
async function bulkApprove(): Promise<void> {
  if (!isDemoMode) {
    const comments = bulkComment.value.trim()
    if (!comments) { bulkError.value = 'A decision comment is required.'; return }
    try {
      const chosen = rows.value.filter((item) => selected.value.includes(item.approvalId))
      if (chosen.some((item) => item.documents !== '4 / 4 verified')) { bulkError.value = 'All selected requests must have verified documents.'; return }
      const result = await lms.bulkDecideApprovals(chosen.map((item) => ({ approvalId: item.approvalId, expectedVersion: item.version ?? 0 })), 'APPROVED', comments)
      rows.value = rows.value.map((row) => { const decided = result.items.find((item) => item.approvalId === row.approvalId); return decided ? { ...row, status: 'Approved', version: decided.version, decisionComment: comments } : row })
      selected.value = []; isBulkReviewOpen.value = false; bulkComment.value = ''; bulkError.value = ''
      refreshKey.value++
    } catch (cause) { bulkError.value = cause instanceof Error ? cause.message : 'Unable to approve selected requests.' }
    return
  }
  rows.value.forEach((row) => { if (selected.value.includes(row.approvalId) && row.documents === '4 / 4 verified') row.status = 'Approved' }); selected.value = []
}
</script>

<template>
  <div class="page-heading"><h1>{{ t('Cross-border Approvals') }}</h1><p>{{ t('Hong Kong–Macau document verification and approval chains.') }}</p></div>
  <p v-if="reviewError && !reviewing" class="form-error" role="alert">{{ t(reviewError) }}</p>
  <section class="filter-bar portal-filter"><label>{{ t('Property') }}<select v-model="propertyFilter"><option value="">{{ t('All properties') }}</option><option v-for="name in availableProperties" :key="name" :value="name">{{ name }}</option></select></label><label>{{ t('Status') }}<select v-model="statusFilter"><option value="">{{ t('All statuses') }}</option><option value="Pending">{{ t('Pending') }}</option><option value="Approved">{{ t('Approved') }}</option><option value="Rejected">{{ t('Rejected') }}</option></select></label><label>{{ t('From') }}<input v-model="from" type="date" /></label><label>{{ t('To') }}<input v-model="to" type="date" /></label><button class="text-button" @click="propertyFilter = ''; statusFilter = ''; from = ''; to = ''">{{ t('Reset') }}</button><span class="filter-note">{{ t('Filters apply immediately') }}</span></section>
  <div class="approval-summary"><strong>{{ isDemoMode ? rows.filter((row) => row.status === 'Pending').length : pendingCount }}</strong> {{ t('pending requests') }} <template v-if="isDemoMode"><strong>{{ rows.filter((row) => row.status === 'Pending' && row.sla.startsWith('01')).length }}</strong> {{ t('urgent · SLA monitored') }}</template> <span>{{ isDemoMode ? t('Approval chain: Property CS → Operations Manager → Border compliance') : approvalChain.join(' → ') }}</span></div>
  <section class="panel"><div class="panel-heading"><h2>{{ t('Cross-border Approval Queue') }}</h2><button class="primary-button" :disabled="selected.length === 0" @click="isDemoMode ? bulkApprove() : isBulkReviewOpen = true">{{ t('BULK REVIEW') }} ({{ selected.length }})</button></div><div class="table-scroll"><table class="data-table portal-table" :aria-label="t('Cross-border Approval Queue')"><thead><tr><th scope="col"><input type="checkbox" :aria-label="t('Select all approvals')" :checked="selected.length === visible.length && visible.length > 0" @change="selected = selected.length === visible.length ? [] : visible.map((row) => row.approvalId)" /></th><th scope="col">{{ t('TRIP REFERENCE ↕') }}</th><th scope="col">{{ t('GUEST ↕') }}</th><th scope="col">{{ t('TIER ↕') }}</th><th scope="col">{{ t('PROPERTY ↕') }}</th><th scope="col">{{ t('ROUTE ↕') }}</th><th scope="col">{{ t('PICKUP TIME ↕') }}</th><th scope="col">{{ t('SLA ↕') }}</th><th scope="col">{{ t('DOCUMENTS ↕') }}</th><th scope="col">{{ t('STATUS ↕') }}</th><th scope="col">{{ t('ACTIONS') }}</th></tr></thead><tbody><tr v-for="row in visible" :key="row.approvalId"><td><input type="checkbox" :aria-label="`Select ${row.approvalId}`" :checked="selected.includes(row.approvalId)" @change="toggle(row.approvalId)" /></td><td class="mono">{{ row.tripReference ?? row.approvalId }}</td><td>{{ row.guestLabel }}</td><td>{{ t(row.tier) }}</td><td>{{ row.propertyName }}</td><td>{{ row.routeName }}</td><td class="mono">{{ row.pickupAt.slice(0, 16) }}</td><td>{{ row.sla }}</td><td>{{ t(row.documents) }}</td><td><span class="status-tag" :class="row.status.toLowerCase()">● {{ t(row.status) }}</span></td><td class="table-actions"><button @click="openReview(row)">{{ t('Review') }}</button></td></tr><tr v-if="visible.length === 0"><td colspan="11" class="empty-cell">{{ t('No matching records') }}</td></tr></tbody></table></div><DataPager v-model:page="page" v-model:pageSize="pageSize" :total-items="isDemoMode ? filtered.length : totalItems" /></section>
  <div v-if="reviewing" class="modal-backdrop" @click.self="reviewing = null"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Review approval')"><header class="dialog-header"><h2>{{ t('Review') }} {{ reviewing.approvalId }}</h2><button class="icon-button" :aria-label="t('Close')" @click="reviewing = null">×</button></header><div class="dialog-body"><p><strong>{{ t('Route:') }}</strong> {{ reviewing.routeName }}</p><p><strong>{{ t('Documents:') }}</strong> {{ t(reviewing.documents) }}</p><label class="field-label">{{ t('Decision comment') }}<textarea v-model="decisionComment" rows="3" :placeholder="t('Enter the approval or rejection comment')" /></label><p v-if="reviewError" class="form-error">{{ t(reviewError) }}</p><p class="muted">{{ t(isDemoMode ? 'Demo decision only. A production approval requires server-side authorization and an audit trail.' : 'The decision is submitted to LMS with your identity and audit trace.') }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="review('Rejected')">{{ t('REJECT') }}</button><button class="primary-button" :disabled="reviewing.documents !== '4 / 4 verified'" @click="review('Approved')">{{ t('APPROVE') }}</button></footer></section></div>
<div v-if="isBulkReviewOpen" class="modal-backdrop" @click.self="isBulkReviewOpen = false"><section class="dialog small-dialog" role="dialog" aria-modal="true"><header class="dialog-header"><h2>{{ t('BULK REVIEW') }} ({{ selected.length }})</h2><button class="icon-button" :aria-label="t('Close')" @click="isBulkReviewOpen = false">?</button></header><div class="dialog-body"><label class="field-label">{{ t('Decision comment') }}<textarea v-model="bulkComment" rows="3" /></label><p v-if="bulkError" class="form-error" role="alert">{{ t(bulkError) }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="isBulkReviewOpen = false">{{ t('CANCEL') }}</button><button class="primary-button" @click="bulkApprove">{{ t('APPROVE') }}</button></footer></section></div>
</template>
