<script setup lang="ts">
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import DataPager from '../../components/DataPager.vue'
import { apiErrorMessage } from '../../api/http'
import { isDemoMode } from '../../api/session'
import { locationPatch, locationRegionLabels, locationRegions, locationsApi } from '../../api/locations'
import type { LocationCategory, LocationDraft, LocationRecord } from '../../api/locations'
import { useLocale } from '../../i18n'

const { t } = useLocale()
const route = useRoute()
const demoLocations: LocationRecord[] = [
  ['LOC-001', 'Macau International Airport', 'Airport arrivals, Taipa', 22.1496, 113.5915, 'AIRPORT', 'LC-001', 'Concierge desk', '+853 8868 8888'],
  ['LOC-002', 'City of Dreams Lobby', 'Estrada do Istmo, Cotai', 22.1490, 113.5660, 'COTAI', 'LC-004', 'Concierge desk', '+853 8868 8888'],
  ['LOC-003', 'Taipa Ferry Terminal', 'Taipa Maritime Terminal', 22.1622, 113.5801, 'COTAI', 'LC-002', 'Concierge desk', '+853 8868 8888'],
  ['LOC-004', 'Altira Macau Lobby', 'Avenida de Kwong Tung, Taipa', 22.1832, 113.5587, 'MACAU_PENINSULA', 'LC-004', 'Concierge desk', '+853 8868 8888'],
  ['LOC-005', 'Hong Kong Airport', 'Terminal 1, Chek Lap Kok', 22.3080, 113.9185, 'HONG_KONG', 'LC-001', 'Concierge desk', '+853 8868 8888'],
  ['LOC-006', 'Studio City Main Entrance', 'Estrada do Istmo, Cotai', 22.1400, 113.5620, 'COTAI', 'LC-003', 'Concierge desk', '+853 8868 8888'],
].map(([locationId, name, address, latitude, longitude, region, categoryCode, contactPerson, contactPhone], index) => ({
  locationId: String(locationId), name: String(name), address: String(address),
  coordinates: { latitude: Number(latitude), longitude: Number(longitude) },
  region: String(region), categoryCode: String(categoryCode), contactPerson: String(contactPerson), contactPhone: String(contactPhone),
  operatingHours: '24 hours', geofence: { isEnabled: true, radiusMeters: 100 }, isActive: true,
  notes: 'Meet at designated limousine pickup point', sortOrder: index + 1, version: 1,
}))
const demoCategories: LocationCategory[] = [
  { categoryCode: 'LC-001', name: 'Airport', isActive: true },
  { categoryCode: 'LC-002', name: 'Ferry Terminal', isActive: true },
  { categoryCode: 'LC-003', name: 'Casino', isActive: true },
  { categoryCode: 'LC-004', name: 'Hotel', isActive: true },
  { categoryCode: 'LC-005', name: 'Shopping Mall', isActive: true },
  { categoryCode: 'LC-006', name: 'Convention Center', isActive: true },
]
const rows = ref<LocationRecord[]>(isDemoMode ? structuredClone(demoLocations) : [])
const categories = ref<LocationCategory[]>(isDemoMode ? demoCategories : [])
const totalItems = ref(rows.value.length)
const page = ref(1)
const pageSize = ref(10)
const search = ref('')
const regionFilter = ref('')
const statusFilter = ref('')
const sortField = ref('sortOrder')
const sortDirection = ref<1 | -1>(1)
const refreshKey = ref(0)
const error = ref('')
const dialog = ref<'view' | 'add' | 'edit' | null>(null)
const dialogPanel = ref<HTMLElement | null>(null)
const original = ref<LocationRecord | null>(null)
const draft = ref<LocationDraft>(emptyDraft())
const isBusy = ref(false)
const draggedId = ref<string | null>(null)
let dialogTrigger: HTMLElement | null = null
let previousBodyOverflow = ''
let detailRequest = 0

function emptyDraft(): LocationDraft {
  return { name: '', address: '', coordinates: { latitude: NaN, longitude: NaN }, region: 'MACAU_PENINSULA', categoryCode: '', contactPerson: '', contactPhone: '', operatingHours: '', geofence: { isEnabled: true, radiusMeters: NaN }, isActive: true, notes: '' }
}
function toDraft(value: LocationRecord): LocationDraft {
  return { name: value.name, address: value.address, coordinates: { ...value.coordinates }, region: value.region, categoryCode: value.categoryCode, contactPerson: value.contactPerson, contactPhone: value.contactPhone, operatingHours: value.operatingHours, geofence: { ...value.geofence }, isActive: value.isActive, notes: value.notes ?? '' }
}
const categoryName = (code: string): string => categories.value.find((item) => item.categoryCode === code)?.name ?? code
const regionName = (code: string): string => t(locationRegionLabels[code] ?? code)
const coordinatesText = (value: LocationRecord): string => `${value.coordinates.latitude.toFixed(4)}, ${value.coordinates.longitude.toFixed(4)}`
const details = computed(() => original.value ? [
  { label: 'id', value: original.value.locationId },
  { label: 'Address', value: draft.value.address },
  { label: 'Latitude', value: String(draft.value.coordinates.latitude) },
  { label: 'Longitude', value: String(draft.value.coordinates.longitude) },
  { label: 'Region', value: regionName(draft.value.region) },
  { label: 'Category', value: t(categoryName(draft.value.categoryCode)) },
  { label: 'Contact person', value: draft.value.contactPerson },
  { label: 'Contact phone', value: draft.value.contactPhone },
  { label: 'Operating hours', value: draft.value.operatingHours },
  { label: 'Geofence', value: t(draft.value.geofence.isEnabled ? 'Enabled' : 'Disabled') },
  { label: 'Boundary radius (meters)', value: String(draft.value.geofence.radiusMeters) },
  { label: 'Record status', value: t(draft.value.isActive ? 'Enabled' : 'Disabled') },
  { label: 'Notes', value: draft.value.notes ?? '' },
] : [])

function reset(): void { search.value = ''; regionFilter.value = ''; statusFilter.value = ''; page.value = 1 }
function sortBy(field: string): void {
  if (sortField.value === field) sortDirection.value = sortDirection.value === 1 ? -1 : 1
  else { sortField.value = field; sortDirection.value = 1 }
  page.value = 1
}
const sort = computed(() => sortField.value === 'sortOrder' ? 'region,sortOrder,locationId' : `${sortDirection.value === -1 ? '-' : ''}${sortField.value}`)
const filtered = computed(() => {
  if (!isDemoMode) return rows.value
  const query = [search.value, String(route.query.q ?? '')].filter(Boolean).join(' ').toLowerCase()
  return rows.value.filter((value) => (!query || `${value.name} ${value.address} ${value.region} ${categoryName(value.categoryCode)}`.toLowerCase().includes(query)) && (!regionFilter.value || value.region === regionFilter.value) && (!statusFilter.value || value.isActive === (statusFilter.value === 'active')))
    .sort((a, b) => {
      if (sortField.value === 'sortOrder') return 0
      const field = sortField.value === 'coordinates.latitude' ? 'coordinates.latitude' : sortField.value
      const left = field === 'coordinates.latitude' ? a.coordinates.latitude : String(a[field as keyof LocationRecord] ?? '')
      const right = field === 'coordinates.latitude' ? b.coordinates.latitude : String(b[field as keyof LocationRecord] ?? '')
      return (typeof left === 'number' && typeof right === 'number' ? left - right : String(left).localeCompare(String(right))) * sortDirection.value
    })
})
const visible = computed(() => isDemoMode ? filtered.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : filtered.value)
watch([search, regionFilter, statusFilter, () => route.query.q], () => { page.value = 1 })
watch([page, pageSize, search, regionFilter, statusFilter, sort, () => route.query.q, refreshKey], async (_values, _previous, onCleanup) => {
  if (isDemoMode) return
  const controller = new AbortController()
  onCleanup(() => controller.abort())
  try {
    const envelope = await locationsApi.list({ page: page.value, pageSize: pageSize.value, search: [search.value, String(route.query.q ?? '')].filter(Boolean).join(' '), region: regionFilter.value || undefined, isActive: statusFilter.value ? statusFilter.value === 'active' : undefined, sort: sort.value }, { signal: controller.signal })
    if (controller.signal.aborted) return
    rows.value = envelope.data
    totalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data.length
    error.value = ''
  } catch (cause) { if (!controller.signal.aborted) error.value = apiErrorMessage(cause) }
}, { immediate: true })
if (!isDemoMode) void locationsApi.categories().then((value) => { categories.value = value }).catch((cause) => { error.value = apiErrorMessage(cause) })

function closeDialog(): void { dialog.value = null; original.value = null; void nextTick(() => dialogTrigger?.focus()) }
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
    void nextTick(() => dialogPanel.value?.querySelector<HTMLElement>('button')?.focus())
  } else document.body.style.overflow = previousBodyOverflow
})
onUnmounted(() => { document.body.style.overflow = previousBodyOverflow })

async function openDialog(mode: 'view' | 'add' | 'edit', value?: LocationRecord): Promise<void> {
  dialogTrigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
  dialog.value = mode
  error.value = ''
  original.value = value ?? null
  draft.value = value ? toDraft(value) : emptyDraft()
  if (mode === 'add' && categories.value[0]) draft.value.categoryCode = categories.value[0].categoryCode
  if (isDemoMode || !value) return
  const request = ++detailRequest
  try {
    const detail = await locationsApi.detail(value.locationId)
    if (request !== detailRequest || dialog.value !== mode) return
    original.value = detail
    draft.value = toDraft(detail)
  } catch (cause) { if (request === detailRequest) error.value = apiErrorMessage(cause) }
}

function validateDraft(value: LocationDraft): string {
  if (![value.name, value.address, value.region, value.categoryCode, value.contactPerson, value.contactPhone, value.operatingHours].every((field) => field.trim())) return 'Complete all required location fields.'
  if (!Number.isFinite(value.coordinates.latitude) || Math.abs(value.coordinates.latitude) > 90 || !Number.isFinite(value.coordinates.longitude) || Math.abs(value.coordinates.longitude) > 180) return 'Enter valid latitude and longitude.'
  if (!Number.isFinite(value.geofence.radiusMeters) || value.geofence.radiusMeters < 10 || value.geofence.radiusMeters > 5000) return 'Boundary radius must be between 10 and 5000 meters.'
  return ''
}
async function save(): Promise<void> {
  const validation = validateDraft(draft.value)
  if (validation) { error.value = validation; return }
  isBusy.value = true
  try {
    if (isDemoMode) {
      if (dialog.value === 'add') rows.value.push({ ...structuredClone(draft.value), locationId: `LOC-${String(rows.value.length + 1).padStart(3, '0')}`, sortOrder: rows.value.filter((row) => row.region === draft.value.region).length + 1, version: 1 })
      else if (original.value) rows.value = rows.value.map((row) => row.locationId === original.value?.locationId ? { ...row, ...structuredClone(draft.value), version: row.version + 1 } : row)
    } else if (dialog.value === 'add') await locationsApi.create(draft.value)
    else if (original.value) {
      const changes = locationPatch(original.value, draft.value)
      if (Object.keys(changes).length) await locationsApi.update(original.value, changes)
    }
    if (!isDemoMode) refreshKey.value++
    closeDialog()
  } catch (cause) { error.value = apiErrorMessage(cause) }
  finally { isBusy.value = false }
}

async function toggleGeofence(value: LocationRecord): Promise<void> {
  isBusy.value = true
  try {
    if (isDemoMode) rows.value = rows.value.map((row) => row.locationId === value.locationId ? { ...row, geofence: { ...row.geofence, isEnabled: !row.geofence.isEnabled } } : row)
    else { await locationsApi.update(value, { geofence: { isEnabled: !value.geofence.isEnabled } }); refreshKey.value++ }
  } catch (cause) { error.value = apiErrorMessage(cause) }
  finally { isBusy.value = false }
}
async function deactivate(value: LocationRecord): Promise<void> {
  if (!window.confirm(`${t('Disable')} ${value.name}?`)) return
  isBusy.value = true
  try {
    if (isDemoMode) rows.value = rows.value.map((row) => row.locationId === value.locationId ? { ...row, isActive: false } : row)
    else { await locationsApi.update(value, { isActive: false }); refreshKey.value++ }
  } catch (cause) { error.value = apiErrorMessage(cause) }
  finally { isBusy.value = false }
}
async function allRegionRows(region: string): Promise<LocationRecord[]> {
  if (isDemoMode) return rows.value.filter((row) => row.region === region).sort((a, b) => a.sortOrder - b.sortOrder)
  const result: LocationRecord[] = []
  for (let currentPage = 1; ; currentPage++) {
    const envelope = await locationsApi.list({ page: currentPage, pageSize: 50, region, sort: 'sortOrder,locationId' })
    result.push(...envelope.data)
    const pagination = envelope.meta?.pagination
    if (!pagination || currentPage >= (pagination.totalPages ?? Math.ceil((pagination.totalItems ?? result.length) / 50))) break
  }
  return result
}
async function commitOrder(region: string, ordered: LocationRecord[]): Promise<void> {
  if (isDemoMode) {
    let position = 0
    rows.value = rows.value.map((row) => row.region === region ? { ...ordered[position]!, sortOrder: ++position } : row)
  } else { await locationsApi.reorder(region, ordered); refreshKey.value++ }
}
async function reorderLocation(sourceId: string, targetId: string): Promise<void> {
  const source = rows.value.find((row) => row.locationId === sourceId)
  const target = rows.value.find((row) => row.locationId === targetId)
  if (!source || (target && source.region !== target.region) || sourceId === targetId) return
  isBusy.value = true
  try {
    const ordered = await allRegionRows(source.region)
    const from = ordered.findIndex((row) => row.locationId === sourceId)
    const to = ordered.findIndex((row) => row.locationId === targetId)
    if (from < 0 || to < 0) return
    const [moved] = ordered.splice(from, 1)
    if (!moved) return
    ordered.splice(to, 0, moved)
    await commitOrder(source.region, ordered)
  } catch (cause) { error.value = apiErrorMessage(cause) }
  finally { isBusy.value = false; draggedId.value = null }
}
async function moveUp(value: LocationRecord): Promise<void> {
  isBusy.value = true
  try {
    const ordered = await allRegionRows(value.region)
    const index = ordered.findIndex((row) => row.locationId === value.locationId)
    if (index < 1) return
    const previous = ordered[index - 1]
    if (previous) await reorderLocation(value.locationId, previous.locationId)
  } catch (cause) { error.value = apiErrorMessage(cause) }
  finally { isBusy.value = false }
}
</script>

<template>
  <div class="page-heading"><h1>{{ t('Common Used Locations') }}</h1><p>{{ t('Maintain pickup and drop-off locations, regions and geofences.') }}</p></div>
  <p v-if="error && !dialog" class="form-error" role="alert">{{ t(error) }}</p>
  <section class="filter-bar portal-filter location-filters">
    <label class="location-mobile-search">{{ t('Search') }}<input v-model="search" :placeholder="`${t('Search')} ${t('Common Used Locations')}`" /></label>
    <label>{{ t('Status') }}<select v-model="statusFilter"><option value="">{{ t('All statuses') }}</option><option value="active">{{ t('Enabled') }}</option><option value="inactive">{{ t('Disabled') }}</option></select></label>
    <label>{{ t('Region') }}<select v-model="regionFilter"><option value="">{{ t('All regions') }}</option><option v-for="region in locationRegions" :key="region" :value="region">{{ regionName(region) }}</option></select></label>
    <button class="text-button" type="button" @click="reset">{{ t('Reset') }}</button><span class="filter-note">{{ t('Filters apply immediately') }}</span>
  </section>
  <section class="panel location-panel">
    <div class="panel-heading"><h2>{{ t('Location Master Data') }}</h2><button class="primary-button" type="button" @click="openDialog('add')">{{ t('ADD LOCATION') }}</button></div>
    <div class="location-table-note">{{ t('Drag rows to reorder within the current region. Use Move up for keyboard-friendly reordering.') }}</div>
    <div class="table-scroll"><table class="data-table portal-table location-table" :aria-label="t('Location Master Data')">
      <thead><tr>
        <th v-for="column in ([['name','LOCATION'],['region','REGION'],['categoryCode','CATEGORY'],['address','ADDRESS'],['coordinates.latitude','GPS'],['contactPerson','CONTACT'],['contactPhone','PHONE'],['operatingHours','HOURS'],['geofence.isEnabled','GEOFENCE'],['isActive','STATUS']] as const)" :key="column[0]" scope="col"><button type="button" @click="sortBy(column[0])">{{ t(column[1]) }} ↕</button></th>
        <th scope="col">{{ t('ACTIONS') }}</th>
      </tr></thead>
      <tbody>
        <tr v-for="row in visible" :key="row.locationId" draggable="true" @dragstart="draggedId = row.locationId" @dragover.prevent @drop.prevent="draggedId && reorderLocation(draggedId, row.locationId)" @dragend="draggedId = null">
          <td>{{ row.name }}</td><td>{{ regionName(row.region) }}</td><td>{{ t(categoryName(row.categoryCode)) }}</td><td>{{ row.address }}</td><td class="mono">{{ coordinatesText(row) }}</td><td>{{ row.contactPerson }}</td><td>{{ row.contactPhone }}</td><td>{{ row.operatingHours }}</td>
          <td><label class="location-toggle"><input type="checkbox" :checked="row.geofence.isEnabled" :disabled="isBusy" :aria-label="`${t('Geofence')} ${row.name}`" @change="toggleGeofence(row)" /><span aria-hidden="true"></span></label></td>
          <td><span class="status-tag" :class="row.isActive ? 'active' : 'inactive'">● {{ t(row.isActive ? 'Enabled' : 'Disabled') }}</span></td>
          <td class="table-actions"><button type="button" @click="openDialog('view', row)">{{ t('View') }}</button><button type="button" @click="openDialog('edit', row)">{{ t('Edit') }}</button><button v-if="row.isActive" type="button" :disabled="isBusy" @click="deactivate(row)">{{ t('Disable') }}</button><button type="button" :disabled="isBusy" @click="moveUp(row)">{{ t('Move up') }}</button></td>
        </tr>
        <tr v-if="visible.length === 0"><td colspan="11" class="empty-cell">{{ t('No matching records') }}</td></tr>
      </tbody>
    </table></div>
    <DataPager v-model:page="page" v-model:pageSize="pageSize" :total-items="isDemoMode ? filtered.length : totalItems" />
  </section>
  <div v-if="dialog === 'view'" class="driver-details-overlay location-details-overlay" @click.self="closeDialog" @keydown="onDialogKeydown">
    <aside ref="dialogPanel" class="driver-details-drawer" role="dialog" aria-modal="true" :aria-label="draft.name">
      <header class="driver-details-header"><h2>{{ draft.name }}</h2><button class="icon-button" type="button" :aria-label="t('Close')" @click="closeDialog">&times;</button></header>
      <div class="driver-details-body"><p v-if="error" class="form-error" role="alert">{{ t(error) }}</p><dl class="driver-details-list"><div v-for="field in details" :key="field.label"><dt>{{ t(field.label) }}</dt><dd>{{ field.value || '\u2014' }}</dd></div></dl></div>
    </aside>
  </div>
  <div v-if="dialog === 'add' || dialog === 'edit'" class="modal-backdrop" @click.self="closeDialog" @keydown="onDialogKeydown">
    <section ref="dialogPanel" class="dialog location-dialog" role="dialog" aria-modal="true" :aria-label="t(dialog === 'add' ? 'Add location' : 'Edit location')">
      <header class="dialog-header"><h2>{{ t(dialog === 'add' ? 'Add location' : 'Edit location') }}</h2><button class="icon-button" type="button" :aria-label="t('Close')" @click="closeDialog">&times;</button></header>
      <div class="dialog-body location-form-grid">
        <label class="field-label">{{ t('Location name') }} *<input v-model="draft.name" required /></label>
        <label class="field-label">{{ t('Address') }} *<input v-model="draft.address" required /></label>
        <label class="field-label">{{ t('Latitude') }} *<input v-model.number="draft.coordinates.latitude" type="number" min="-90" max="90" step="any" required /></label>
        <label class="field-label">{{ t('Longitude') }} *<input v-model.number="draft.coordinates.longitude" type="number" min="-180" max="180" step="any" required /></label>
        <label class="field-label">{{ t('Region') }} *<select v-model="draft.region"><option v-for="region in locationRegions" :key="region" :value="region">{{ regionName(region) }}</option></select></label>
        <label class="field-label">{{ t('Category') }} *<select v-model="draft.categoryCode"><option v-for="category in categories" :key="category.categoryCode" :value="category.categoryCode">{{ t(category.name) }}</option></select></label>
        <label class="field-label">{{ t('Contact person') }} *<input v-model="draft.contactPerson" required /></label>
        <label class="field-label">{{ t('Contact phone') }} *<input v-model="draft.contactPhone" type="tel" required /></label>
        <label class="field-label">{{ t('Operating hours') }} *<input v-model="draft.operatingHours" required /></label>
        <label class="field-label">{{ t('Geofence') }} *<select v-model="draft.geofence.isEnabled"><option :value="true">{{ t('Enabled') }}</option><option :value="false">{{ t('Disabled') }}</option></select></label>
        <label class="field-label">{{ t('Boundary radius (meters)') }} *<input v-model.number="draft.geofence.radiusMeters" type="number" min="10" max="5000" required /></label>
        <label class="field-label">{{ t('Record status') }} *<select v-model="draft.isActive"><option :value="true">{{ t('Enabled') }}</option><option :value="false">{{ t('Disabled') }}</option></select></label>
        <label class="field-label location-notes">{{ t('Notes') }}<textarea v-model="draft.notes" rows="3"></textarea></label>
        <p v-if="error" class="form-error location-form-error" role="alert">{{ t(error) }}</p>
      </div>
      <footer class="dialog-footer"><button class="secondary-button" type="button" @click="closeDialog">{{ t('CANCEL') }}</button><button class="primary-button" type="button" :disabled="isBusy" @click="save">{{ t('SAVE CHANGES') }}</button></footer>
    </section>
  </div>
</template>
