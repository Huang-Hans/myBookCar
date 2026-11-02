<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { CalendarDays, Check, CheckCircle2, ChevronLeft, ChevronRight, Clock3, RefreshCw, Search, ShieldCheck, Users, X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { currentUser } from '../../api/session'
import type { DriverView, PropertyView } from '../../api/lms'
import type { ShiftPeriod, ShiftStatus } from '../../api/dispatch'
import type { ShiftBatchView } from '../../api/contracts/shiftBatches'
import { useShiftBatch } from './useShiftBatch'

const props = defineProps<{ month: string; propertyId: string; properties: PropertyView[]; drivers: DriverView[] }>()
const emit = defineEmits<{ close: []; saved: [result: ShiftBatchView] }>()
const { dateLocale, t } = useLocale()
const { draft, preview, error, isPreviewing, isSubmitting, selectedDates, loadPreview, selectPeriod, submit } = useShiftBatch(props.month, props.propertyId)
const search = ref('')
const page = ref(1)
const dialogElement = ref<HTMLElement | null>(null)
const canWrite = computed(() => currentUser.value?.permissions.includes('dispatch:write') ?? false)
const filteredDrivers = computed(() => props.drivers.filter((driver) =>
  driver.status === 'ACTIVE' && (!draft.value.propertyId || driver.propertyId === draft.value.propertyId) &&
  `${driver.name} ${driver.driverId}`.toLocaleLowerCase().includes(search.value.trim().toLocaleLowerCase())))
const allSelected = computed(() => filteredDrivers.value.length > 0 && filteredDrivers.value.every((driver) => draft.value.driverIds.includes(driver.driverId)))
const someSelected = computed(() => filteredDrivers.value.some((driver) => draft.value.driverIds.includes(driver.driverId)))
const totalPages = computed(() => Math.max(1, Math.ceil((preview.value?.items.length ?? 0) / 20)))
const previewRows = computed(() => preview.value?.items.slice((page.value - 1) * 20, page.value * 20) ?? [])
const periods: ShiftPeriod[] = ['MORNING', 'AFTERNOON', 'NIGHT']
const weekdayKeys = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
const previousOverflow = document.body.style.overflow

function close(): void { if (!isSubmitting.value) emit('close') }
function toggleAll(): void {
  const visible = new Set(filteredDrivers.value.map((driver) => driver.driverId))
  draft.value.driverIds = allSelected.value ? draft.value.driverIds.filter((id) => !visible.has(id))
    : [...new Set([...draft.value.driverIds, ...visible])]
}
function toggleWeekday(day: number): void {
  draft.value.weekdays = draft.value.weekdays.includes(day)
    ? draft.value.weekdays.filter((value) => value !== day) : [...draft.value.weekdays, day]
}
function initials(name: string): string { return name.split(/\s+/).slice(0, 2).map((part) => part[0] ?? '').join('').toUpperCase() }
function periodLabel(value?: ShiftPeriod | null): string { return value ? t({ MORNING: 'Morning', AFTERNOON: 'Afternoon', NIGHT: 'Night' }[value]) : '—' }
function periodHours(value: ShiftPeriod): string { return { MORNING: '07:00–15:00', AFTERNOON: '15:00–23:00', NIGHT: '23:00–07:00 +1' }[value] }
function statusLabel(value?: ShiftStatus | null): string { return value ? t({ ON_DUTY: 'On duty', REST: 'Resting', LEAVE: 'On leave' }[value]) : '—' }
function dateLabel(date: string): string {
  return new Intl.DateTimeFormat(dateLocale.value,
    { month: 'short', day: 'numeric', weekday: 'short', timeZone: 'UTC' }).format(new Date(`${date}T00:00:00Z`))
}
async function save(): Promise<void> {
  if (!canWrite.value) return
  const result = await submit()
  if (result) emit('saved', result)
}
function trapTab(event: KeyboardEvent): void {
  const nodes = dialogElement.value?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), [tabindex="0"]')
  const first = nodes?.[0]
  const last = nodes?.[nodes.length - 1]
  const boundary = event.shiftKey ? first : last
  if (document.activeElement !== boundary && document.activeElement !== dialogElement.value) return
  event.preventDefault()
  const target = event.shiftKey ? last : first
  target?.focus()
}
function keydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') { event.preventDefault(); close() }
  if (event.key === 'Tab') trapTab(event)
}
watch(() => draft.value.propertyId, () => { draft.value.driverIds = []; search.value = '' }, { flush: 'sync' })
watch(preview, () => { page.value = 1 })
onMounted(async () => {
  document.body.style.overflow = 'hidden'
  await nextTick()
  dialogElement.value?.focus()
})
onBeforeUnmount(() => { document.body.style.overflow = previousOverflow; previousFocus?.focus() })
</script>

<template>
  <Teleport to="body">
    <div class="modal-backdrop roster-batch-backdrop" @click.self="close" @keydown="keydown">
      <section ref="dialogElement" class="roster-batch-drawer" role="dialog" aria-modal="true" aria-labelledby="roster-batch-title" tabindex="-1" :aria-busy="isSubmitting">
        <header class="roster-batch-header"><div><h2 id="roster-batch-title">{{ t('Batch scheduling') }}</h2><p>{{ t('Schedule multiple drivers across several dates at once.') }}</p></div><button class="icon-button" :disabled="isSubmitting" :aria-label="t('Close')" @click="close"><X :size="20" /></button></header>
        <ol class="roster-batch-steps"><li :class="{ active: !preview }"><span>1</span>{{ t('Set conditions') }}</li><li :class="{ active: preview }"><span>2</span>{{ t('Preview and confirm') }}</li></ol>
        <div class="roster-batch-body">
          <fieldset class="roster-batch-form" :disabled="isSubmitting">
            <section class="roster-batch-card roster-batch-drivers"><h3>{{ t('Select drivers') }}</h3>
              <label class="field-label">{{ t('Property') }}<select v-model="draft.propertyId"><option value="">{{ t('All properties') }}</option><option v-for="item in properties" :key="item.propertyId" :value="item.propertyId">{{ item.propertyId }} · {{ item.name }}</option></select></label>
              <label class="roster-batch-search"><Search :size="16" /><input v-model="search" :aria-label="t('Search driver name or ID')" :placeholder="t('Search driver name or ID')" /></label>
              <div class="roster-batch-select-all"><label><input type="checkbox" :checked="allSelected" :indeterminate="someSelected && !allSelected" :disabled="!filteredDrivers.length" @change="toggleAll" />{{ t('Select all') }}</label><span>{{ t('Selected drivers') }} {{ draft.driverIds.length }}</span></div>
              <div class="roster-batch-driver-list"><label v-for="driver in filteredDrivers" :key="driver.driverId" class="roster-batch-driver"><input v-model="draft.driverIds" type="checkbox" :value="driver.driverId" /><span class="roster-batch-avatar">{{ initials(driver.name) }}</span><span><strong>{{ driver.name }}</strong></span></label><p v-if="!filteredDrivers.length" class="empty-cell">{{ t('No matching active drivers.') }}</p></div>
            </section>
            <section class="roster-batch-card roster-batch-rules"><h3>{{ t('Set dates and shift') }}</h3>
              <div class="field-label">{{ t('Date range') }}<div class="roster-batch-date-range"><input v-model="draft.from" type="date" :aria-label="t('From date')" /><span>→</span><input v-model="draft.to" type="date" :aria-label="t('To date')" /></div></div>
              <div class="field-label">{{ t('Repeat on weekdays') }}<div class="roster-batch-weekday-options"><div class="roster-batch-weekdays"><button v-for="(day, index) in weekdayKeys" :key="day" type="button" :class="{ selected: draft.weekdays.includes(index + 1) }" :aria-pressed="draft.weekdays.includes(index + 1)" @click="toggleWeekday(index + 1)">{{ t(day) }}</button></div><div class="roster-batch-weekday-shortcuts"><button type="button" class="text-button" @click="draft.weekdays = [1, 2, 3, 4, 5]">{{ t('Working days') }}</button><button type="button" class="text-button" @click="draft.weekdays = [1, 2, 3, 4, 5, 6, 7]">{{ t('Every day') }}</button></div></div></div>
              <div class="roster-batch-date-shortcuts"><span>{{ t('Selected dates total').replace('{count}', String(selectedDates.length)) }}</span></div>
              <div class="field-label">{{ t('Shift') }}<div class="roster-batch-periods"><label v-for="period in periods" :key="period" :class="{ selected: draft.period === period }"><input :checked="draft.period === period" type="radio" name="batch-shift-period" :value="period" @change="selectPeriod(period)" /><span><strong>{{ periodLabel(period) }}</strong><small>{{ periodHours(period) }}</small></span></label></div></div>
              <div class="roster-batch-status-role"><label class="field-label">{{ t('Status') }}<select v-model="draft.status"><option value="ON_DUTY">{{ t('On duty') }}</option><option value="REST">{{ t('Resting') }}</option><option value="LEAVE">{{ t('On leave') }}</option></select></label><label class="field-label">{{ t('Role') }}<select v-model="draft.role"><option value="PREFERRED">{{ t('Preferred') }}</option><option value="STANDBY">{{ t('Standby') }}</option></select></label></div>
              <div class="roster-batch-notice"><ShieldCheck :size="22" /><div><strong>{{ t('Only the selected period is checked. Existing shifts in that period are retained.') }}</strong><small>{{ t('Shift vehicles follow the current driver binding.') }}</small></div></div>
            </section>
          </fieldset>
          <section class="roster-batch-card roster-batch-preview"><div class="roster-batch-preview-heading"><h3>{{ t('Schedule preview') }}</h3><button class="secondary-button" :disabled="isPreviewing || isSubmitting || !canWrite" @click="loadPreview"><RefreshCw :size="14" />{{ t(isPreviewing ? 'Loading' : preview ? 'Preview again' : 'Preview') }}</button></div>
            <div v-if="preview" class="roster-batch-summary"><div><Users :size="20" /><span>{{ t('Selected drivers') }}<strong>{{ preview.driverCount }} <small>{{ t('people') }}</small></strong></span></div><div><CalendarDays :size="20" /><span>{{ t('Selected dates') }}<strong>{{ preview.dateCount }} <small>{{ t('days') }}</small></strong></span></div><div class="is-new"><CheckCircle2 :size="20" /><span>{{ t('New shifts') }}<strong>{{ preview.createCount }} <small>{{ t('items') }}</small></strong></span></div><div class="is-existing"><Clock3 :size="20" /><span>{{ t('Existing shifts') }}<strong>{{ preview.existingCount }} <small>{{ t('items') }}</small></strong></span></div></div>
            <div v-if="preview" class="roster-batch-table-wrap"><table class="roster-batch-table"><thead><tr><th>{{ t('Date') }}</th><th>{{ t('Driver') }}</th><th>{{ t('Shift') }}</th><th>{{ t('Status') }}</th><th>{{ t('Result') }}</th></tr></thead><tbody><tr v-for="item in previewRows" :key="`${item.businessDate}:${item.driverId}`"><td>{{ dateLabel(item.businessDate) }}</td><td>{{ item.driverName || item.driverId }}</td><td>{{ periodLabel(item.period) }}</td><td>{{ statusLabel(item.status) }}</td><td><span class="roster-batch-result" :class="{ 'is-existing': item.isExisting }">{{ t(item.isExisting ? 'Existing · retained' : 'New') }}</span></td></tr></tbody></table></div>
            <div v-if="preview" class="roster-batch-pager"><span>{{ (page - 1) * 20 + 1 }}–{{ Math.min(page * 20, preview.items.length) }} {{ t('of') }} {{ preview.items.length }} {{ t('records') }}</span><div><button class="secondary-button" :disabled="page <= 1 || isSubmitting" :aria-label="t('Previous page')" @click="page -= 1"><ChevronLeft :size="16" /></button><span>{{ page }} / {{ totalPages }}</span><button class="secondary-button" :disabled="page >= totalPages || isSubmitting" :aria-label="t('Next page')" @click="page += 1"><ChevronRight :size="16" /></button></div></div>
            <p v-else class="roster-batch-empty">{{ t('Set the conditions, then preview the new and existing shifts.') }}</p>
            <p v-if="preview && !preview.canSubmit" class="roster-batch-limit">{{ t('There are no new shifts to create') }}</p>
          </section>
          <p v-if="error" class="form-error" role="alert">{{ t(error) }}</p>
        </div>
        <footer class="roster-batch-footer"><p v-if="preview"><strong>{{ preview.createCount }}</strong> {{ t('new shifts') }} · <span>{{ preview.existingCount }}</span> {{ t('existing shifts retained') }}</p><p v-else>{{ t('Preview the schedule before confirming.') }}</p><div><button class="secondary-button" :disabled="isSubmitting" @click="close">{{ t('Cancel') }}</button><button class="primary-button" :disabled="!preview?.canSubmit || isSubmitting || isPreviewing || !canWrite" @click="save"><Check :size="16" /><span>{{ t(isSubmitting ? 'Saving' : 'Confirm new shifts') }}<template v-if="preview"> {{ preview.createCount }}</template></span></button></div></footer>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.roster-batch-backdrop { justify-content: flex-end; padding: 0; z-index: 100; background: #1b23413d; }
.roster-batch-drawer { width: 52vw; min-width: 760px; max-width: 960px; height: 100dvh; display: flex; flex-direction: column; background: #fff; box-shadow: -12px 0 35px #11162c24; outline: none; }
.roster-batch-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; padding: 24px 20px 10px; }
.roster-batch-header h2 { font-size: 24px; font-weight: 600; letter-spacing: 0; }
.roster-batch-header p { margin: 7px 0 0; color: #70798c; font-size: 12px; }
.roster-batch-header .icon-button { color: #697386; }
.roster-batch-steps { display: flex; justify-content: center; gap: 64px; padding: 0 20px 18px; margin: 0; list-style: none; font-size: 12px; color: #7d8498; }
.roster-batch-steps li { position: relative; display: flex; align-items: center; gap: 10px; }
.roster-batch-steps li:first-child::after { content: ''; position: absolute; left: calc(100% + 16px); width: 48px; height: 1px; background: #3540a5; }
.roster-batch-steps span { display: grid; place-items: center; width: 26px; height: 26px; border-radius: 50%; background: #eef0f6; font-size: 13px; }
.roster-batch-steps .active { color: #3540a5; font-weight: 600; }
.roster-batch-steps .active span { background: #3540a5; color: #fff; }
.roster-batch-body { flex: 1; min-height: 0; overflow: auto; padding: 0 20px 20px; }
.roster-batch-form { display: grid; grid-template-columns: minmax(240px, 0.9fr) minmax(0, 1.65fr); gap: 16px; padding: 0; margin: 0; border: 0; min-width: 0; }
.roster-batch-card { border: 1px solid #e0e3ec; border-radius: 4px; padding: 16px; min-width: 0; }
.roster-batch-card h3 { font-size: 16px; font-weight: 600; margin-bottom: 12px; letter-spacing: 0; }
.roster-batch-card .field-label { display: flex; flex-direction: column; gap: 8px; font-size: 12px; margin-bottom: 12px; letter-spacing: 0; }
.roster-batch-card input:not([type=checkbox]):not([type=radio]), .roster-batch-card select { height: 34px; border-radius: 3px; }
.roster-batch-card input[type=checkbox], .roster-batch-card input[type=radio] { width: 16px; height: 16px; padding: 0; margin: 0; accent-color: #4753bc; flex: none; }
.roster-batch-search { display: flex; gap: 8px; align-items: center; border: 1px solid #d9dce6; padding-left: 10px; color: #727c94; }
.roster-batch-search input { min-width: 0; width: 100%; padding-left: 0; border: 0; font-size: 12px; }
.roster-batch-select-all { display: flex; justify-content: space-between; gap: 8px; font-size: 11px; color: #69738a; margin: 14px 0 8px; }
.roster-batch-select-all label { display: flex; align-items: center; gap: 9px; }
.roster-batch-driver-list { height: 268px; overflow: auto; }
.roster-batch-driver { display: flex; align-items: center; gap: 12px; height: 33px; padding: 0; cursor: pointer; }
.roster-batch-avatar { width: 30px; height: 30px; display: grid; place-items: center; border-radius: 50%; color: #59647e; background: #eef0f5; font-size: 11px; flex: none; }
.roster-batch-driver strong { display: block; font-size: 12px; font-weight: 400; overflow-wrap: anywhere; }
.roster-batch-driver small { display: block; color: #8b93a3; font-size: 10px; overflow-wrap: anywhere; }
.roster-batch-date-range { display: grid; grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr); align-items: center; gap: 10px; }
.roster-batch-date-range input { min-width: 0; width: 100%; font-size: 12px; }
.roster-batch-weekday-options { display: flex; align-items: center; gap: 10px; }
.roster-batch-weekdays { display: grid; flex: 1; grid-template-columns: repeat(7, minmax(0, 1fr)); gap: 5px; }
.roster-batch-weekdays button { height: 33px; padding: 0 3px; border: 1px solid #d9dce6; border-radius: 3px; background: white; color: #647089; font-size: 11px; }
.roster-batch-weekdays button.selected, .roster-batch-periods label.selected { border-color: #737ee6; background: #eff1ff; color: #3540a5; }
.roster-batch-weekday-shortcuts { display: flex; gap: 2px; }
.roster-batch-weekday-shortcuts .text-button { padding: 4px; font-size: 11px; white-space: nowrap; }
.roster-batch-date-shortcuts { display: flex; align-items: center; margin: -7px 0 14px; color: #79829b; font-size: 11px; }
.roster-batch-periods { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; }
.roster-batch-periods label { display: flex; align-items: center; gap: 8px; min-height: 56px; padding: 9px; border: 1px solid #d9dce6; border-radius: 3px; cursor: pointer; }
.roster-batch-periods strong { display: block; font-size: 12px; }
.roster-batch-periods small { display: block; margin-top: 4px; color: #6f7893; font-size: 10px; white-space: nowrap; }
.roster-batch-status-role { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.roster-batch-notice { display: flex; gap: 10px; align-items: center; background: #edf4ff; color: #4057ae; border-radius: 3px; padding: 13px; }
.roster-batch-notice svg { flex: none; }
.roster-batch-notice strong, .roster-batch-notice small { display: block; font-size: 11px; }
.roster-batch-notice small { margin-top: 5px; color: #697c9e; font-weight: normal; }
.roster-batch-limit { margin: 12px 0 0; font-size: 11px; color: #737e94; }
.roster-batch-preview { margin-top: 16px; }
.roster-batch-preview:has(.roster-batch-table-wrap) { display: flex; flex-direction: column; height: min(420px, calc(100dvh - 480px)); min-height: 290px; }
.roster-batch-preview-heading { display: flex; justify-content: space-between; align-items: center; gap: 10px; margin-bottom: 14px; }
.roster-batch-preview-heading h3 { margin: 0; }
.roster-batch-preview-heading button, .roster-batch-footer button { display: inline-flex; align-items: center; justify-content: center; gap: 7px; }
.roster-batch-summary { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 16px; }
.roster-batch-summary > div { display: flex; align-items: center; gap: 12px; min-height: 68px; border: 1px solid #e5e8f0; border-radius: 4px; padding: 12px; color: #5a64ba; background: #f6f7ff; }
.roster-batch-summary span { color: #737e94; font-size: 10px; }
.roster-batch-summary strong { display: block; margin-top: 4px; font-size: 17px; color: #29364f; }
.roster-batch-summary strong small { font-size: 11px; font-weight: 500; }
.roster-batch-summary .is-new { color: #23964d; background: #f0faf3; border-color: #dcefe2; }
.roster-batch-summary .is-new strong { color: #23964d; }
.roster-batch-summary .is-existing { color: #cc8a16; background: #fffaf0; border-color: #f0e5cc; }
.roster-batch-summary .is-existing strong { color: #bd8018; }
.roster-batch-table-wrap { flex: 1; min-height: 0; overflow: auto; border: 1px solid #e1e4ed; }
.roster-batch-table { width: 100%; border-collapse: collapse; font-size: 11px; }
.roster-batch-table th, .roster-batch-table td { padding: 9px 10px; text-align: left; border-bottom: 1px solid #e1e4ed; white-space: nowrap; }
.roster-batch-table th + th, .roster-batch-table td + td { border-left: 1px solid #e1e4ed; }
.roster-batch-table th { background: #f5f6fa; color: #66718a; font-weight: 500; }
.roster-batch-result { display: inline-block; border-radius: 20px; background: #e2f5e6; color: #27834a; padding: 3px 8px; }
.roster-batch-result.is-existing { background: #fff1d6; color: #ae7619; }
.roster-batch-pager, .roster-batch-pager > div { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.roster-batch-pager { padding-top: 12px; font-size: 11px; color: #758098; }
.roster-batch-pager button { padding: 0 8px; display: grid; place-items: center; }
.roster-batch-empty { color: #8690a6; padding: 28px 10px; margin: 0; text-align: center; font-size: 12px; }
.roster-batch-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 16px 20px; border-top: 1px solid #e0e3ec; background: #fff; }
.roster-batch-footer p { margin: 0; font-size: 12px; color: #6a748a; }
.roster-batch-footer strong { color: #23854a; }
.roster-batch-footer p > span { color: #b57e20; }
.roster-batch-footer > div { display: flex; gap: 10px; }
.roster-batch-footer button { min-height: 36px; border-radius: 3px; }
.roster-batch-footer .secondary-button { min-width: 76px; }
.roster-batch-footer .primary-button { min-width: 168px; }
@media (max-width: 1100px) {
  .roster-batch-drawer { width: min(880px, 100%); min-width: 0; }
}
@media (max-width: 760px) {
  .roster-batch-form { grid-template-columns: 1fr; }
  .roster-batch-header, .roster-batch-footer { padding-left: 14px; padding-right: 14px; }
  .roster-batch-body { padding-left: 14px; padding-right: 14px; }
  .roster-batch-summary { grid-template-columns: repeat(2, 1fr); }
  .roster-batch-footer { flex-wrap: wrap; }
  .roster-batch-footer > div { margin-left: auto; }
  .roster-batch-weekday-options { align-items: flex-start; flex-direction: column; }
  .roster-batch-weekdays { width: 100%; }
}
@media (max-width: 420px) {
  .roster-batch-periods { grid-template-columns: 1fr; }
  .roster-batch-steps { gap: 18px; padding-left: 12px; padding-right: 12px; }
}
</style>
