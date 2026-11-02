<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { FileChartColumnIncreasing } from 'lucide-vue-next'
import DataPager from '../../components/DataPager.vue'
import { useDemoStore } from '../../stores/demo'
import { api, isDemoMode } from '../../api/session'
import { lms } from '../../api/lms'
import { apiErrorMessage } from '../../api/http'
import { macauDateBoundary } from '../../api/dates'
const { t } = useLocale()
const store = useDemoStore()

const reports = [
  { name: 'Daily Job Summary', description: 'Bookings, completed trips, cancellations, no-shows and redemption references.' },
  { name: 'Trip Detail', description: 'Scheduled and actual times, driver, vehicle, distance, duration and loyalty references.' },
  { name: 'Driver Utilization', description: 'Trips per driver, idle time, overtime and redemption contribution references.' },
  { name: 'Vehicle Utilization', description: 'Mileage, trip count, downtime and entitlement contribution references.' },
  { name: 'Cancellation Report', description: 'Reason, source, notice period and points-return policy reference.' },
  { name: 'Export & Scheduling', description: 'Excel exports, printable PDF reports and local delivery schedules.' },
]
const route = useRoute()
const visibleReports = computed(() => reports.filter((report) => !route.query.q || `${report.name} ${report.description}`.toLowerCase().includes(String(route.query.q).toLowerCase())))
interface Schedule { scheduleId: string; emailAddress: string; frequency: string; reportName: string; isActive: boolean }
interface ReportScheduleView { scheduleId: string; recipientEmail: string; frequency: string; reportType: string; registrationStatus: string }
const reportDefinitions: Record<string, { path: string; type: string; fields: string[] }> = Object.fromEntries([
  ['Daily Job Summary', { path: 'daily-job-summaries', type: 'DAILY_JOB_SUMMARY', fields: ['propertyId', 'bookings', 'completed', 'cancellations', 'noShows', 'updatedAt'] }],
  ['Trip Detail', { path: 'trip-details', type: 'TRIP_DETAIL', fields: ['tripReference', 'guestName', 'propertyId', 'scheduledAt', 'actualAt', 'driverName'] }],
  ['Driver Utilization', { path: 'driver-utilizations', type: 'DRIVER_UTILIZATION', fields: ['driverId', 'driverName', 'propertyId', 'tripCount', 'idleSeconds', 'overtimeSeconds'] }],
  ['Vehicle Utilization', { path: 'vehicle-utilizations', type: 'VEHICLE_UTILIZATION', fields: ['vehicleDescription', 'mileageMeters', 'tripCount', 'downtimeSeconds', 'updatedAt'] }],
  ['Cancellation Report', { path: 'cancellations', type: 'CANCELLATION', fields: ['reasonCode', 'reason', 'source', 'noticeSecondsBeforePickup', 'pointsReturnPolicyCode', 'updatedAt'] }],
])
const propertyFilter = ref('')
const from = ref('')
const to = ref('')
const service = ref('')
const generated = ref<string[]>([])
const reportRows = ref<Record<string, unknown>[]>([])
const reportTotalItems = ref(0)
const reportPage = ref(1)
const reportPageSize = ref(10)
const reportError = ref('')
const selectedReport = ref('')
const propertyOptions = ref<{ propertyId: string; name: string }[]>([])
const reportProperties = computed(() => isDemoMode ? [...new Set(store.bookings.map((booking) => booking.propertyName))] : propertyOptions.value.map((property) => property.name))
onMounted(() => { if (!isDemoMode) void lms.properties().then((values) => { propertyOptions.value = values.map((value) => ({ propertyId: value.propertyId, name: value.name })) }).catch((cause) => { reportError.value = apiErrorMessage(cause) }) })
const previewRows = computed(() => store.bookings.filter((booking) => {
  const date = booking.pickupAt.slice(0, 10)
  return (!propertyFilter.value || booking.propertyName === propertyFilter.value)
    && (!from.value || date >= from.value)
    && (!to.value || date <= to.value)
    && (!service.value || (booking.serviceType ?? 'Property transfer') === service.value)
    && (selectedReport.value !== 'Cancellation Report' || booking.status === 'Cancelled')
}))
const canPreview = computed(() => ['Daily Job Summary', 'Trip Detail', 'Cancellation Report'].includes(selectedReport.value))
const emailAddress = ref('')
const frequency = ref('Daily · 08:00')
const reportName = ref('Daily Job Summary')
const schedules = ref<Schedule[]>([])
onMounted(() => { if (!isDemoMode) void loadSchedules() })
async function loadSchedules(): Promise<void> {
  try { const values = await api.request<ReportScheduleView[]>('/api/reporting/staff/v1/report-schedules?page=1&pageSize=50'); schedules.value = values.map((value) => ({ scheduleId: value.scheduleId, emailAddress: value.recipientEmail, frequency: value.frequency, reportName: value.reportType, isActive: value.registrationStatus === 'REGISTERED' })) }
  catch (error) { scheduleError.value = error instanceof Error ? error.message : 'Unable to load report schedules.' }
}
const scheduleError = ref('')
const page = ref(1)
const pageSize = ref(10)
async function saveSchedule(): Promise<void> {
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailAddress.value)) { scheduleError.value = 'Enter a valid recipient email address.'; return }
  if (!isDemoMode) {
    const reportType = reportDefinitions[reportName.value]?.type
    if (!reportType) return
    try { await api.request<ReportScheduleView>('/api/reporting/staff/v1/report-schedules', { method: 'POST', headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]), body: JSON.stringify({ recipientEmail: emailAddress.value, frequency: frequency.value === 'Weekly · Monday' ? 'WEEKLY_MONDAY' : frequency.value === 'Monthly · First day' ? 'MONTHLY_FIRST_DAY' : 'DAILY_0800', reportType }) }); emailAddress.value = ''; scheduleError.value = ''; await loadSchedules() }
    catch (error) { scheduleError.value = error instanceof Error ? error.message : 'Unable to save report schedule.' }
    return
  }
  schedules.value.unshift({ scheduleId: `sch_${Date.now()}`, emailAddress: emailAddress.value, frequency: frequency.value, reportName: reportName.value, isActive: true })
  emailAddress.value = ''; scheduleError.value = ''
}
async function generate(name: string): Promise<void> {
  selectedReport.value = name
  if (!isDemoMode) {
    if (reportPage.value !== 1) reportPage.value = 1
    else await loadReport()
    return
  }
  if (!generated.value.includes(name)) generated.value.push(name)
}
async function loadReport(): Promise<void> {
  const definition = reportDefinitions[selectedReport.value]
  if (!definition) return
  const query = new URLSearchParams({ page: String(reportPage.value), pageSize: String(reportPageSize.value) })
  if (propertyFilter.value) query.set('propertyId', propertyOptions.value.find((item) => item.name === propertyFilter.value)?.propertyId ?? propertyFilter.value)
  if (from.value) query.set('from', macauDateBoundary(from.value, false))
  if (to.value) query.set('to', macauDateBoundary(to.value, true))
  if (service.value) query.set('serviceType', service.value === 'Cross-border' ? 'CROSS_BORDER' : 'PROPERTY_TRANSFER')
  reportError.value = ''
  try {
    const envelope = await api.requestEnvelope<Record<string, unknown>[]>(`/api/reporting/staff/v1/reports/${definition.path}?${query}`)
    reportRows.value = envelope.data
    reportTotalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data.length
  } catch (cause) { reportRows.value = []; reportTotalItems.value = 0; reportError.value = apiErrorMessage(cause) }
}
watch([reportPage, reportPageSize], () => { if (!isDemoMode && selectedReport.value) void loadReport() })
async function removeSchedule(id: string): Promise<void> {
  if (isDemoMode) { schedules.value = schedules.value.filter((item) => item.scheduleId !== id); return }
  try { await api.request<unknown>(`/api/reporting/staff/v1/report-schedules/${encodeURIComponent(id)}`, { method: 'DELETE' }); await loadSchedules() }
  catch (error) { scheduleError.value = error instanceof Error ? error.message : 'Unable to remove report schedule.' }
}
function reportCell(value: unknown): string { return value == null ? '—' : typeof value === 'object' ? JSON.stringify(value) : String(value) }
function openReport(name: string): void {
  if (name === 'Export & Scheduling') document.getElementById('schedule-form')?.scrollIntoView({ behavior: 'smooth' })
  else void generate(name)
}
</script>

<template>
  <div class="page-heading"><h1>{{ t('Report Center') }}</h1><p>{{ t('Operational reports with reference-only loyalty redemption records.') }}</p></div>
  <section class="filter-bar portal-filter"><label>{{ t('Property') }}<select v-model="propertyFilter"><option value="">{{ t('All properties') }}</option><option v-for="name in reportProperties" :key="name" :value="name">{{ name }}</option></select></label><label>{{ t('From') }}<input v-model="from" type="date" /></label><label>{{ t('To') }}<input v-model="to" type="date" /></label><label>{{ t('Service') }}<select v-model="service"><option value="">{{ t('All services') }}</option><option value="Cross-border">{{ t('Cross-border') }}</option><option value="Property transfer">{{ t('Property transfer') }}</option><option v-if="isDemoMode" value="Complimentary">{{ t('Complimentary') }}</option></select></label><button class="text-button" @click="propertyFilter = ''; from = ''; to = ''; service = ''">{{ t('Reset') }}</button><span class="filter-note">{{ t('Filters apply immediately') }}</span></section>
  <div class="report-grid"><article v-for="report in visibleReports" :key="report.name" class="report-card"><FileChartColumnIncreasing :size="20" class="report-icon" /><h2>{{ t(report.name) }}</h2><p>{{ t(report.description) }}</p><small>{{ isDemoMode ? t(generated.includes(report.name) ? 'Demo request recorded' : 'Not generated in this session') : t('Read from LMS reporting API') }}</small><button class="primary-button" @click="openReport(report.name)">{{ t(report.name === 'Export & Scheduling' ? 'OPEN SCHEDULING' : 'GENERATE') }}</button></article></div>
  <section v-if="selectedReport" class="panel report-preview"><div class="panel-heading"><div><h2>{{ t(selectedReport) }} · {{ t('Local preview') }}</h2><p>{{ t(isDemoMode ? 'Preview uses session demo records only; production reports require backend data.' : 'Read-only operational report from LMS.') }}</p></div><button class="secondary-button" @click="selectedReport = ''">{{ t('CLOSE') }}</button></div><template v-if="!isDemoMode"><p v-if="reportError" class="form-error" role="alert">{{ reportError }}</p><div class="table-scroll"><table class="data-table portal-table"><thead><tr><th v-for="field in reportDefinitions[selectedReport]?.fields ?? []" :key="field" scope="col">{{ field.replace(/([A-Z])/g, ' $1').toUpperCase() }}</th></tr></thead><tbody><tr v-for="(row, index) in reportRows" :key="index"><td v-for="field in reportDefinitions[selectedReport]?.fields ?? []" :key="field">{{ reportCell(row[field]) }}</td></tr></tbody></table></div><DataPager v-model:page="reportPage" v-model:pageSize="reportPageSize" :total-items="reportTotalItems" /></template><template v-else-if="canPreview"><div class="report-preview-summary"><span>{{ t('Matching bookings') }} <strong>{{ previewRows.length }}</strong></span><span>{{ t('Completed') }} <strong>{{ previewRows.filter((booking) => booking.status === 'Completed').length }}</strong></span><span>{{ t('Cancelled') }} <strong>{{ previewRows.filter((booking) => booking.status === 'Cancelled').length }}</strong></span></div><div class="table-scroll"><table class="data-table portal-table" :aria-label="t(selectedReport)"><thead><tr><th scope="col">{{ t('BOOKING ID') }}</th><th scope="col">{{ t('GUEST') }}</th><th scope="col">{{ t('PROPERTY') }}</th><th scope="col">{{ t('PICKUP TIME') }}</th><th scope="col">{{ t('STATUS') }}</th><th scope="col">{{ t('SERVICE') }}</th></tr></thead><tbody><tr v-for="booking in previewRows" :key="booking.bookingId"><td>{{ booking.bookingId }}</td><td>{{ booking.guestName }}</td><td>{{ booking.propertyName }}</td><td>{{ booking.pickupAt.slice(0, 16).replace('T', ' ') }} UTC</td><td>{{ t(booking.status) }}</td><td>{{ t(booking.serviceType ?? 'Property transfer') }}</td></tr><tr v-if="previewRows.length === 0"><td colspan="6" class="empty-cell">{{ t('No matching records') }}</td></tr></tbody></table></div></template><p v-else class="panel-note">{{ t('This report requires driver or vehicle telemetry from the production backend.') }}</p></section>
  <section id="schedule-form" class="panel schedule-panel"><div class="panel-heading"><div><h2>{{ t('Scheduled Email Delivery') }}</h2><p>{{ t(isDemoMode ? 'Schedules are saved locally. An email integration is required to send reports.' : 'Schedules are registered with the LMS reporting service.') }}</p></div></div><div class="schedule-form"><label class="field-label">{{ t('Recipient email') }}<input v-model="emailAddress" type="email" placeholder="operations@example.com" /></label><label class="field-label">{{ t('Frequency') }}<select v-model="frequency"><option value="Daily · 08:00">{{ t('Daily · 08:00') }}</option><option value="Weekly · Monday">{{ t('Weekly · Monday') }}</option><option value="Monthly · First day">{{ t('Monthly · First day') }}</option></select></label><label class="field-label">{{ t('Report') }}<select v-model="reportName"><option v-for="report in reports.slice(0, 5)" :key="report.name" :value="report.name">{{ t(report.name) }}</option></select></label><button class="primary-button" @click="saveSchedule">{{ t('SAVE SCHEDULE') }}</button></div><p v-if="scheduleError" class="form-error">{{ t(scheduleError) }}</p></section>
  <section class="panel"><div class="panel-heading"><h2>{{ t('Delivery Schedules') }}</h2></div><div class="table-scroll"><table class="data-table portal-table" :aria-label="t('Delivery Schedules')"><thead><tr><th scope="col">{{ t('RECIPIENT ↕') }}</th><th scope="col">{{ t('FREQUENCY ↕') }}</th><th scope="col">{{ t('REPORT ↕') }}</th><th scope="col">{{ t('STATUS ↕') }}</th><th scope="col">{{ t('ACTIONS') }}</th></tr></thead><tbody><tr v-for="schedule in schedules" :key="schedule.scheduleId"><td>{{ schedule.emailAddress }}</td><td>{{ t(schedule.frequency) }}</td><td>{{ t(schedule.reportName) }}</td><td><span class="status-tag" :class="schedule.isActive ? 'active' : 'inactive'">● {{ t(schedule.isActive ? 'Enabled' : 'Disabled') }}</span></td><td class="table-actions"><button v-if="isDemoMode" @click="schedule.isActive = !schedule.isActive">{{ t(schedule.isActive ? 'Disable' : 'Enable') }}</button><button @click="removeSchedule(schedule.scheduleId)">{{ t('Remove') }}</button></td></tr><tr v-if="schedules.length === 0"><td colspan="5" class="empty-cell">{{ t('No matching records') }}<br /><small>{{ t('Try a different search or clear the filters.') }}</small></td></tr></tbody></table></div><DataPager v-model:page="page" v-model:pageSize="pageSize" :total-items="schedules.length" /></section>
</template>
