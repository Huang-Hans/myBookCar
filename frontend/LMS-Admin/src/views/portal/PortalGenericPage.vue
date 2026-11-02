<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import DataPager from '../../components/DataPager.vue'
import { useDemoStore } from '../../stores/demo'
import { api, isDemoMode } from '../../api/session'
import { apiErrorMessage } from '../../api/http'
import { portalField, portalSourcePath, portalSources, portalTabLabel } from '../../api/portalSources'
const { t } = useLocale()

interface RegistryRow { rowId: string; cells: string[] }
interface Registry { title: string; description: string; panelTitle: string; columns: string[]; rows: RegistryRow[]; isReadOnly: boolean; addLabel?: string; notes?: string[] }
const props = defineProps<{ section: string }>()
const route = useRoute()
const store = useDemoStore()
function registry(title: string, description: string, panelTitle: string, columns: string[], values: string[][], isReadOnly = true, addLabel?: string, notes?: string[]): Registry {
  return { title, description, panelTitle, columns, rows: values.map((cells, index) => ({ rowId: `${title.replaceAll(' ', '-')}-${index}`, cells })), isReadOnly, addLabel, notes }
}
const registries: Record<string, Registry> = {
  locations: registry('Common Used Locations', 'Maintain pickup and drop-off locations, regions and geofences.', 'Location Master Data', ['LOCATION', 'REGION', 'CATEGORY', 'ADDRESS', 'GPS', 'HOURS', 'STATUS'], [
    ['Macau International Airport', 'Airport', 'Airport', 'Airport arrivals, Taipa', '22.1496, 113.5915', '24 hours', 'Enabled'],
    ['City of Dreams Lobby', 'Cotai', 'Hotel', 'Estrada do Istmo, Cotai', '22.1490, 113.5660', '24 hours', 'Enabled'],
    ['Taipa Ferry Terminal', 'Cotai', 'Ferry Terminal', 'Taipa Maritime Terminal', '22.1622, 113.5801', '24 hours', 'Enabled'],
    ['Altira Macau Lobby', 'Macau Peninsula', 'Hotel', 'Avenida de Kwong Tung, Taipa', '22.1832, 113.5587', '24 hours', 'Enabled'],
    ['Hong Kong Airport', 'Hong Kong', 'Airport', 'Terminal 1', '22.3080, 113.9185', '24 hours', 'Enabled'],
    ['Studio City Main Entrance', 'Cotai', 'Casino', 'Estrada do Istmo, Cotai', '22.1400, 113.5620', '24 hours', 'Enabled'],
  ], false, 'ADD LOCATION'),
  properties: registry('Properties', 'Property service information and uploaded operational rules.', 'Properties Registry', ['PROPERTY CODE', 'PROPERTY', 'ADDRESS', 'CONTACT', 'ASSIGNED FLEET', 'HOURS', 'SERVICE LEVEL'], [
    ['COD', 'City of Dreams', 'Estrada do Istmo, Cotai', 'Property transport desk', '18 assigned vehicles', '24 hours', 'Pickup within 5 minutes'],
    ['SC', 'Studio City', 'Estrada do Istmo, Cotai', 'Property transport desk', '16 assigned vehicles', '24 hours', 'Pickup within 5 minutes'],
    ['ALT', 'Altira', 'Avenida de Kwong Tung, Taipa', 'Property transport desk', '8 assigned vehicles', '24 hours', 'Pickup within 5 minutes'],
    ['GH', 'Grand Hyatt', 'City of Dreams, Cotai', 'Property transport desk', '10 assigned vehicles', '24 hours', 'Pickup within 5 minutes'],
  ]),
  reasons: registry('Cancellation Reasons', 'Standard cancellation reasons and points-return policy references.', 'Cancellation Reasons Registry', ['REASON CODE', 'CANCELLATION REASON', 'POINTS-RETURN POLICY', 'POLICY REFERENCE', 'SOURCE'], [
    ['CR-001', 'Customer no-show', 'PR-002', 'Points forfeiture assessed by loyalty system', 'Reference only'],
    ['CR-002', 'Flight delayed', 'PR-001', 'Points returned under eligibility rules', 'Reference only'],
    ['CR-003', 'Changed plans', 'PR-003', 'Return eligibility depends on notice period', 'Reference only'],
    ['CR-004', 'Driver unavailable', 'PR-001', 'Points returned', 'Reference only'],
    ['CR-005', 'Vehicle breakdown', 'PR-001', 'Points returned', 'Reference only'],
  ]),
  travel: registry('Travel Time Index', 'Navigation integration status and route-category buffers.', 'Travel Time Index Registry', ['ROUTE CATEGORY', 'BUFFER TIME', 'NAVIGATION PROVIDER', 'TRAFFIC INTEGRATION', 'APPLICATION RULE'], [
    ['Airport transfer', '20 minutes', 'Google Maps', 'Traffic-aware ETA enabled', 'Applied before scheduled pickup'],
    ['Ferry terminal', '15 minutes', 'Google Maps', 'Traffic-aware ETA enabled', 'Applied before scheduled pickup'],
    ['Property transfer', '10 minutes', 'Google Maps', 'Traffic-aware ETA enabled', 'Applied before scheduled pickup'],
    ['Cross-border', '60 minutes', 'Amap', 'Traffic-aware ETA enabled', 'Applied before scheduled pickup'],
    ['Peak-hour adjustment', '15 minutes', 'Google Maps', 'Traffic-aware ETA enabled', 'Weekdays 17:00–20:00'],
  ], true, undefined, ['Google Maps · Reference integration', 'Amap · Reference integration']),
  notifications: registry('System Notifications', 'Uploaded event templates, channels and delivery history.', 'Notification Center', ['TEMPLATE ID', 'EVENT', 'TRIGGER', 'CHANNELS', 'STATUS'], [
    ['NT-001', 'Booking confirmation', 'Entitlement confirmed', 'Email, In-app', 'Enabled'],
    ['NT-002', 'Trip reminder', '30 minutes before pickup', 'SMS, Push', 'Enabled'],
    ['NT-003', 'Driver assigned', 'Driver assignment received', 'SMS, In-app', 'Enabled'],
    ['NT-004', 'Trip started', 'Driver starts trip', 'Push, In-app', 'Enabled'],
    ['NT-005', 'Booking cancelled', 'Cancellation confirmed', 'Email, SMS', 'Enabled'],
  ]),
  categories: registry('Location Categories', 'Standard pickup and drop-off location classifications.', 'Location Categories Registry', ['CATEGORY CODE', 'CATEGORY', 'DESCRIPTION', 'STATUS'], [
    ['LC-001', 'Airport', 'Airport terminals and arrivals', 'Enabled'], ['LC-002', 'Ferry Terminal', 'Passenger ferry terminals', 'Enabled'], ['LC-003', 'Casino', 'Gaming and entertainment entrances', 'Enabled'], ['LC-004', 'Hotel', 'Hotel lobbies and concierge desks', 'Enabled'], ['LC-005', 'Shopping Mall', 'Retail complex pickup points', 'Enabled'], ['LC-006', 'Convention Center', 'Conference and exhibition entrances', 'Enabled'],
  ]),
  blacklist: registry('Blacklist', 'Customer restrictions and deactivated driver records.', 'Restriction Registry', ['CUSTOMER', 'MEMBER ID', 'REASON', 'EFFECTIVE', 'EXPIRY', 'STATUS'], [
    ['Guest E', 'g_005', 'Demo risk restriction', '2026-06-01', '2026-12-31', 'Enabled'],
  ], false, 'ADD CUSTOMER'),
  tiers: registry('Membership Tiers', 'Uploaded tier benefits and loyalty-system qualification references.', 'Membership Benefits Matrix', ['BENEFIT', 'SILVER', 'GOLD', 'PLATINUM', 'DIAMOND', 'VIP'], [
    ['Monthly entitlement quota', '1 transfer', '2 transfers', '4 transfers', '8 transfers', '12 transfers'],
    ['Booking priority', 'Standard', 'Standard', 'Priority', 'Priority', 'Priority'],
    ['Vehicle upgrade', 'Standard rules', 'Standard rules', 'Included', 'Included', 'Included'],
    ['Advance booking window', '24 hours', '24 hours', '24 hours', '24 hours', '24 hours'],
    ['Cancellation flexibility', 'Standard rules', 'Standard rules', 'Included', 'Included', 'Included'],
  ], true, undefined, ['Silver · 1,824 active members', 'Gold · 956 active members', 'Platinum · 428 active members', 'Diamond · 186 active members', 'VIP · 64 active members']),
  selfBooking: registry('Self-Booking Control', 'Eligibility rules, editable property quotas and audited escalation.', 'Property Entitlement Quotas', ['PROPERTY', 'DAILY USED / QUOTA', 'WEEKLY USED / QUOTA', 'MONTHLY USED / QUOTA', 'WHEN EXHAUSTED'], [
    ['City of Dreams', '42 / 60', '212 / 360', '950 / 1440', 'Waitlist'], ['Studio City', '36 / 48', '180 / 288', '724 / 1152', 'Waitlist'], ['Altira', '18 / 24', '80 / 144', '388 / 576', 'Waitlist'], ['Grand Hyatt', '20 / 36', '126 / 216', '504 / 864', 'Auto-reject'],
  ], false, undefined, ['Lead time · Minimum 24 hours', 'Advance window · Maximum 30 days', 'Customer limit · One active request per day', 'Eligibility source · Loyalty system']),
  csQualify: registry('CS Qualify Control', 'Customer-service qualification references and manager escalation.', 'Qualification References', ['GUEST', 'GUEST ID', 'TIER', 'PROPERTY', 'LOYALTY OUTCOME', 'STATUS'], [
    ['Guest A', 'MB-31050', 'Platinum', 'City of Dreams', 'Meets threshold', 'Eligible'], ['Guest B', 'MB-31051', 'Gold', 'Studio City', 'Below threshold', 'Manager approval required'], ['Guest C', 'MB-31052', 'Diamond', 'Altira', 'Meets threshold', 'Eligible'],
  ], true, undefined, ['Eligible guest · Membership tier and entitlement must qualify', 'Exceptions · Manager approval required', 'Qualification outcome only; no underlying monetary metrics']),
  system: registry('System Configuration', 'SSO user administration, access references, integrations and audit logs.', 'Corporate SSO Users', ['NAME', 'CORPORATE EMAIL', 'ROLE', 'PROPERTY ACCESS', 'IDENTITY PROVIDER', 'STATUS'], [
    ['User A', 'masked@example.invalid', 'Operations Manager', 'All properties', 'Corporate SSO', 'Enabled'], ['User B', 'masked@example.invalid', 'Dispatcher', 'City of Dreams', 'Corporate SSO', 'Enabled'], ['User C', 'masked@example.invalid', 'Property CS Staff', 'Studio City', 'Corporate SSO', 'Enabled'],
  ], true, undefined, ['Users', 'Permissions', 'Parameters', 'Integrations', 'Audit Log']),
  register: registry('Configuration Register', 'Configuration ownership and upload-managed versus screen-managed areas.', 'Configuration Ownership Register', ['CONFIGURATION AREA', 'MECHANISM', 'PORTAL ACCESS', 'OWNER', 'VERSION', 'SOURCE / SCREEN'], [
    ['Property list & details', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Cancellation reasons', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Notification templates', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Location categories', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Membership tiers', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Cancellation fee rules', 'Code constant', 'Read-only', 'Configuration administrator', '2026.06.24', 'External fee policy reference only'],
    ['Notification channels', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Tier qualification rules', 'Loyalty system', 'Read-only', 'Loyalty administrator', '2026.06.24', 'External qualification source'],
    ['System parameters', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Route-category buffer time', 'Configuration upload', 'Read-only', 'Configuration administrator', '2026.06.24', 'Managed via configuration upload'],
    ['Driver blacklist', 'Driver deactivation', 'Screen-managed', 'Operations Manager', '2026.06.24', 'Driver Management'],
    ['Commonly used locations', 'Portal screen', 'Editable', 'Operations Manager', '2026.06.24', 'Common Used Locations'],
    ['Complimentary quota per property', 'Portal screen', 'Editable', 'Operations Manager', '2026.06.24', 'Self-Booking Control'],
    ['Customer blacklist', 'Portal screen', 'Editable', 'Risk control', '2026.06.24', 'Blacklist'],
    ['Driver management', 'Screen-managed', 'Editable', 'Operations Manager', '2026.06.24', 'Driver Management'],
  ], true, undefined, ['15 configuration areas · Uploaded and screen-managed', '4 screen-managed operational areas', 'User administration is separately permitted']),
}

const rows = ref<RegistryRow[]>([])
const query = ref('')
const tab = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalItems = ref(0)
const refreshKey = ref(0)
const sourceGroup = computed(() => portalSources[props.section === 'self-booking' ? 'selfBooking' : props.section === 'cs-qualify' ? 'csQualify' : props.section])
const activeTab = computed(() => tab.value || sourceGroup.value?.defaultTab || '')
const activeSource = computed(() => sourceGroup.value?.tabs[activeTab.value])
const config = computed(() => {
  const value = registries[props.section === 'self-booking' ? 'selfBooking' : props.section === 'cs-qualify' ? 'csQualify' : props.section] ?? registries.properties!
  if (isDemoMode) return value
  const isCustomerBlacklist = props.section === 'blacklist' && activeTab.value === 'customers'
  return { ...value, panelTitle: Object.keys(sourceGroup.value?.tabs ?? {}).length > 1 ? portalTabLabel(activeTab.value) : value.panelTitle, columns: activeSource.value?.fields.map((field) => field.replace(/([A-Z])/g, ' $1').replaceAll('.', ' ').toUpperCase()) ?? value.columns, isReadOnly: !isCustomerBlacklist, addLabel: isCustomerBlacklist ? value.addLabel : undefined, notes: undefined }
})
const dialog = ref<'view' | 'edit' | 'add' | null>(null)
const draft = ref<RegistryRow>({ rowId: '', cells: [] })
const originalCells = ref<string[]>([])
const saveError = ref('')
const draggedLocationId = ref<string | null>(null)
const liveError = ref('')
function cell(value: unknown): string { if (value == null) return '—'; if (Array.isArray(value)) return value.map(cell).join(', '); if (typeof value === 'object') return Object.values(value).map(cell).join(' · '); return String(value) }
function isActiveColumn(index: number): boolean {
  const source = activeSource.value ?? sourceGroup.value?.tabs[tab.value.toLowerCase()]
  return source?.fields[index] === 'isActive'
}
function isLocationDataCell(index: number): boolean {
  return props.section === 'locations' && !isActiveColumn(index)
}
function isPropertyDataCell(index: number): boolean {
  const column = (config.value.columns[index] ?? '').toUpperCase()
  return props.section === 'properties' || column.includes('PROPERTY')
}
function fieldCell(field: string, value: unknown): string { return field === 'isActive' ? value === true ? 'Enabled' : 'Disabled' : cell(value) }
watch(() => props.section, () => {
  rows.value = []; query.value = ''; tab.value = ''; page.value = 1; totalItems.value = 0; liveError.value = ''
  if (isDemoMode) rows.value = structuredClone(config.value.rows)
}, { immediate: true })
watch([activeSource, page, pageSize, query, () => route.query.q, refreshKey], async ([source], _previous, onCleanup) => {
  if (isDemoMode || !source) return
  const controller = new AbortController()
  onCleanup(() => controller.abort())
  rows.value = []; totalItems.value = 0; liveError.value = ''
  try {
    const search = [query.value, String(route.query.q ?? '')].filter(Boolean).join(' ')
    const envelope = await api.requestEnvelope<Record<string, unknown> | Record<string, unknown>[]>(portalSourcePath(source, page.value, pageSize.value, search), { signal: controller.signal })
    if (controller.signal.aborted) return
    const values = Array.isArray(envelope.data) ? envelope.data : [envelope.data]
    rows.value = values.map((value, index) => ({ rowId: cell(source.idField ? portalField(value, source.idField) : index), cells: source.fields.map((field) => fieldCell(field, portalField(value, field))) }))
    totalItems.value = envelope.meta?.pagination?.totalItems ?? values.length
  } catch (cause) { if (!controller.signal.aborted) liveError.value = apiErrorMessage(cause) }
}, { immediate: true })
const displayRows = computed(() => {
  if (!isDemoMode) return rows.value
  if (props.section === 'blacklist' && tab.value === 'Drivers') return store.drivers.filter((driver) => driver.status !== 'Active').map((driver) => ({ rowId: driver.driverId, cells: [driver.fullName, driver.driverId, 'Driver deactivated', '2026-10-09', '—', driver.status] }))
  if (props.section === 'blacklist' && tab.value === 'Hit alerts') return store.blacklistHits.map((hit) => ({ rowId: hit.hitId, cells: [hit.guestName, hit.guestId, hit.reason ?? 'Blocked booking attempt', hit.occurredAt.slice(0, 10) || '—', '—', 'Active'] }))
  const search = [query.value, String(route.query.q ?? '')].filter(Boolean).join(' ').trim().toLowerCase()
  return rows.value.filter((row) => [...row.cells, ...row.cells.map(t)].join(' ').toLowerCase().includes(search))
})
const visible = computed(() => isDemoMode ? displayRows.value.slice((page.value - 1) * pageSize.value, page.value * pageSize.value) : displayRows.value)
async function openDialog(mode: 'view' | 'edit' | 'add', row?: RegistryRow): Promise<void> {
  dialog.value = mode; saveError.value = ''; draft.value = row ? { rowId: row.rowId, cells: [...row.cells] } : { rowId: `row_${Date.now()}`, cells: config.value.columns.map(() => '') }
  if (mode === 'add' && props.section === 'blacklist') draft.value.cells[5] = 'Enabled'
  originalCells.value = [...draft.value.cells]
  const source = activeSource.value
  if (isDemoMode || mode === 'add' || !row || !source?.hasDetails) return
  try {
    const detail = await api.request<Record<string, unknown>>(`${source.path}/${encodeURIComponent(row.rowId)}`)
    draft.value = { rowId: row.rowId, cells: source.fields.map((field) => fieldCell(field, portalField(detail, field))) }
    originalCells.value = [...draft.value.cells]
  } catch (cause) { saveError.value = apiErrorMessage(cause) }
}
async function save(): Promise<void> {
  if (!isDemoMode) {
    if (props.section !== 'blacklist' || activeTab.value !== 'customers') return
    const [customerName, memberId, reason, effectiveDate, expiryDate, status] = draft.value.cells
    if (!customerName?.trim() || !memberId?.trim() || !reason?.trim() || !effectiveDate || expiryDate && expiryDate < effectiveDate) { saveError.value = 'Enter a member ID, reason and valid effective period.'; return }
    const values: Record<string, string | boolean | null> = { customerName, memberId, reason, effectiveDate, expiryDate: expiryDate && expiryDate !== '—' ? expiryDate : null, isActive: status === 'Enabled' }
    const fields = ['customerName', 'memberId', 'reason', 'effectiveDate', 'expiryDate', 'isActive'] as const
    const payload = dialog.value === 'add' ? values : Object.fromEntries(fields.filter((_, index) => draft.value.cells[index] !== originalCells.value[index]).map((field) => [field, values[field]]))
    if (dialog.value === 'edit' && Object.keys(payload).length === 0) { dialog.value = null; return }
    const body = JSON.stringify(payload)
    try {
      if (dialog.value === 'add') await api.request('/api/platform/staff/v1/customer-blacklists', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body })
      else await api.request(`/api/platform/staff/v1/customer-blacklists/${encodeURIComponent(draft.value.rowId)}`, { method: 'PATCH', body })
      dialog.value = null; refreshKey.value++
    } catch (cause) { saveError.value = apiErrorMessage(cause) }
    return
  }
  if (!draft.value.cells[0]?.trim()) { saveError.value = 'The first field is required.'; return }
  if (props.section === 'blacklist') {
    const [, guestId, reason, effectiveDate, expiryDate, status] = draft.value.cells
    if (!guestId || !reason || !effectiveDate || expiryDate && expiryDate < effectiveDate) { saveError.value = 'Enter a member ID, reason and valid effective period.'; return }
    store.setGuestRestriction(guestId, status === 'Enabled')
  }
  const index = rows.value.findIndex((row) => row.rowId === draft.value.rowId)
  const saved = { rowId: draft.value.rowId, cells: [...draft.value.cells] }
  if (index < 0) rows.value.unshift(saved)
  else rows.value[index] = saved
  dialog.value = null
}
function reorderLocation(targetId: string): void {
  if (!isDemoMode) return
  if (props.section !== 'locations' || !draggedLocationId.value || draggedLocationId.value === targetId) return
  const source = rows.value.findIndex((row) => row.rowId === draggedLocationId.value)
  const target = rows.value.findIndex((row) => row.rowId === targetId)
  if (source < 0 || target < 0 || rows.value[source]?.cells[1] !== rows.value[target]?.cells[1]) return
  const [moved] = rows.value.splice(source, 1)
  if (moved) rows.value.splice(target, 0, moved)
  draggedLocationId.value = null
}
function moveLocation(rowId: string, direction: -1 | 1): void {
  if (!isDemoMode) return
  if (props.section !== 'locations') return
  const row = rows.value.find((item) => item.rowId === rowId)
  if (!row) return
  const group = rows.value.filter((item) => item.cells[1] === row.cells[1])
  const index = group.findIndex((item) => item.rowId === rowId)
  const neighbor = group[index + direction]
  if (!neighbor) return
  draggedLocationId.value = rowId
  reorderLocation(neighbor.rowId)
}
</script>

<template>
  <div class="page-heading"><h1>{{ t(config.title) }}</h1><p>{{ t(config.description) }}</p></div>
  <p v-if="liveError" class="form-error" role="alert">{{ liveError }}</p>
  <div v-if="config.notes" class="reference-cards"><div v-for="note in config.notes" :key="note" class="reference-card">{{ t(note) }}</div></div>
  <section class="filter-bar portal-filter"><span v-if="(isDemoMode && config.isReadOnly) || (!isDemoMode && !activeSource?.isSearchable)" class="muted">{{ t('Reference data') }}</span><label v-else>{{ t('Search') }}<input v-model="query" :placeholder="`${t('Search')} ${t(config.title)}`" /></label><button class="text-button" @click="query = ''; tab = ''; page = 1">{{ t('Reset') }}</button><span class="filter-note">{{ t('Filters apply immediately') }}</span></section>
  <section class="panel"><div class="panel-heading"><h2>{{ t(config.panelTitle) }}</h2><button v-if="config.addLabel && (section !== 'blacklist' || !tab || tab === 'Customers' || tab === 'customers')" class="primary-button" @click="openDialog('add')">{{ t(config.addLabel) }}</button></div><div v-if="!isDemoMode && sourceGroup && Object.keys(sourceGroup.tabs).length > 1" class="tabs"><button v-for="name in Object.keys(sourceGroup.tabs)" :key="name" :class="{ active: activeTab === name }" @click="tab = name; page = 1">{{ t(portalTabLabel(name)) }}</button></div><div v-else-if="isDemoMode && section === 'blacklist'" class="tabs"><button v-for="name in ['Customers', 'Drivers', 'Hit alerts']" :key="name" :class="{ active: (tab || 'Customers') === name }" @click="tab = name">{{ t(name) }}</button></div><div class="table-scroll"><table class="data-table portal-table" :aria-label="t(config.panelTitle)"><thead><tr><th scope="col" v-for="column in config.columns" :key="column">{{ t(column) }} ↕</th><th scope="col">{{ t('ACTIONS') }}</th></tr></thead><tbody><tr v-for="row in visible" :key="row.rowId" :draggable="isDemoMode && section === 'locations'" @dragstart="draggedLocationId = row.rowId" @dragover.prevent @drop="reorderLocation(row.rowId)" @dragend="draggedLocationId = null"><td v-for="(cell, index) in row.cells" :key="index"><span v-if="cell === 'Enabled' || cell === 'Active' || cell === 'Eligible'" class="status-tag active">● {{ t(cell) }}</span><span v-else-if="cell === 'Disabled'" class="status-tag inactive">● {{ t(cell) }}</span><span v-else-if="cell === 'Manager approval required'" class="status-tag pending">● {{ t(cell) }}</span><span v-else>{{ isLocationDataCell(index) || isPropertyDataCell(index) ? cell : t(cell) }}</span></td><td class="table-actions"><button @click="openDialog('view', row)">{{ t(config.isReadOnly ? 'View details' : 'View') }}</button><button v-if="!config.isReadOnly && (section !== 'blacklist' || !tab || tab === 'Customers' || tab === 'customers')" @click="openDialog('edit', row)">{{ t('Edit') }}</button><template v-if="isDemoMode && section === 'locations'"><button :aria-label="`${t('Move up')}: ${row.cells[0] ?? ''}`" @click="moveLocation(row.rowId, -1)">↑</button><button :aria-label="`${t('Move down')}: ${row.cells[0] ?? ''}`" @click="moveLocation(row.rowId, 1)">↓</button></template></td></tr><tr v-if="visible.length === 0"><td :colspan="config.columns.length + 1" class="empty-cell">{{ t('No matching records') }}</td></tr></tbody></table></div><DataPager v-model:page="page" v-model:pageSize="pageSize" :total-items="isDemoMode ? displayRows.length : totalItems" /><p v-if="isDemoMode && section === 'locations'" class="panel-note">{{ t('Drag locations within the same region to reorder in this local demo.') }}</p></section>
  <div v-if="dialog" class="modal-backdrop" @click.self="dialog = null"><section class="dialog" role="dialog" aria-modal="true" :aria-label="`${t(config.title)} ${t('details')}`"><header class="dialog-header"><h2>{{ dialog === 'add' ? `${t('Add')} ${t(config.title)}` : dialog === 'edit' ? `${t('Edit')} ${t(config.title)}` : t('Record Details') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="dialog = null">×</button></header><div class="dialog-body form-grid"><label v-for="(column, index) in config.columns" :key="column" class="field-label">{{ t(column) }}<select v-if="isActiveColumn(index)" v-model="draft.cells[index]" :disabled="dialog === 'view'"><option value="Enabled">{{ t('Enabled') }}</option><option value="Disabled">{{ t('Disabled') }}</option></select><input v-else v-model="draft.cells[index]" :type="column.includes('DATE') ? 'date' : 'text'" :disabled="dialog === 'view'" /></label><p v-if="saveError" class="form-error">{{ t(saveError) }}</p><p v-if="config.isReadOnly && isDemoMode" class="muted">{{ t('This configuration is reference-only and managed through upload.') }}</p></div><footer class="dialog-footer"><button class="secondary-button" @click="dialog = null">{{ t('CLOSE') }}</button><button v-if="dialog !== 'view'" class="primary-button" @click="save">{{ t('SAVE') }}</button></footer></section></div>
</template>
