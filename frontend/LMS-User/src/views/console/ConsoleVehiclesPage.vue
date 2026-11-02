<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { apiErrorMessage, type Pagination } from '../../api/http'
import { dispatchApi, type VehicleGrade, type VehicleStatus, type VehicleSummaryView, type VehicleView } from '../../api/dispatch'
import { lms, type PropertyView } from '../../api/lms'

interface VehicleDraft { plate: string; model: string; grade: VehicleGrade; propertyId: string; seats: string; mileageKm: string; status: VehicleStatus }
const { t } = useLocale()
const search = ref('')
const propertyId = ref('')
const grade = ref<VehicleGrade | ''>('')
const status = ref<VehicleStatus | ''>('')
const page = ref(1)
const pagination = ref<Pagination | null>(null)
const vehicles = ref<VehicleView[]>([])
const properties = ref<PropertyView[]>([])
const summary = ref<VehicleSummaryView>({ available: 0, onTrip: 0, maintenance: 0, outOfService: 0 })
const selected = ref<VehicleView | null>(null)
const dialog = ref(false)
const draft = ref(emptyDraft())
const formError = ref('')
const loadError = ref('')
const isLoading = ref(false)
const isSaving = ref(false)
let searchTimer: ReturnType<typeof setTimeout> | undefined
let requestNumber = 0

const metrics = computed(() => [
  { label: 'In service', value: summary.value.available },
  { label: 'On a trip', value: summary.value.onTrip },
  { label: 'In workshop', value: summary.value.maintenance },
  { label: 'Out of service', value: summary.value.outOfService },
])
function emptyDraft(): VehicleDraft { return { plate: '', model: '', grade: 'BUSINESS_SEDAN', propertyId: '', seats: '3', mileageKm: '0', status: 'AVAILABLE' } }
function gradeLabel(value: VehicleGrade): string { return new Map<VehicleGrade, string>([['BUSINESS_SEDAN', 'Standard Sedan'], ['LUXURY_SEDAN', 'Luxury Sedan'], ['LUXURY_MPV', 'Luxury MPV']]).get(value) ?? value }
function statusLabel(value: VehicleStatus): string { return new Map<VehicleStatus, string>([['AVAILABLE', 'Active'], ['ON_TRIP', 'On a trip'], ['MAINTENANCE', 'In workshop'], ['OUT_OF_SERVICE', 'Out of service']]).get(value) ?? value }
function tone(value: VehicleStatus): string { return new Map<VehicleStatus, string>([['AVAILABLE', 'active'], ['ON_TRIP', 'on-trip'], ['MAINTENANCE', 'pending'], ['OUT_OF_SERVICE', 'off-duty']]).get(value) ?? 'off-duty' }
function propertyName(id: string): string { return properties.value.find((item) => item.propertyId === id)?.name ?? id }
async function load(): Promise<void> {
  const current = ++requestNumber
  isLoading.value = true
  loadError.value = ''
  try {
    const [list, totals] = await Promise.all([
      dispatchApi.vehicles({ page: page.value, pageSize: 20, search: search.value.trim() || undefined, propertyId: propertyId.value || undefined, grade: grade.value || undefined, status: status.value || undefined }),
      dispatchApi.vehicleSummary(propertyId.value || undefined),
    ])
    if (current !== requestNumber) return
    vehicles.value = list.data
    pagination.value = list.meta?.pagination ?? null
    summary.value = totals
  } catch (cause) { if (current === requestNumber) loadError.value = apiErrorMessage(cause) }
  finally { if (current === requestNumber) isLoading.value = false }
}
function scheduleLoad(): void { if (searchTimer) clearTimeout(searchTimer); searchTimer = setTimeout(() => { void load() }, 250) }
watch([search, propertyId, grade, status], () => { page.value = 1; scheduleLoad() })
function changePage(next: number): void { page.value = next; void load() }
function resetFilters(): void { search.value = ''; propertyId.value = ''; grade.value = ''; status.value = '' }
function openCreate(): void { draft.value = emptyDraft(); formError.value = ''; dialog.value = true }
async function openVehicle(vehicleId: string): Promise<void> {
  try { selected.value = await dispatchApi.vehicle(vehicleId) }
  catch (cause) { loadError.value = apiErrorMessage(cause) }
}
async function saveVehicle(): Promise<void> {
  const value = draft.value
  const seats = Number(value.seats)
  const mileageKm = Number(value.mileageKm)
  if (!value.plate.trim() || !value.model.trim() || !value.propertyId || !Number.isInteger(seats) || seats < 1 || !Number.isInteger(mileageKm) || mileageKm < 0) {
    formError.value = 'Plate, model, property, seats and mileage are required.'
    return
  }
  isSaving.value = true
  formError.value = ''
  try {
    await dispatchApi.createVehicle({ plate: value.plate.trim().toUpperCase(), model: value.model.trim(), grade: value.grade, propertyId: value.propertyId, seats, mileageKm, status: value.status })
    dialog.value = false
    page.value = 1
    await load()
  } catch (cause) { formError.value = apiErrorMessage(cause) }
  finally { isSaving.value = false }
}
onMounted(() => { void load(); void lms.properties().then((items) => { properties.value = items }).catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) }) })
onUnmounted(() => { if (searchTimer) clearTimeout(searchTimer) })
</script>

<template>
  <div class="ops-metrics"><article v-for="metric in metrics" :key="metric.label" class="ops-metric"><span>{{ t(metric.label) }}</span><strong>{{ metric.value }}</strong><small>{{ t('vehicles') }}</small></article></div>
  <section class="panel">
    <div class="panel-heading"><h2>{{ t('Fleet list') }} <span class="count-badge">{{ pagination?.totalItems ?? vehicles.length }}</span></h2><button class="primary-button" @click="openCreate">{{ t('New vehicle') }}</button></div>
    <div class="history-filters">
      <label class="field-label expand-field">{{ t('PLATE / MODEL') }}<input v-model="search" :placeholder="t('Search plate or model')" /></label>
      <label class="field-label">{{ t('PROPERTY') }}<select v-model="propertyId"><option value="">{{ t('All properties') }}</option><option v-for="item in properties" :key="item.propertyId" :value="item.propertyId">{{ item.name }}</option></select></label>
      <label class="field-label">{{ t('CLASS') }}<select v-model="grade"><option value="">{{ t('All classes') }}</option><option value="BUSINESS_SEDAN">{{ t('Standard Sedan') }}</option><option value="LUXURY_SEDAN">{{ t('Luxury Sedan') }}</option><option value="LUXURY_MPV">{{ t('Luxury MPV') }}</option></select></label>
      <label class="field-label">{{ t('STATUS') }}<select v-model="status"><option value="">{{ t('All') }}</option><option value="AVAILABLE">{{ t('Active') }}</option><option value="ON_TRIP">{{ t('On a trip') }}</option><option value="MAINTENANCE">{{ t('In workshop') }}</option><option value="OUT_OF_SERVICE">{{ t('Out of service') }}</option></select></label>
      <button class="secondary-button" @click="resetFilters">{{ t('RESET') }}</button>
    </div>
    <p v-if="loadError" class="form-error" role="alert">{{ loadError }}</p>
    <div class="table-scroll"><table class="data-table console-table" :aria-label="t('Vehicle Management')"><thead><tr><th scope="col">{{ t('PLATE / MODEL') }}</th><th scope="col">{{ t('CLASS') }}</th><th scope="col">{{ t('PROPERTY') }}</th><th scope="col">{{ t('SEATS') }}</th><th scope="col">{{ t('MILEAGE') }}</th><th scope="col">{{ t('DRIVER') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col"></th></tr></thead><tbody>
      <tr v-for="vehicle in vehicles" :key="vehicle.vehicleId"><td><strong>{{ vehicle.plate }}</strong><small>{{ vehicle.model }}</small></td><td>{{ t(gradeLabel(vehicle.grade)) }}</td><td>{{ propertyName(vehicle.propertyId) }}</td><td>{{ vehicle.seats }}</td><td>{{ vehicle.mileageKm }} km</td><td>{{ vehicle.driverName || t('Unassigned') }}</td><td><span class="status-tag" :class="tone(vehicle.status)">{{ t(statusLabel(vehicle.status)) }}</span></td><td class="table-actions"><button @click="openVehicle(vehicle.vehicleId)">{{ t('VIEW →') }}</button></td></tr>
      <tr v-if="!isLoading && vehicles.length === 0"><td colspan="8" class="empty-cell">{{ t('No matching vehicles') }}</td></tr><tr v-if="isLoading && vehicles.length === 0"><td colspan="8" class="empty-cell">{{ t('Loading') }}</td></tr>
    </tbody></table></div>
    <div v-if="pagination && pagination.totalPages > 1" class="list-pager"><button :disabled="page <= 1" @click="changePage(page - 1)">‹</button><span>{{ page }} / {{ pagination.totalPages }}</span><button :disabled="page >= pagination.totalPages" @click="changePage(page + 1)">›</button></div>
  </section>
  <div v-if="selected" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Vehicle details')"><header class="dialog-header"><h2>{{ selected.plate }}</h2><button class="icon-button" :aria-label="t('Close')" @click="selected = null"><X :size="18" /></button></header><div class="dialog-body"><p><strong>{{ selected.model }}</strong> · {{ t(gradeLabel(selected.grade)) }}</p><p>{{ t('Property') }}: {{ propertyName(selected.propertyId) }}</p><p>{{ t('Seats') }}: {{ selected.seats }}</p><p>{{ t('Mileage') }}: {{ selected.mileageKm }} km</p><p>{{ t('Assigned driver') }}: {{ selected.driverName || t('Unassigned') }}</p><p><span class="status-tag" :class="tone(selected.status)">{{ t(statusLabel(selected.status)) }}</span></p></div><footer class="dialog-footer"><button class="secondary-button" @click="selected = null">{{ t('CLOSE') }}</button></footer></section></div>
  <div v-if="dialog" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('New vehicle')"><header class="dialog-header"><h2>{{ t('New vehicle') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="dialog = false"><X :size="18" /></button></header><div class="dialog-body">
    <label class="field-label">{{ t('Plate') }}<input v-model="draft.plate" /></label><label class="field-label">{{ t('Model') }}<input v-model="draft.model" /></label>
    <label class="field-label">{{ t('Class') }}<select v-model="draft.grade"><option value="BUSINESS_SEDAN">{{ t('Standard Sedan') }}</option><option value="LUXURY_SEDAN">{{ t('Luxury Sedan') }}</option><option value="LUXURY_MPV">{{ t('Luxury MPV') }}</option></select></label>
    <label class="field-label">{{ t('Property') }}<select v-model="draft.propertyId"><option value="">{{ t('Select property') }}</option><option v-for="item in properties" :key="item.propertyId" :value="item.propertyId">{{ item.name }}</option></select></label>
    <label class="field-label">{{ t('Seats') }}<input v-model="draft.seats" type="number" min="1" /></label><label class="field-label">{{ t('Mileage') }} (km)<input v-model="draft.mileageKm" type="number" min="0" /></label>
    <label class="field-label">{{ t('Status') }}<select v-model="draft.status"><option value="AVAILABLE">{{ t('In service') }}</option><option value="ON_TRIP">{{ t('On a trip') }}</option><option value="MAINTENANCE">{{ t('In workshop') }}</option><option value="OUT_OF_SERVICE">{{ t('Out of service') }}</option></select></label>
    <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
  </div><footer class="dialog-footer"><button class="secondary-button" @click="dialog = false">{{ t('CLOSE') }}</button><button class="primary-button" :disabled="isSaving" @click="saveVehicle">{{ t('Save') }}</button></footer></section></div>
</template>
<style scoped>
.list-pager { display:flex; justify-content:flex-end; align-items:center; gap:12px; padding:12px 16px; }
.list-pager button { min-width:32px; min-height:32px; border:1px solid #e1e4ec; background:#fff; color:#3f4cb8; }
.list-pager button:disabled { opacity:.4; }
</style>
