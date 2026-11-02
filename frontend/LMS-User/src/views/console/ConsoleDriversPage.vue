<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { currentUser, isDemoMode } from '../../api/session'
import { apiErrorMessage } from '../../api/http'
import { lms, mapDriver, type DriverView } from '../../api/lms'
import ConsoleDriverEditor from './ConsoleDriverEditor.vue'
import DataPager from '../../components/DataPager.vue'
import { useDemoStore } from '../../stores/demo'
import { useFleetStore } from '../../stores/fleet'

interface DriverRow {
  id: string
  name: string
  property: string
  phone: string
  license: string
  licenseExpiry: string
  vehicle: string
  languages: string
  status: string
  availability: string
}

const { t } = useLocale()
const store = useDemoStore()
const fleet = useFleetStore()
const search = ref('')
const propertyId = ref('')
const status = ref('All')
const page = ref(1)
const pageSize = ref(20)
const totalItems = ref(0)
const isLoading = ref(false)
const loadError = ref('')
const rows = ref<DriverRow[]>([])
const properties = ref<{ propertyId: string; name: string }[]>([])
const selectedId = ref('')
const editingDriver = ref<DriverView | null>(null)
const deletingDriver = ref<DriverRow | null>(null)
const deleteError = ref('')
const successMessage = ref('')
const isReading = ref(false)
const isDeleting = ref(false)
const canWrite = computed(() => !isDemoMode && (currentUser.value?.permissions.includes('drivers:write') ?? false))
let requestId = 0
let timer = 0

const demoRows = computed<DriverRow[]>(() => fleet.drivers.filter((driver) => {
  const query = search.value.trim().toLowerCase()
  const matchesQuery = !query || `${driver.staffId} ${driver.name}`.toLowerCase().includes(query)
  const matchesProperty = !propertyId.value || driver.property === propertyId.value
  const matchesStatus = status.value === 'All' || driver.status === status.value
  return matchesQuery && matchesProperty && matchesStatus
}).map((driver) => ({
  id: driver.staffId,
  name: driver.name,
  property: driver.property,
  phone: driver.phoneLastFour ? `•••• ${driver.phoneLastFour}` : '—',
  license: driver.license,
  licenseExpiry: driver.licenseExpiry,
  vehicle: '—',
  languages: driver.languages,
  status: driver.status,
  availability: driver.status === 'On duty' ? 'Available' : driver.status === 'On leave' ? 'Off Duty' : 'Off Duty',
})))

const visibleRows = computed(() => isDemoMode ? demoRows.value : rows.value)
const selected = computed(() => visibleRows.value.find((driver) => driver.id === selectedId.value) ?? null)

function tone(statusName: string): string {
  if (statusName === 'Active' || statusName === 'Available' || statusName === 'On duty') return 'active'
  if (statusName === 'On Trip' || statusName === 'Break') return 'on-trip'
  if (statusName === 'Suspended' || statusName === 'On leave') return 'pending'
  return 'off-duty'
}

async function loadDrivers(): Promise<void> {
  if (isDemoMode) return
  const current = ++requestId
  isLoading.value = true
  loadError.value = ''
  try {
    if (properties.value.length === 0) properties.value = await store.loadProperties()
    const envelope = await lms.listDrivers({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value.trim() || undefined,
      propertyId: propertyId.value || undefined,
      status: status.value === 'All' ? undefined : status.value,
    })
    if (current !== requestId) return
    rows.value = (envelope.data ?? []).map((item) => {
      const driver = mapDriver({ ...item, languages: item.languages ?? [] })
      return {
        id: driver.driverId,
        name: driver.fullName,
        property: driver.propertyName,
        phone: driver.phoneMasked || '—',
        license: driver.licenseNumber,
        licenseExpiry: driver.licenseExpiryDate || '—',
        vehicle: driver.vehicleName || '—',
        languages: driver.languagesSpoken || '—',
        status: driver.status,
        availability: driver.availability,
      }
    })
    totalItems.value = envelope.meta?.pagination?.totalItems ?? rows.value.length
  } catch (cause) {
    if (current !== requestId) return
    rows.value = []
    totalItems.value = 0
    loadError.value = apiErrorMessage(cause)
  } finally {
    if (current === requestId) isLoading.value = false
  }
}

function schedule(): void {
  window.clearTimeout(timer)
  timer = window.setTimeout(() => { void loadDrivers() }, 300)
}

async function openEdit(driverId: string): Promise<void> {
  if (!canWrite.value || isReading.value || isDeleting.value) return
  isReading.value = true
  loadError.value = ''; successMessage.value = ''
  try { editingDriver.value = await lms.driver(driverId); selectedId.value = '' }
  catch (cause) { loadError.value = apiErrorMessage(cause) }
  finally { isReading.value = false }
}
function openDelete(driver: DriverRow): void {
  if (!canWrite.value || isReading.value || isDeleting.value) return
  deletingDriver.value = driver; deleteError.value = ''; successMessage.value = ''; selectedId.value = ''
}
function closeDelete(): void { if (!isDeleting.value) deletingDriver.value = null }
async function saved(): Promise<void> {
  editingDriver.value = null; successMessage.value = 'Driver updated.'
  await loadDrivers()
}
async function remove(): Promise<void> {
  if (!deletingDriver.value || isDeleting.value || !canWrite.value) return
  isDeleting.value = true; deleteError.value = ''
  try {
    await lms.removeDriver(deletingDriver.value.id)
    deletingDriver.value = null; successMessage.value = 'Driver deleted.'
    if (rows.value.length === 1 && page.value > 1) page.value -= 1
    else await loadDrivers()
  } catch (cause) { deleteError.value = apiErrorMessage(cause) }
  finally { isDeleting.value = false }
}
onBeforeUnmount(() => { window.clearTimeout(timer); requestId += 1 })

watch([search, propertyId, status], () => {
  if (isDemoMode) return
  if (page.value !== 1) page.value = 1
  else schedule()
})
watch([page, pageSize], () => { if (!isDemoMode) schedule() }, { immediate: true })
</script>

<template>
  <p v-if="successMessage" role="status">{{ t(successMessage) }}</p>
  <p v-if="loadError" class="form-error" role="alert">{{ loadError }} <button class="text-button" @click="loadDrivers()">{{ t('Retry') }}</button></p>
  <section class="panel">
    <div class="panel-heading">
      <h2>{{ t('Driver list') }} <span class="count-badge">{{ isDemoMode ? visibleRows.length : totalItems }}</span></h2>
    </div>
    <div class="history-filters">
      <label class="field-label expand-field">{{ t('NAME / STAFF ID') }}<input v-model="search" :placeholder="t('Search driver name or staff ID')" /></label>
      <label class="field-label">{{ t('PROPERTY') }}
        <select v-model="propertyId">
          <option value="">{{ t('All properties') }}</option>
          <option v-if="isDemoMode" value="City of Dreams">City of Dreams</option>
          <option v-if="isDemoMode" value="Studio City">Studio City</option>
          <option v-if="isDemoMode" value="Morpheus">Morpheus</option>
          <option v-if="isDemoMode" value="Altira">Altira</option>
          <option v-if="isDemoMode" value="Nuwa">Nuwa</option>
          <option v-for="property in properties" :key="property.propertyId" :value="property.propertyId">{{ property.name }}</option>
        </select>
      </label>
      <label class="field-label">{{ t('STATUS') }}
        <select v-model="status">
          <option value="All">{{ t('All') }}</option>
          <template v-if="isDemoMode">
            <option value="On duty">{{ t('On duty') }}</option>
            <option value="Available">{{ t('Available') }}</option>
            <option value="On leave">{{ t('On leave') }}</option>
            <option value="Suspended">{{ t('Suspended') }}</option>
          </template>
          <template v-else>
            <option value="ACTIVE">{{ t('Active') }}</option>
            <option value="INACTIVE">{{ t('Inactive') }}</option>
            <option value="SUSPENDED">{{ t('Suspended') }}</option>
          </template>
        </select>
      </label>
      <button class="secondary-button" @click="search = ''; propertyId = ''; status = 'All'">{{ t('RESET') }}</button>
    </div>
    <div class="table-scroll">
      <table class="data-table console-table" :aria-label="t('Driver Management')">
        <thead>
          <tr>
            <th scope="col">{{ t('NAME / STAFF ID') }}</th>
            <th scope="col">{{ t('PROPERTY') }}</th>
            <th scope="col">{{ t('PHONE') }}</th>
            <th scope="col">{{ t('LICENSE') }}</th>
            <th scope="col">{{ t('VEHICLE') }}</th>
            <th scope="col">{{ t('Availability') }}</th>
            <th scope="col">{{ t('STATUS') }}</th>
            <th scope="col"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="isLoading && visibleRows.length === 0"><td colspan="8" class="empty-cell">{{ t('Loading drivers') }}</td></tr>
          <tr v-for="driver in visibleRows" :key="driver.id">
            <td><strong>{{ driver.name }}</strong><small>{{ driver.id }}</small></td>
            <td>{{ driver.property }}</td>
            <td>{{ driver.phone }}</td>
            <td>{{ driver.license }}<small>{{ driver.licenseExpiry }}</small></td>
            <td>{{ driver.vehicle }}</td>
            <td><span class="status-tag" :class="tone(driver.availability)">{{ t(driver.availability) }}</span></td>
            <td><span class="status-tag" :class="tone(driver.status)">{{ t(driver.status) }}</span></td>
            <td class="table-actions"><button @click="selectedId = driver.id">{{ t('VIEW →') }}</button><button v-if="canWrite" :disabled="isReading || isDeleting" @click="openEdit(driver.id)">{{ t('Edit') }}</button><button v-if="canWrite" :disabled="isReading || isDeleting" @click="openDelete(driver)">{{ t('Delete') }}</button></td>
          </tr>
          <tr v-if="!isLoading && visibleRows.length === 0"><td colspan="8" class="empty-cell">{{ t('No matching drivers') }}</td></tr>
        </tbody>
      </table>
    </div>
    <div class="panel-note">{{ isDemoMode ? t('Layout sample · not connected to LMS') : t('Live driver list from LMS') }}</div>
    <DataPager v-if="!isDemoMode" v-model:page="page" v-model:page-size="pageSize" :total-items="totalItems" />
  </section>

  <div v-if="selected" class="modal-backdrop">
    <section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Driver details')">
      <header class="dialog-header"><h2>{{ selected.name }}</h2><button class="icon-button" :aria-label="t('Close')" @click="selectedId = ''"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ selected.id }}</strong> · {{ selected.property }}</p>
        <p>{{ t('Phone') }}: {{ selected.phone }}</p>
        <p>{{ t('License number') }}: {{ selected.license }}</p>
        <p>{{ t('License expiry date') }}: {{ selected.licenseExpiry }}</p>
        <p>{{ t('Assigned vehicle') }}: {{ selected.vehicle }}</p>
        <p>{{ t('Languages') }}: {{ selected.languages }}</p>
        <p><span class="status-tag" :class="tone(selected.availability)">{{ t(selected.availability) }}</span></p>
        <p><span class="status-tag" :class="tone(selected.status)">{{ t(selected.status) }}</span></p>
      </div>
      <footer class="dialog-footer"><button v-if="canWrite" class="text-button" :disabled="isReading" @click="openEdit(selected.id)">{{ t('Edit') }}</button><button v-if="canWrite" class="text-button" :disabled="isReading" @click="openDelete(selected)">{{ t('Delete') }}</button><button class="secondary-button" @click="selectedId = ''">{{ t('CLOSE') }}</button></footer>
    </section>
  </div>
  <ConsoleDriverEditor v-if="editingDriver" :driver="editingDriver" :properties="properties" @close="editingDriver = null" @saved="saved" />
  <div v-if="deletingDriver" class="modal-backdrop">
    <section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Delete driver')">
      <header class="dialog-header"><h2>{{ t('Delete driver') }}</h2><button class="icon-button" :disabled="isDeleting" :aria-label="t('Close')" @click="closeDelete"><X :size="18" /></button></header>
      <div class="dialog-body"><p><strong>{{ deletingDriver.name }}</strong></p><p>{{ t('Delete this driver? Historical shifts and audit records will be retained. The driver will no longer be able to sign in.') }}</p><p v-if="deleteError" class="form-error" role="alert">{{ deleteError }}</p></div>
      <footer class="dialog-footer"><button class="secondary-button" :disabled="isDeleting" @click="closeDelete">{{ t('Cancel') }}</button><button class="primary-button" :disabled="isDeleting" @click="remove">{{ t(isDeleting ? 'Deleting' : 'Delete') }}</button></footer>
    </section>
  </div>
</template>
