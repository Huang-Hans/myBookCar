<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { ChevronLeft, ChevronRight, ListFilter, X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { apiErrorMessage, type SuccessEnvelope } from '../../api/http'
import { dispatchApi, type ShiftBoardView, type ShiftPeriod, type ShiftRole, type ShiftStatus, type ShiftView } from '../../api/dispatch'
import { lms, type DriverView, type PropertyView } from '../../api/lms'
import { macauDayKey } from '../../stores/fleet'
import { mondayOffset, monthDates, shiftMonth } from './rosterCalendar'
import { currentUser } from '../../api/session'
import type { ShiftBatchView } from '../../api/contracts/shiftBatches'
import ConsoleRosterBatchDrawer from './ConsoleRosterBatchDrawer.vue'
import ConsoleRosterWithdrawalDialog from './ConsoleRosterWithdrawalDialog.vue'
import ConsoleRosterVehicleDialog from './ConsoleRosterVehicleDialog.vue'
import type { ShiftWithdrawalView } from '../../api/contracts/shiftWithdrawals'

interface ShiftDraft { driverId: string; period: ShiftPeriod; status: ShiftStatus; role: ShiftRole }
const { dateLocale, t } = useLocale()
const selectedDate = ref(macauDayKey(0))
const currentDate = selectedDate.value
const monthKey = ref(selectedDate.value.slice(0, 7))
const propertyId = ref('')
const properties = ref<PropertyView[]>([])
const drivers = ref<DriverView[]>([])
const monthBoards = ref<Record<string, ShiftBoardView>>({})
const selectedShift = ref<ShiftView | null>(null)
const dialog = ref(false)
const draft = ref(emptyDraft())
const formError = ref('')
const isReadingShift = ref(false)
const loadError = ref('')
const isLoading = ref(false)
const isSaving = ref(false)
const batchDialog = ref(false)
const batchResult = ref<ShiftBatchView | null>(null)
const withdrawal = ref<{ shifts: ShiftView[]; businessDate: string; period: ShiftPeriod; propertyId: string; scopeLabel: string; isWholePeriod: boolean } | null>(null)
const withdrawalResult = ref<ShiftWithdrawalView | null>(null)
const canChangeDriverVehicle = computed(() => ['drivers:read', 'drivers:write'].every(permission => currentUser.value?.permissions.includes(permission)))
const canBatchSchedule = computed(() => currentUser.value?.permissions.includes('dispatch:write') ?? false)
let requestNumber = 0
let monthRequest: AbortController | null = null

const dates = computed(() => monthDates(monthKey.value))
const calendarCells = computed(() => [...Array<string | null>(mondayOffset(dates.value[0] ?? `${monthKey.value}-01`)).fill(null), ...dates.value])
const monthTitle = computed(() => new Intl.DateTimeFormat(dateLocale.value, { year: 'numeric', month: 'long', timeZone: 'UTC' }).format(new Date(`${monthKey.value}-01T00:00:00Z`)))
const selectedDateLabel = computed(() => new Intl.DateTimeFormat(dateLocale.value, { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long', timeZone: 'UTC' }).format(new Date(`${selectedDate.value}T00:00:00Z`)))
const weekdayLabels = computed(() => Array.from({ length: 7 }, (_, index) => new Intl.DateTimeFormat(dateLocale.value, { weekday: 'short', timeZone: 'UTC' }).format(new Date(Date.UTC(2026, 9, 19 + index)))))
const board = computed(() => monthBoards.value[selectedDate.value] ?? null)
const columns = computed(() => board.value?.periods ?? [])
const shiftsByDate = computed<Record<string, ShiftView[]>>(() => Object.fromEntries(
  Object.entries(monthBoards.value).map(([date, day]) => [date,
    [...new Map(day.periods.flatMap((period) => period.shifts).map((shift) => [shift.driverId, shift])).values()]])))
function shiftsForDate(date: string): ShiftView[] { return shiftsByDate.value[date] ?? [] }
const schedulable = computed(() => drivers.value.filter((item) => item.status === 'ACTIVE' && (!propertyId.value || item.propertyId === propertyId.value)))
function emptyDraft(): ShiftDraft { return { driverId: '', period: 'MORNING', status: 'ON_DUTY', role: 'PREFERRED' } }
function periodLabel(value: ShiftPeriod): string { return new Map<ShiftPeriod, string>([['MORNING', 'Morning'], ['AFTERNOON', 'Afternoon'], ['NIGHT', 'Night']]).get(value) ?? value }
function statusLabel(value: ShiftStatus): string { return new Map<ShiftStatus, string>([['ON_DUTY', 'On duty'], ['REST', 'Resting'], ['LEAVE', 'On leave']]).get(value) ?? value }
function tone(value: ShiftStatus): string { return new Map<ShiftStatus, string>([['ON_DUTY', 'active'], ['REST', 'off-duty'], ['LEAVE', 'pending']]).get(value) ?? 'off-duty' }
function propertyName(id: string): string { return properties.value.find((item) => item.propertyId === id)?.name ?? id }
function periodHours(startsAt: string, endsAt: string, endsNextDay: boolean): string { return `${startsAt}–${endsAt}${endsNextDay ? ' +1' : ''}` }

async function loadAllPages<T>(fetchPage: (page: number) => Promise<SuccessEnvelope<T[]>>): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page += 1) {
    const result = await fetchPage(page)
    items.push(...result.data)
    const pagination = result.meta?.pagination
    if (result.data.length === 0 || (pagination ? page >= pagination.totalPages : result.data.length < 50)) return items
  }
}

async function loadMonth(): Promise<void> {
  const current = ++requestNumber
  const property = propertyId.value || undefined
  const from = `${monthKey.value}-01`
  const to = dates.value.at(-1) ?? from
  monthRequest?.abort()
  monthRequest = new AbortController()
  monthBoards.value = {}
  isLoading.value = true
  loadError.value = ''
  try {
    const boards = await dispatchApi.shiftCalendar(from, to, property, monthRequest.signal)
    if (current === requestNumber) monthBoards.value = Object.fromEntries(boards.map((item) => [item.businessDate, item]))
  } catch (cause) {
    if (current === requestNumber) loadError.value = apiErrorMessage(cause)
  } finally {
    if (current === requestNumber) isLoading.value = false
  }
}
async function reloadSelectedBoard(): Promise<void> {
  const current = requestNumber
  const date = selectedDate.value
  const result = await dispatchApi.shiftBoard(date, propertyId.value || undefined)
  if (current === requestNumber) monthBoards.value = { ...monthBoards.value, [date]: result }
}
function changeMonth(offset: number): void {
  monthKey.value = shiftMonth(monthKey.value, offset)
  selectedDate.value = monthKey.value === currentDate.slice(0, 7) ? currentDate : `${monthKey.value}-01`
}
function goToToday(): void {
  const monthChanged = monthKey.value !== currentDate.slice(0, 7)
  monthKey.value = currentDate.slice(0, 7)
  selectedDate.value = currentDate
  if (!monthChanged && board.value === null && !isLoading.value) void loadMonth()
}
watch([monthKey, propertyId], () => { void loadMonth() }, { immediate: true })
onBeforeUnmount(() => { requestNumber += 1; monthRequest?.abort() })
function openCreate(): void { draft.value = emptyDraft(); formError.value = ''; dialog.value = true }
async function batchSaved(result: ShiftBatchView): Promise<void> {
  batchDialog.value = false
  batchResult.value = result
  await loadMonth()
}
function openWithdrawal(period: ShiftPeriod, shift?: ShiftView): void {
  if (!canBatchSchedule.value || isLoading.value) return
  const scope = shift?.propertyId ?? propertyId.value
  withdrawal.value = { shifts: shift ? [shift] : [...(columns.value.find(column => column.period === period)?.shifts ?? [])],
    businessDate: selectedDate.value, period, propertyId: scope,
    scopeLabel: scope ? propertyName(scope) : t('All visible properties'), isWholePeriod: !shift }
  withdrawalResult.value = null
}
async function withdrawalSaved(result: ShiftWithdrawalView): Promise<void> {
  withdrawal.value = null
  withdrawalResult.value = result
  batchResult.value = null
  await loadMonth()
}
async function refreshWithdrawal(): Promise<void> { withdrawal.value = null; await loadMonth() }
async function saveShift(): Promise<void> {
  if (!draft.value.driverId) { formError.value = 'Select a driver.'; return }
  isSaving.value = true
  formError.value = ''
  try {
    await dispatchApi.createShift({ businessDate: selectedDate.value, ...draft.value })
    dialog.value = false
    await reloadSelectedBoard()
  } catch (cause) { formError.value = apiErrorMessage(cause) }
  finally { isSaving.value = false }
}
async function openAssign(shiftId: string): Promise<void> {
  if (!canChangeDriverVehicle.value || isReadingShift.value) return
  isReadingShift.value = true; loadError.value = ''
  try { selectedShift.value = await dispatchApi.shift(shiftId) }
  catch (cause) { loadError.value = apiErrorMessage(cause) }
  finally { isReadingShift.value = false }
}
async function vehicleSaved(): Promise<void> {
  selectedShift.value = null
  await loadMonth()
}
onMounted(() => {
  void Promise.all([
    lms.properties(),
    loadAllPages((page) => lms.listDrivers({ page, pageSize: 50 })),
  ])
    .then(([propertyRows, driverRows]) => { properties.value = propertyRows; drivers.value = driverRows })
    .catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) })
})
</script>

<template>
  <h1 class="roster-page-title">{{ t('Driver Roster') }}</h1>
  <section class="panel roster-panel">
    <div class="roster-calendar-toolbar">
      <div class="roster-month-navigation"><button type="button" class="secondary-button" :aria-label="t('Previous month')" @click="changeMonth(-1)"><ChevronLeft :size="17" /></button><h3>{{ monthTitle }}</h3><button type="button" class="secondary-button" :aria-label="t('Next month')" @click="changeMonth(1)"><ChevronRight :size="17" /></button></div>
      <div class="roster-heading-actions"><button class="secondary-button" @click="openCreate">{{ t('New shift') }}</button><button v-if="canBatchSchedule" class="primary-button" @click="batchDialog = true; batchResult = null">{{ t('Batch scheduling') }}</button></div>
      <div class="roster-calendar-view-options">
        <label class="roster-property-filter">
          <ListFilter :size="16" aria-hidden="true" /><span>{{ t('Property') }}</span>
          <select v-model="propertyId" :aria-label="t('PROPERTY')">
            <option value="">{{ t('All properties') }}</option>
            <option v-for="item in properties" :key="item.propertyId" :value="item.propertyId">{{ item.name }}</option>
          </select>
        </label>
        <button type="button" class="secondary-button roster-today-button" @click="goToToday">{{ t('Today') }}</button>
      </div>
    </div>
    <p v-if="batchResult" class="roster-batch-success" role="status">{{ t('Batch scheduling completed.') }} {{ t('Created shifts') }} {{ batchResult.createdCount }} · {{ t('Existing shifts') }} {{ batchResult.skippedCount }}</p>
    <div class="roster-calendar" role="group" :aria-label="t('Monthly roster')">
      <div v-for="weekday in weekdayLabels" :key="weekday" class="roster-weekday">{{ weekday }}</div>
      <template v-for="(date, index) in calendarCells" :key="date ?? `blank-${index}`">
        <div v-if="!date" class="roster-calendar-empty" aria-hidden="true" />
        <button v-else type="button" class="roster-calendar-day" :class="{ active: selectedDate === date, today: currentDate === date }" :aria-label="date" :aria-pressed="selectedDate === date" @click="selectedDate = date">
          <span class="roster-calendar-date">{{ Number(date.slice(-2)) }}<small v-if="currentDate === date">{{ t('Today') }}</small></span>
          <template v-if="monthBoards[date]"><span class="roster-calendar-count">{{ shiftsForDate(date).length }} {{ t('Drivers scheduled') }}</span><span v-for="shift in shiftsForDate(date).slice(0, 3)" :key="shift.shiftId" class="roster-calendar-driver">{{ shift.driverName || shift.driverId }}</span><span v-if="shiftsForDate(date).length > 3" class="roster-calendar-more">+{{ shiftsForDate(date).length - 3 }}</span></template>
          <span v-else-if="isLoading" class="roster-calendar-muted">…</span>
          <span v-else class="roster-calendar-muted">—</span>
        </button>
      </template>
    </div>
    <p v-if="loadError" class="form-error" role="alert">{{ loadError }}</p>
    <p v-if="withdrawalResult" class="roster-batch-success" role="status">{{ withdrawalResult.businessDate }} · {{ t(periodLabel(withdrawalResult.period)) }} · {{ t('Withdrawn shifts') }} {{ withdrawalResult.withdrawnCount }}</p>
    <div class="roster-selected-heading"><h3>{{ selectedDateLabel }}</h3><span v-if="board">{{ shiftsForDate(selectedDate).length }} {{ t('Drivers scheduled') }}</span></div>
    <div class="roster-board"><section v-for="column in columns" :key="column.period" class="roster-column"><header><div><strong>{{ t(periodLabel(column.period)) }}</strong><small>{{ periodHours(column.startsAt, column.endsAt, column.endsNextDay) }}</small></div><div class="roster-period-actions"><span class="count-badge">{{ column.count }}</span><button v-if="canBatchSchedule" class="text-button roster-withdraw-button" :disabled="isLoading || column.shifts.length === 0" @click="openWithdrawal(column.period)">{{ t('Withdraw period shifts') }}</button></div></header>
      <article v-for="item in column.shifts" :key="item.shiftId" class="roster-card"><strong>{{ item.driverName || item.driverId }}</strong><small>{{ propertyName(item.propertyId) }} · {{ item.vehiclePlate || t('No bound vehicle') }}</small><span class="status-tag" :class="tone(item.status)">{{ t(statusLabel(item.status)) }}</span><button v-if="canChangeDriverVehicle" class="text-button" :disabled="isReadingShift" @click="openAssign(item.shiftId)">{{ t(item.vehiclePlate ? 'Change vehicle' : 'Bind vehicle') }}</button><button v-if="canBatchSchedule" class="text-button roster-withdraw-button" :disabled="isLoading" @click="openWithdrawal(item.period, item)">{{ t('Withdraw shift') }}</button></article>
      <p v-if="!isLoading && column.shifts.length === 0" class="empty-cell">{{ t('No shifts') }}</p>
    </section></div>
    <p v-if="isLoading && !board" class="empty-cell">{{ t('Loading') }}</p>
    <div class="panel-note">{{ t('All times in Macau (GMT+8)') }}</div>
  </section>
  <ConsoleRosterBatchDrawer v-if="batchDialog" :month="monthKey" :property-id="propertyId" :properties="properties" :drivers="drivers" @close="batchDialog = false" @saved="batchSaved" />
  <ConsoleRosterWithdrawalDialog v-if="withdrawal" v-bind="withdrawal" @close="withdrawal = null" @saved="withdrawalSaved" @refresh="refreshWithdrawal" />
  <div v-if="dialog" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('New shift')"><header class="dialog-header"><h2>{{ t('New shift') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="dialog = false"><X :size="18" /></button></header><div class="dialog-body">
    <p class="muted">{{ selectedDate }}</p>
    <label class="field-label">{{ t('Driver') }}<select v-model="draft.driverId"><option value="">{{ t('Select a driver') }}</option><option v-for="driver in schedulable" :key="driver.driverId" :value="driver.driverId">{{ driver.name }}</option></select></label>
    <label class="field-label">{{ t('Shift') }}<select v-model="draft.period"><option value="MORNING">{{ t('Morning') }}</option><option value="AFTERNOON">{{ t('Afternoon') }}</option><option value="NIGHT">{{ t('Night') }}</option></select></label>
    <label class="field-label">{{ t('Status') }}<select v-model="draft.status"><option value="ON_DUTY">{{ t('On duty') }}</option><option value="REST">{{ t('Resting') }}</option><option value="LEAVE">{{ t('On leave') }}</option></select></label>
    <label class="field-label">{{ t('Role') }}<select v-model="draft.role"><option value="PREFERRED">{{ t('Preferred') }}</option><option value="STANDBY">{{ t('Standby') }}</option></select></label>
    <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
  </div><footer class="dialog-footer"><button class="secondary-button" @click="dialog = false">{{ t('CLOSE') }}</button><button class="primary-button" :disabled="isSaving" @click="saveShift">{{ t('Save') }}</button></footer></section></div>
  <ConsoleRosterVehicleDialog v-if="selectedShift" :key="selectedShift.shiftId" :shift="selectedShift" @close="selectedShift = null" @saved="vehicleSaved" />
</template>

<style scoped>
.roster-heading-actions { display: flex; gap: 8px; }
.roster-period-actions { display: flex; align-items: center; justify-content: flex-end; gap: 10px; flex-wrap: wrap; }
.roster-withdraw-button { color: #b23b45; }
.roster-page-title { margin: 0 0 22px; font-size: 24px; line-height: 30px; font-weight: 600; letter-spacing: 0; }
.roster-panel { padding: 0; border: 0; background: transparent; }
.roster-panel .roster-calendar-toolbar { flex-wrap: wrap; gap: 12px; padding: 0 0 16px; }
.roster-panel .roster-month-navigation { gap: 8px; }
.roster-panel .roster-month-navigation h3 { min-width: 132px; font-size: 17px; text-align: center; }
.roster-panel .roster-month-navigation .secondary-button { width: 34px; height: 34px; }
.roster-panel .roster-heading-actions { margin-left: 16px; }
.roster-panel .roster-heading-actions button { height: 34px; padding: 0 20px; border-radius: 3px; }
.roster-calendar-view-options { display: flex; align-items: center; gap: 12px; margin-left: auto; }
.roster-property-filter { display: flex; align-items: center; min-width: 0; gap: 8px; color: #6d778c; }
.roster-property-filter > svg, .roster-property-filter > span { flex-shrink: 0; }
.roster-property-filter > span { white-space: nowrap; }
.roster-property-filter select { width: 200px; max-width: 100%; height: 34px; padding: 0 28px 0 10px; border: 1px solid #d7dcec; border-radius: 3px; color: #242838; background: #fff; }
.roster-property-filter:focus-within { color: #3540a5; }
.roster-property-filter option { color: #242838; }
.roster-panel .roster-calendar { margin: 0 0 18px; border-color: #dfe2e8; }
.roster-panel .roster-weekday { padding: 9px 8px; border-color: #dfe2e8; background: #f5f6f9; font-size: 12px; font-weight: 400; }
.roster-panel .roster-calendar-day { min-height: 114px; gap: 5px; padding: 9px; border-color: #dfe2e8; }
.roster-panel .roster-calendar-date { font-size: 13px; font-weight: 600; }
.roster-panel .roster-calendar-count, .roster-panel .roster-calendar-driver, .roster-panel .roster-calendar-more { font-size: 11px; }
.roster-panel .roster-selected-heading { margin: 0 0 12px; }
.roster-panel .roster-selected-heading h3 { font-size: 16px; font-weight: 600; }
.roster-panel .roster-board { gap: 12px; }
.roster-panel .roster-column { border-radius: 3px; }
.roster-panel .panel-note { display: none; }
.roster-batch-success { margin: 0 16px 14px; padding: 12px; background: #edf7ef; color: #287d46; font-size: 12px; }
@media (max-width: 750px) {
  .roster-panel .roster-heading-actions { flex-wrap: wrap; margin-left: 0; }
  .roster-calendar-view-options { width: 100%; justify-content: flex-end; }
  .roster-property-filter { flex: 1; }
  .roster-property-filter select { flex: 1; min-width: 0; width: auto; }
}
</style>
