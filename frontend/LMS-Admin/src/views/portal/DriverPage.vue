<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useDemoStore } from '../../stores/demo'
import { propertyNames } from '../../data/demo'
import type { Driver, DriverStatus } from '../../data/demo'
import DataPager from '../../components/DataPager.vue'
import { isDemoMode } from '../../api/session'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { lms, mapDriver } from '../../api/lms'
import VehicleSelect from '../../../../shared/vehicles/VehicleSelect.vue'
const { t } = useLocale()

const store = useDemoStore()
const route = useRoute()
const propertyFilter = ref('')
const statusFilter = ref('')
const tab = ref('All drivers')
const sortColumn = ref<keyof Driver>('fullName')
const sortDirection = ref<1 | -1>(1)
const page = ref(1)
const pageSize = ref(10)
const liveDrivers = ref<Driver[]>([])
const totalItems = ref(0)
const propertyOptions = ref<{ propertyId: string; name: string }[]>([])
const refreshKey = ref(0)
const dialog = ref<'add' | 'edit' | 'view' | null>(null)
const draft = ref<Driver>(emptyDriver())
const isVehiclesLoading = ref(false)
const originalDriver = ref<Driver | null>(null)
const vehiclePropertyId = computed(() => propertyOptions.value.find(property => property.name === draft.value.propertyName)?.propertyId
  ?? (draft.value.propertyName || ''))
const error = ref('')
const dialogPanel = ref<HTMLElement | null>(null)
let dialogTrigger: HTMLElement | null = null
let previousBodyOverflow = ''
function displayedPhone(value: string): string { return isDemoMode ? value : value.replace(/.(?=.{4})/g, '•') }
const driverDetails = computed(() => [
  { label: 'Driver ID', value: draft.value.driverId },
  { label: 'License number', value: draft.value.licenseNumber },
  { label: 'License expiry date', value: draft.value.licenseExpiryDate },
  { label: 'Phone', value: displayedPhone(draft.value.phoneMasked) },
  { label: 'Property', value: draft.value.propertyName },
  { label: 'Assigned vehicle', value: draft.value.vehicleName },
  { label: 'Status', value: t(draft.value.status) },
  { label: 'Availability', value: t(draft.value.availability) },
  { label: 'Languages spoken', value: draft.value.languagesSpoken },
  { label: 'Emergency contact', value: draft.value.emergencyContact },
  { label: 'Emergency contact phone', value: draft.value.emergencyContactPhone },
  { label: 'Blacklist reason', value: draft.value.blacklistReason },
  { label: 'Blacklist effective date', value: draft.value.blacklistEffectiveDate },
  { label: 'Blacklist expiry date', value: draft.value.blacklistExpiryDate },
])
function closeDialog(): void {
  dialog.value = null
  void nextTick(() => dialogTrigger?.focus())
}
function onDialogKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') { closeDialog(); return }
  if (event.key !== 'Tab' || !dialogPanel.value) return
  const controls = [...dialogPanel.value.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])')]
  const first = controls[0]
  const last = controls.at(-1)
  if (event.shiftKey && document.activeElement === first && last) { event.preventDefault(); last.focus() }
  else if (!event.shiftKey && document.activeElement === last && first) { event.preventDefault(); first.focus() }
}
watch(dialog, (mode) => {
  if (mode) {
    if (document.body.style.overflow !== 'hidden') previousBodyOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
  } else document.body.style.overflow = previousBodyOverflow
  if (mode) void nextTick(() => dialogPanel.value?.querySelector<HTMLElement>('button')?.focus())
})
onUnmounted(() => { document.body.style.overflow = previousBodyOverflow })

function emptyDriver(): Driver { const propertyName = isDemoMode ? propertyNames[0] : propertyOptions.value[0]?.name ?? ''; return { driverId: '', fullName: '', licenseNumber: '', licenseExpiryDate: '', phoneMasked: '', propertyName, vehicleName: '', availability: 'Off Duty', status: 'Active', emergencyContact: '', emergencyContactPhone: '', languagesSpoken: '', assignedProperties: propertyName ? [propertyName] : [], blacklistReason: '', blacklistEffectiveDate: '', blacklistExpiryDate: '' } }
const availableProperties = computed(() => isDemoMode ? propertyNames : propertyOptions.value.map((item) => item.name))
function reset(): void { propertyFilter.value = ''; statusFilter.value = ''; tab.value = 'All drivers'; page.value = 1 }
async function openDialog(mode: 'add' | 'edit' | 'view', driver?: Driver): Promise<void> {
  dialogTrigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
  dialog.value = mode; draft.value = driver ? { ...driver, assignedProperties: [...driver.assignedProperties] } : emptyDriver(); error.value = ''
  originalDriver.value = driver ? { ...driver, assignedProperties: [...driver.assignedProperties] } : null
  if (!isDemoMode && driver) {
    try { draft.value = mapDriver(await lms.driver(driver.driverId)); originalDriver.value = { ...draft.value, assignedProperties: [...draft.value.assignedProperties] } }
    catch (cause) { originalDriver.value = null; error.value = apiErrorMessage(cause) }
  }
}
const sortFields: Partial<Record<keyof Driver, string>> = { fullName: 'name', driverId: 'driverId', licenseNumber: 'licenseNumber', phoneMasked: 'phone', propertyName: 'propertyId', vehicleName: 'assignedVehicle', availability: 'availability', status: 'status' }
watch([propertyFilter, statusFilter, tab, sortColumn, sortDirection, page, pageSize, () => route.query.q, refreshKey], async (_values, _previous, onCleanup) => {
  if (isDemoMode) return
  const controller = new AbortController()
  onCleanup(() => controller.abort())
  const propertyId = propertyOptions.value.find((item) => item.name === propertyFilter.value)?.propertyId ?? propertyFilter.value
  const field = sortFields[sortColumn.value]
  try {
    const envelope = await lms.listDrivers({ page: page.value, pageSize: pageSize.value, search: String(route.query.q ?? ''), propertyId, status: statusFilter.value ? statusFilter.value.toUpperCase() : undefined, view: tab.value === 'Available' ? 'AVAILABLE' : tab.value === 'On Trip' ? 'ON_TRIP' : tab.value === 'Driver blacklist' ? 'BLACKLIST' : undefined, sort: field ? `${sortDirection.value === -1 ? '-' : ''}${field}` : undefined }, { signal: controller.signal })
    if (controller.signal.aborted) return
    liveDrivers.value = envelope.data.map(mapDriver)
    totalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data.length
    error.value = ''
  } catch (cause) { if (!controller.signal.aborted) error.value = apiErrorMessage(cause) }
}, { immediate: true })
if (!isDemoMode) void lms.properties().then((values) => { propertyOptions.value = values.map((value) => ({ propertyId: value.propertyId, name: value.name })); refreshKey.value++ }).catch((cause) => { error.value = apiErrorMessage(cause) })
async function save(): Promise<void> {
  if (isVehiclesLoading.value) return
  if (!draft.value.fullName.trim() || !draft.value.licenseNumber.trim()) { error.value = 'Name and license are required.'; return }
  if (isDemoMode && draft.value.assignedProperties.length === 0) { error.value = 'Select at least one assigned property.'; return }
  if (!isDemoMode && !draft.value.propertyName) { error.value = 'Select a property.'; return }
  if (draft.value.blacklistExpiryDate && draft.value.blacklistEffectiveDate && draft.value.blacklistExpiryDate < draft.value.blacklistEffectiveDate) { error.value = 'Blacklist expiry must follow the effective date.'; return }
  if (!isDemoMode && (!draft.value.licenseExpiryDate || !draft.value.phoneMasked || !draft.value.emergencyContact || !draft.value.emergencyContactPhone || !draft.value.languagesSpoken)) { error.value = 'Complete all required driver fields.'; return }
  if (isDemoMode && !draft.value.driverId) draft.value.driverId = `DR-${Date.now().toString().slice(-6)}`
  if (!isDemoMode && draft.value.version && originalDriver.value && JSON.stringify(draft.value) === JSON.stringify(originalDriver.value)) { dialog.value = null; return }
  try { await store.persistDriver({ ...draft.value }, originalDriver.value ?? undefined) }
  catch (cause) { error.value = driverSaveError(cause); return }
  if (!isDemoMode) refreshKey.value++
  reset(); sortColumn.value = 'driverId'; sortDirection.value = -1; dialog.value = null
}
function driverSaveError(cause: unknown): string {
  if (cause instanceof ApiRequestError) {
    const vehicleIssue = cause.errors.find(issue => issue.field === 'assignedVehicleId')
    if (vehicleIssue) return `${t(vehicleIssue.message)} · traceId: ${cause.traceId}`
  }
  return apiErrorMessage(cause)
}
async function setStatus(driver: Driver): Promise<void> {
  const nextStatus: DriverStatus = driver.status === 'Active' ? 'Inactive' : 'Active'
  if (window.confirm(`${t(nextStatus === 'Inactive' ? 'Deactivate' : 'Activate')} ${driver.fullName}?`)) {
    try { await store.persistDriverStatus(driver, nextStatus); if (!isDemoMode) refreshKey.value++ }
    catch (cause) { error.value = cause instanceof Error ? cause.message : 'Unable to update driver.' }
  }
}
function sortBy(column: keyof Driver): void { if (sortColumn.value === column) sortDirection.value = sortDirection.value === 1 ? -1 : 1; else { sortColumn.value = column; sortDirection.value = 1 } }
const filtered = computed(() => (isDemoMode ? store.drivers : liveDrivers.value).filter((driver) => {
  if (!isDemoMode) return true
  const query = String(route.query.q ?? '').toLowerCase()
  if (query && !`${driver.fullName} ${driver.driverId} ${driver.licenseNumber} ${driver.propertyName}`.toLowerCase().includes(query)) return false
  if (propertyFilter.value && driver.propertyName !== propertyFilter.value) return false
  if (statusFilter.value && driver.status !== statusFilter.value) return false
  if (tab.value === 'Available' && driver.availability !== 'Available') return false
  if (tab.value === 'On Trip' && driver.availability !== 'On Trip') return false
  if (tab.value === 'Driver blacklist' && driver.status === 'Active') return false
  return true
}).sort((a, b) => isDemoMode ? String(a[sortColumn.value]).localeCompare(String(b[sortColumn.value])) * sortDirection.value : 0))
const visible = computed(() => isDemoMode ? filtered.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : filtered.value)
watch([propertyFilter, statusFilter, tab], () => { page.value = 1 })
</script>

<template>
  <div class="page-heading"><h1>{{ t('Driver Management') }}</h1><p>{{ t('Driver master data, license records and mobile-app availability.') }}</p></div>
  <p v-if="error && !dialog" class="form-error" role="alert">{{ error }}</p>
  <section class="filter-bar portal-filter"><label>{{ t('Property') }}<select v-model="propertyFilter"><option value="">{{ t('All properties') }}</option><option v-for="propertyName in availableProperties" :key="propertyName" :value="propertyName">{{ propertyName }}</option></select></label><label>{{ t('Status') }}<select v-model="statusFilter"><option value="">{{ t('All statuses') }}</option><option value="Active">{{ t('Active') }}</option><option value="Inactive">{{ t('Inactive') }}</option><option value="Suspended">{{ t('Suspended') }}</option></select></label><button class="text-button" @click="reset">{{ t('Reset') }}</button><span class="filter-note">{{ t('Filters apply immediately') }}</span></section>
  <section class="panel">
    <div class="panel-heading"><h2>{{ t('Driver Directory') }}</h2><button class="primary-button" @click="openDialog('add')">{{ t('ADD DRIVER') }}</button></div>
    <div class="tabs"><button v-for="name in ['All drivers', 'Available', 'On Trip', 'Driver blacklist']" :key="name" :class="{ active: tab === name }" @click="tab = name">{{ t(name) }}</button></div>
    <div class="table-scroll"><table class="data-table portal-table" :aria-label="t('Driver Directory')"><thead><tr><th scope="col" v-for="column in ([['fullName','DRIVER'],['driverId','DRIVER ID'],['licenseNumber','LICENSE'],['phoneMasked','PHONE'],['propertyName','PROPERTY'],['vehicleName','ASSIGNED VEHICLE'],['availability','AVAILABILITY'],['status','STATUS']] as const)" :key="column[0]"><button @click="sortBy(column[0])">{{ t(column[1]) }} ↕</button></th><th scope="col">{{ t('ACTIONS') }}</th></tr></thead><tbody><tr v-for="driver in visible" :key="driver.driverId"><td><span class="mini-avatar">{{ driver.fullName.split(' ').map((part) => part[0]).join('').slice(0, 2) }}</span>{{ driver.fullName }}</td><td class="mono">{{ driver.driverId }}</td><td class="mono">{{ driver.licenseNumber }}</td><td>{{ displayedPhone(driver.phoneMasked) }}</td><td>{{ driver.propertyName }}</td><td>{{ driver.vehicleName }}</td><td><span class="status-tag" :class="driver.availability.toLowerCase().replace(' ', '-')">● {{ t(driver.availability) }}</span></td><td><span class="status-tag" :class="driver.status.toLowerCase()">● {{ t(driver.status) }}</span></td><td class="table-actions"><button @click="openDialog('view', driver)">{{ t('View') }}</button><button @click="openDialog('edit', driver)">{{ t('Edit') }}</button><button @click="setStatus(driver)">{{ t(driver.status === 'Active' ? 'Deactivate' : 'Activate') }}</button></td></tr><tr v-if="visible.length === 0"><td colspan="9" class="empty-cell">{{ t('No matching records') }}<br /><small>{{ t('Try a different filter or clear the filters.') }}</small></td></tr></tbody></table></div>
    <DataPager v-model:page="page" v-model:pageSize="pageSize" :total-items="isDemoMode ? filtered.length : totalItems" />
    <div class="panel-note">{{ t('Availability is reference-only and synchronized from the Driver Mobile App. Deactivated and suspended records appear in the driver blacklist.') }}</div>
  </section>
  <div v-if="dialog && dialog !== 'view'" class="modal-backdrop" @click.self="closeDialog" @keydown="onDialogKeydown">
    <section class="dialog driver-dialog" role="dialog" aria-modal="true" :aria-label="t(dialog === 'add' ? 'Add Driver' : 'Edit Driver')">
      <header class="dialog-header">
        <h2>{{ t(dialog === 'add' ? 'Add Driver' : 'Edit Driver') }}</h2>
        <button class="icon-button" type="button" :aria-label="t('Close')" @click="closeDialog">&times;</button>
      </header>
      <div class="dialog-body form-grid">
        <label class="field-label">{{ t('Driver name') }}<input v-model="draft.fullName" /></label>
        <label class="field-label">{{ t('License number') }}<input v-model="draft.licenseNumber" /></label>
        <label v-if="!isDemoMode" class="field-label">{{ t('License expiry date') }}<input v-model="draft.licenseExpiryDate" type="date" /></label>
        <label v-if="!isDemoMode" class="field-label">{{ t('Phone') }}<input v-model="draft.phoneMasked" /></label>
        <label class="field-label">{{ t('Property') }}<select v-model="draft.propertyName"><option v-for="propertyName in availableProperties" :key="propertyName" :value="propertyName">{{ propertyName }}</option></select></label>
        <label v-if="!isDemoMode" class="field-label">{{ t('Assigned vehicle') }}<VehicleSelect v-model="draft.vehicleId" :property-id="vehiclePropertyId" :driver-id="draft.driverId" :current-label="originalDriver?.vehicleName" :load-page="lms.vehicleOptions" :translate="t" :error-message="apiErrorMessage" @busy="isVehiclesLoading = $event" /></label>
        <label v-else class="field-label">{{ t('Assigned vehicle') }}<select v-model="draft.vehicleName"><option value="">{{ t('Unassigned') }}</option><option v-for="vehicle in [...new Set(store.drivers.map(driver => driver.vehicleName))]" :key="vehicle" :value="vehicle">{{ vehicle }}</option></select></label>
        <label v-if="isDemoMode" class="field-label">{{ t('Availability') }}<select v-model="draft.availability"><option value="Available">{{ t('Available') }}</option><option value="On Trip">{{ t('On Trip') }}</option><option value="Break">{{ t('Break') }}</option><option value="Off Duty">{{ t('Off Duty') }}</option></select></label>
        <label class="field-label">{{ t('Status') }}<select v-model="draft.status"><option value="Active">{{ t('Active') }}</option><option value="Inactive">{{ t('Inactive') }}</option><option value="Suspended">{{ t('Suspended') }}</option></select></label>
        <label class="field-label">{{ t('Emergency contact') }}<input v-model="draft.emergencyContact" /></label>
        <label v-if="!isDemoMode" class="field-label">{{ t('Emergency contact phone') }}<input v-model="draft.emergencyContactPhone" /></label>
        <label class="field-label">{{ t('Languages spoken') }}<input v-model="draft.languagesSpoken" /></label>
        <label v-if="isDemoMode" class="field-label">{{ t('Assigned properties') }}<select v-model="draft.assignedProperties" multiple class="multi-select"><option v-for="propertyName in availableProperties" :key="propertyName" :value="propertyName">{{ propertyName }}</option></select></label>
        <label v-if="isDemoMode" class="field-label">{{ t('Blacklist reason') }}<input v-model="draft.blacklistReason" /></label>
        <label v-if="isDemoMode" class="field-label">{{ t('Blacklist effective date') }}<input v-model="draft.blacklistEffectiveDate" type="date" /></label>
        <label v-if="isDemoMode" class="field-label">{{ t('Blacklist expiry date') }}<input v-model="draft.blacklistExpiryDate" type="date" /></label>
        <p v-if="error" class="form-error" role="alert">{{ t(error) }}</p>
      </div>
      <footer class="dialog-footer">
        <button class="secondary-button" type="button" @click="closeDialog">{{ t('CANCEL') }}</button>
        <button class="primary-button" type="button" :disabled="isVehiclesLoading" @click="save">{{ t('SAVE DRIVER') }}</button>
      </footer>
    </section>
  </div>
  <div v-if="dialog === 'view'" class="driver-details-overlay" @click.self="closeDialog" @keydown="onDialogKeydown">
    <aside ref="dialogPanel" class="driver-details-drawer" role="dialog" aria-modal="true" :aria-label="draft.fullName">
      <header class="driver-details-header">
        <h2>{{ draft.fullName }}</h2>
        <button class="icon-button" type="button" :aria-label="t('Close')" @click="closeDialog">&times;</button>
      </header>
      <div class="driver-details-body">
        <p v-if="error" class="form-error" role="alert">{{ error }}</p>
        <dl class="driver-details-list">
          <div v-for="field in driverDetails" :key="field.label">
            <dt>{{ t(field.label) }}</dt>
            <dd>{{ field.value || '\u2014' }}</dd>
          </div>
        </dl>
      </div>
    </aside>
  </div>
</template>
