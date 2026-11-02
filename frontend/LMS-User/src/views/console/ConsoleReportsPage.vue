<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { FileChartColumnIncreasing, X } from 'lucide-vue-next'
import DataPager from '../../components/DataPager.vue'
import { useDemoStore } from '../../stores/demo'
import { api, isDemoMode } from '../../api/session'
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

// 列与原型一致。format 决定显示方式：时长（秒）显示为分钟、距离（米）显示为公里、时间按澳门时间显示，空值一律显示为“—”。
type ReportFormat = 'text' | 'property' | 'number' | 'minutes' | 'km' | 'time' | 'actual' | 'source'
interface ReportColumn { label: string; field: string; format?: ReportFormat }
const reportDefinitions = new Map<string, { path: string; type: string; columns: ReportColumn[] }>([
  ['Daily Job Summary', { path: 'daily-job-summaries', type: 'DAILY_JOB_SUMMARY', columns: [
    { label: 'PROPERTY', field: 'propertyId', format: 'property' },
    { label: 'BOOKINGS', field: 'bookings', format: 'number' },
    { label: 'COMPLETED', field: 'completed', format: 'number' },
    { label: 'CANCELLATIONS', field: 'cancellations', format: 'number' },
    { label: 'NO-SHOWS', field: 'noShows', format: 'number' },
    { label: 'REDEMPTION SUMMARY', field: 'redemptionSummary' },
  ] }],
  ['Trip Detail', { path: 'trip-details', type: 'TRIP_DETAIL', columns: [
    { label: 'ID', field: 'tripReference' },
    { label: 'PROPERTY', field: 'propertyId', format: 'property' },
    { label: 'GUEST', field: 'guestName' },
    { label: 'SCHEDULED', field: 'scheduledAt', format: 'time' },
    { label: 'ACTUAL', field: 'actualAt', format: 'actual' },
    { label: 'DRIVER', field: 'driverName' },
    { label: 'VEHICLE', field: 'vehicleDescription' },
    { label: 'DISTANCE', field: 'distanceMeters', format: 'km' },
    { label: 'DURATION', field: 'durationSeconds', format: 'minutes' },
    { label: 'REDEMPTION ID', field: 'loyalty.redemptionId' },
    { label: 'POINTS', field: 'loyalty.pointsRequired', format: 'number' },
    { label: 'ENTITLEMENT', field: 'loyalty.entitlement' },
    { label: 'REDEMPTION STATE', field: 'loyalty.redemptionState' },
    { label: 'SOURCE', field: 'source' },
  ] }],
  ['Driver Utilization', { path: 'driver-utilizations', type: 'DRIVER_UTILIZATION', columns: [
    { label: 'DRIVER', field: 'driverName' },
    { label: 'PROPERTY', field: 'propertyId', format: 'property' },
    { label: 'TRIPS', field: 'tripCount', format: 'number' },
    { label: 'IDLE TIME', field: 'idleSeconds', format: 'minutes' },
    { label: 'OVERTIME', field: 'overtimeSeconds', format: 'minutes' },
    { label: 'REDEMPTION CONTRIBUTION', field: 'redemptionContributionReference' },
  ] }],
  ['Vehicle Utilization', { path: 'vehicle-utilizations', type: 'VEHICLE_UTILIZATION', columns: [
    { label: 'VEHICLE', field: 'vehicleDescription' },
    { label: 'MILEAGE', field: 'mileageMeters', format: 'km' },
    { label: 'TRIP COUNT', field: 'tripCount', format: 'number' },
    { label: 'DOWNTIME', field: 'downtimeSeconds', format: 'minutes' },
    { label: 'REDEMPTION REFERENCE', field: 'redemptionReference' },
  ] }],
  ['Cancellation Report', { path: 'cancellations', type: 'CANCELLATION', columns: [
    { label: 'REASON', field: 'reason' },
    { label: 'SOURCE', field: 'source', format: 'source' },
    { label: 'BEFORE PICKUP', field: 'noticeSecondsBeforePickup', format: 'minutes' },
    { label: 'POLICY REFERENCE', field: 'pointsReturnPolicyCode' },
    { label: 'POINTS-RETURN REFERENCE', field: 'pointsReturnReference' },
  ] }],
])
const cancellationSources: Record<string, string> = { CUSTOMER: 'Customer', STAFF: 'CS staff' }
const macauTime = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Macau', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false })

const frequencyOptions = [
  { value: 'DAILY_0800', label: 'Daily · 08:00' },
  { value: 'WEEKLY_MONDAY', label: 'Weekly · Monday' },
  { value: 'MONTHLY_FIRST_DAY', label: 'Monthly · First day' },
]
const reportTypeLabels = Object.fromEntries([...reportDefinitions].map(([name, definition]) => [definition.type, name]))

interface Schedule { scheduleId: string; emailAddress: string; frequency: string; reportName: string; isActive: boolean }
interface ReportScheduleView { scheduleId: string; recipientEmail: string; frequency: string; reportType: string; registrationStatus: string }
interface PropertyOption { propertyId: string; name: string }
interface ExportOperationView { operationId: string; state: string; failureMessage?: string }
interface DownloadDescriptionView { url: string; fileName: string }

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
const propertyOptions = ref<PropertyOption[]>([])
const emailAddress = ref('operations@melco-resorts.com')
const frequency = ref('DAILY_0800')
const reportName = ref('Daily Job Summary')
const schedules = ref<Schedule[]>([])
const scheduleError = ref('')
const schedulePage = ref(1)
const schedulePageSize = ref(10)
const exporting = ref(false)
const exportMessage = ref('')

const reportProperties = computed(() => isDemoMode
  ? [...new Set(store.bookings.map((booking) => booking.propertyName))]
  : propertyOptions.value.map((property) => property.name))
const previewRows = computed(() => store.bookings.filter((booking) => {
  const date = booking.pickupAt.slice(0, 10)
  return (!propertyFilter.value || booking.propertyName === propertyFilter.value)
    && (!from.value || date >= from.value)
    && (!to.value || date <= to.value)
    && (!service.value || (booking.serviceType ?? 'Property transfer') === service.value)
    && (selectedReport.value !== 'Cancellation Report' || booking.status === 'Cancelled')
}))
const canPreview = computed(() => ['Daily Job Summary', 'Trip Detail', 'Cancellation Report'].includes(selectedReport.value))
const visibleSchedules = computed(() => {
  const start = (schedulePage.value - 1) * schedulePageSize.value
  return schedules.value.slice(start, start + schedulePageSize.value)
})

function frequencyLabel(value: string): string {
  return frequencyOptions.find((option) => option.value === value)?.label ?? value
}
function valueAt(row: Record<string, unknown>, field: string): unknown {
  return field.split('.').reduce<unknown>((value, key) => (value as Record<string, unknown> | null | undefined)?.[key], row)
}
function propertyName(propertyId: string): string {
  return propertyOptions.value.find((item) => item.propertyId === propertyId)?.name ?? propertyId
}
function formatValue(value: unknown, format: ReportFormat): string {
  if (value == null || value === '') return format === 'actual' ? t('Awaiting pickup') : '—'
  if (format === 'minutes') return `${Math.round(Number(value) / 60)} ${t('minutes')}`
  if (format === 'km') return `${(Number(value) / 1000).toFixed(1)} km`
  if (format === 'time' || format === 'actual') return macauTime.format(new Date(String(value))).replace(',', '')
  if (format === 'number') return Number(value).toLocaleString('en-US')
  if (format === 'property') return propertyName(String(value))
  if (format === 'source') return t(cancellationSources[String(value)] ?? String(value))
  return String(value)
}
// 合并多天数据时，部分天的时长或里程未知（row.partial），在这两类数值后标注“部分数据”。
function reportCell(row: Record<string, unknown>, column: ReportColumn): string {
  const value = valueAt(row, column.field)
  const text = formatValue(value, column.format ?? 'text')
  const isMeasure = column.format === 'minutes' || column.format === 'km'
  return isMeasure && row.partial === true && value != null ? `${text} (${t('partial')})` : text
}
function cardStatus(name: string): string {
  if (name === 'Export & Scheduling') return 'Not generated in this session'
  if (!generated.value.includes(name)) return 'Not generated in this session'
  return isDemoMode ? 'Demo request recorded' : 'Read from LMS reporting API'
}

onMounted(() => {
  if (isDemoMode) return
  void api.request<PropertyOption[]>('/api/platform/staff/v1/properties?page=1&pageSize=50')
    .then((values) => { propertyOptions.value = values })
    .catch(() => { /* The property filter stays on all properties when the catalog cannot be read. */ })
  void loadSchedules()
})

async function loadSchedules(): Promise<void> {
  try {
    const values = await api.request<ReportScheduleView[]>('/api/reporting/staff/v1/report-schedules?page=1&pageSize=50')
    schedules.value = values.map((value) => ({
      scheduleId: value.scheduleId,
      emailAddress: value.recipientEmail,
      frequency: value.frequency,
      reportName: reportTypeLabels[value.reportType] ?? value.reportType,
      isActive: value.registrationStatus === 'REGISTERED',
    }))
  } catch (error) {
    scheduleError.value = apiErrorMessage(error)
  }
}

// 预览与导出共用的筛选条件（UTC 边界由澳门日期换算）。
function currentFilter(): Record<string, string> {
  const filter: Record<string, string> = {}
  if (propertyFilter.value) filter.propertyId = propertyOptions.value.find((item) => item.name === propertyFilter.value)?.propertyId ?? propertyFilter.value
  if (from.value) filter.from = macauDateBoundary(from.value, false)
  if (to.value) filter.to = macauDateBoundary(to.value, true)
  if (service.value) filter.serviceType = service.value === 'Cross-border' ? 'CROSS_BORDER' : 'PROPERTY_TRANSFER'
  return filter
}

async function loadReport(): Promise<void> {
  const definition = reportDefinitions.get(selectedReport.value)
  if (!definition) return
  const query = new URLSearchParams({ page: String(reportPage.value), pageSize: String(reportPageSize.value), ...currentFilter() })
  reportError.value = ''
  try {
    const envelope = await api.requestEnvelope<Record<string, unknown>[]>(`/api/reporting/staff/v1/reports/${definition.path}?${query}`)
    reportRows.value = envelope.data
    reportTotalItems.value = envelope.meta?.pagination?.totalItems ?? envelope.data.length
    if (!generated.value.includes(selectedReport.value)) generated.value.push(selectedReport.value)
  } catch (cause) {
    reportRows.value = []
    reportTotalItems.value = 0
    reportError.value = apiErrorMessage(cause)
  }
}

watch([reportPage, reportPageSize], () => { if (!isDemoMode && selectedReport.value) void loadReport() })
watch([propertyFilter, from, to, service], () => {
  if (!isDemoMode && selectedReport.value) {
    if (reportPage.value !== 1) reportPage.value = 1
    else void loadReport()
  }
})

async function generate(name: string): Promise<void> {
  selectedReport.value = name
  if (isDemoMode) {
    if (!generated.value.includes(name)) generated.value.push(name)
    return
  }
  if (reportPage.value !== 1) reportPage.value = 1
  else await loadReport()
}

// 导出 Excel：申请导出后轮询作业状态，完成后取签名下载地址并下载；筛选条件与预览一致。
async function exportExcel(): Promise<void> {
  const definition = reportDefinitions.get(selectedReport.value)
  if (!definition || exporting.value) return
  exporting.value = true
  exportMessage.value = 'Preparing Excel export…'
  try {
    const operation = await api.request<ExportOperationView>('/api/reporting/staff/v1/report-exports', {
      method: 'POST',
      headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]),
      body: JSON.stringify({ reportType: definition.type, format: 'XLSX', filter: currentFilter() }),
    })
    const finished = await waitForExport(operation.operationId)
    if (finished.state !== 'SUCCEEDED') {
      exportMessage.value = finished.failureMessage ?? 'The export did not finish. Try again later.'
      return
    }
    const download = await api.request<DownloadDescriptionView>(`/api/reporting/staff/v1/report-exports/${encodeURIComponent(finished.operationId)}/download-descriptions`)
    window.location.assign(download.url)
    exportMessage.value = ''
  } catch (cause) {
    exportMessage.value = apiErrorMessage(cause)
  } finally {
    exporting.value = false
  }
}

// 每 2 秒查询一次导出状态，最多约 2 分钟。
async function waitForExport(operationId: string): Promise<ExportOperationView> {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    const operation = await api.request<ExportOperationView>(`/api/reporting/staff/v1/report-exports/${encodeURIComponent(operationId)}`)
    if (operation.state === 'SUCCEEDED' || operation.state === 'FAILED') return operation
    await new Promise((resolve) => window.setTimeout(resolve, 2000))
  }
  return { operationId, state: 'FAILED', failureMessage: 'The export is taking longer than expected. Try again later.' }
}

// 打印 / 另存 PDF：用浏览器打印当前预览（打印样式只输出预览表格），服务端不生成 PDF。
function printReport(): void {
  window.print()
}

// 关闭预览弹窗（✕、点击遮罩、Esc）。导出进行中关闭弹窗不会中断下载，只清掉提示。
function closeReport(): void {
  selectedReport.value = ''
  exportMessage.value = ''
}

function closeOnEscape(event: KeyboardEvent): void {
  if (event.key === 'Escape' && selectedReport.value) closeReport()
}
onMounted(() => window.addEventListener('keydown', closeOnEscape))
onUnmounted(() => window.removeEventListener('keydown', closeOnEscape))

function openReport(name: string): void {
  if (name === 'Export & Scheduling') {
    document.getElementById('schedule-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    return
  }
  void generate(name)
}

async function saveSchedule(): Promise<void> {
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailAddress.value)) {
    scheduleError.value = 'Enter a valid recipient email address.'
    return
  }
  const definition = reportDefinitions.get(reportName.value)
  if (!definition) return
  if (!isDemoMode) {
    try {
      await api.request<ReportScheduleView>('/api/reporting/staff/v1/report-schedules', {
        method: 'POST',
        headers: new Headers([['Idempotency-Key', crypto.randomUUID()]]),
        body: JSON.stringify({ recipientEmail: emailAddress.value, frequency: frequency.value, reportType: definition.type }),
      })
      scheduleError.value = ''
      await loadSchedules()
    } catch (error) {
      scheduleError.value = apiErrorMessage(error)
    }
    return
  }
  schedules.value.unshift({
    scheduleId: `sch_${Date.now()}`,
    emailAddress: emailAddress.value,
    frequency: frequency.value,
    reportName: reportName.value,
    isActive: true,
  })
  scheduleError.value = ''
  schedulePage.value = 1
}

async function removeSchedule(id: string): Promise<void> {
  if (isDemoMode) {
    schedules.value = schedules.value.filter((item) => item.scheduleId !== id)
    return
  }
  try {
    await api.request<unknown>(`/api/reporting/staff/v1/report-schedules/${encodeURIComponent(id)}`, { method: 'DELETE' })
    await loadSchedules()
  } catch (error) {
    scheduleError.value = apiErrorMessage(error)
  }
}
</script>

<template>
  <div class="page-heading">
    <h1>{{ t('Report Center') }}</h1>
    <p>{{ t('Operational reports with reference-only loyalty redemption records.') }}</p>
  </div>

  <section class="filter-bar" :aria-label="t('Report Center')">
    <label>{{ t('Property') }}
      <select v-model="propertyFilter">
        <option value="">{{ t('All properties') }}</option>
        <option v-for="name in reportProperties" :key="name" :value="name">{{ t(name) }}</option>
      </select>
    </label>
    <label>{{ t('From') }}<input v-model="from" type="date" /></label>
    <label>{{ t('To') }}<input v-model="to" type="date" /></label>
    <label>{{ t('Service') }}
      <select v-model="service">
        <option value="">{{ t('All services') }}</option>
        <option value="Cross-border">{{ t('Cross-border') }}</option>
        <option value="Property transfer">{{ t('Property transfer') }}</option>
        <option v-if="isDemoMode" value="Complimentary">{{ t('Complimentary') }}</option>
      </select>
    </label>
    <button class="text-button" type="button" @click="propertyFilter = ''; from = ''; to = ''; service = ''">{{ t('Reset') }}</button>
    <span class="filter-note">{{ t('Filters apply immediately') }}</span>
  </section>

  <div class="report-grid">
    <article v-for="report in reports" :key="report.name" class="report-card">
      <FileChartColumnIncreasing :size="20" class="report-icon" />
      <h2>{{ t(report.name) }}</h2>
      <p>{{ t(report.description) }}</p>
      <small>{{ t(cardStatus(report.name)) }}</small>
      <button class="primary-button" type="button" @click="openReport(report.name)">{{ t(report.name === 'Export & Scheduling' ? 'OPEN SCHEDULING' : 'GENERATE') }}</button>
    </article>
  </div>

  <div v-if="selectedReport" class="modal-backdrop" @click.self="closeReport">
  <section class="dialog report-dialog report-preview" role="dialog" aria-modal="true" :aria-label="`${t(selectedReport)} · ${t('Preview')}`">
    <header class="dialog-header">
      <h2>{{ t(selectedReport) }} · {{ t('Preview') }}</h2>
      <button class="icon-button" type="button" :aria-label="t('Close')" @click="closeReport"><X :size="18" /></button>
    </header>
    <div class="dialog-body report-dialog-body">
    <p class="report-dialog-note">{{ t(isDemoMode ? 'Preview uses session demo records only; production reports require backend data.' : 'Read-only operational report from LMS. Loyalty references are display-only.') }}</p>
    <p v-if="exportMessage" class="report-dialog-note" role="status">{{ t(exportMessage) }}</p>
    <template v-if="!isDemoMode">
      <p v-if="reportError" class="form-error" role="alert">{{ reportError }}</p>
      <div class="table-scroll">
        <table class="data-table">
          <thead>
            <tr><th v-for="column in reportDefinitions.get(selectedReport)?.columns ?? []" :key="column.field" scope="col">{{ t(column.label) }}</th></tr>
          </thead>
          <tbody>
            <tr v-for="(row, index) in reportRows" :key="index">
              <td v-for="column in reportDefinitions.get(selectedReport)?.columns ?? []" :key="column.field">{{ reportCell(row, column) }}</td>
            </tr>
            <tr v-if="reportRows.length === 0"><td :colspan="reportDefinitions.get(selectedReport)?.columns.length ?? 1" class="empty-cell">{{ t('No matching records') }}</td></tr>
          </tbody>
        </table>
      </div>
      <DataPager v-model:page="reportPage" v-model:page-size="reportPageSize" :total-items="reportTotalItems" />
    </template>
    <template v-else-if="canPreview">
      <div class="report-preview-summary">
        <span>{{ t('Matching bookings') }} <strong>{{ previewRows.length }}</strong></span>
        <span>{{ t('Completed') }} <strong>{{ previewRows.filter((booking) => booking.status === 'Completed').length }}</strong></span>
        <span>{{ t('Cancelled') }} <strong>{{ previewRows.filter((booking) => booking.status === 'Cancelled').length }}</strong></span>
      </div>
      <div class="table-scroll">
        <table class="data-table" :aria-label="t(selectedReport)">
          <thead>
            <tr>
              <th scope="col">{{ t('BOOKING ID') }}</th>
              <th scope="col">{{ t('GUEST') }}</th>
              <th scope="col">{{ t('PROPERTY') }}</th>
              <th scope="col">{{ t('PICKUP TIME') }}</th>
              <th scope="col">{{ t('STATUS') }}</th>
              <th scope="col">{{ t('SERVICE') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="booking in previewRows" :key="booking.bookingId">
              <td>{{ booking.bookingId }}</td>
              <td>{{ booking.guestName }}</td>
              <td>{{ booking.propertyName }}</td>
              <td>{{ booking.pickupAt.slice(0, 16).replace('T', ' ') }}</td>
              <td>{{ t(booking.status) }}</td>
              <td>{{ t(booking.serviceType ?? 'Property transfer') }}</td>
            </tr>
            <tr v-if="previewRows.length === 0"><td colspan="6" class="empty-cell">{{ t('No matching records') }}</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else class="panel-note">{{ t('This report requires driver or vehicle telemetry from the production backend.') }}</p>
    </div>
    <footer class="dialog-footer">
      <button class="secondary-button" type="button" @click="printReport">{{ t('PRINT / PDF') }}</button>
      <button v-if="!isDemoMode" class="primary-button" type="button" :disabled="exporting" @click="exportExcel">{{ t(exporting ? 'EXPORTING…' : 'EXPORT EXCEL') }}</button>
    </footer>
  </section>
  </div>

  <section id="schedule-form" class="panel schedule-panel">
    <div class="panel-heading">
      <div>
        <h2>{{ t('Scheduled Email Delivery') }}</h2>
        <p>{{ t(isDemoMode ? 'Schedules are saved locally. An email integration is required to send reports.' : 'Schedules are registered with the LMS reporting service.') }}</p>
      </div>
    </div>
    <div class="schedule-form">
      <label class="field-label">{{ t('Recipient email') }}
        <input v-model="emailAddress" type="email" autocomplete="email" />
      </label>
      <label class="field-label">{{ t('Frequency') }}
        <select v-model="frequency">
          <option v-for="option in frequencyOptions" :key="option.value" :value="option.value">{{ t(option.label) }}</option>
        </select>
      </label>
      <label class="field-label">{{ t('Report') }}
        <select v-model="reportName">
          <option v-for="report in reports.slice(0, 5)" :key="report.name" :value="report.name">{{ t(report.name) }}</option>
        </select>
      </label>
      <button class="primary-button" type="button" @click="saveSchedule">{{ t('SAVE SCHEDULE') }}</button>
    </div>
    <p v-if="scheduleError" class="form-error" role="alert">{{ t(scheduleError) }}</p>
  </section>

  <section class="panel">
    <div class="panel-heading"><h2>{{ t('Delivery Schedules') }}</h2></div>
    <div class="table-scroll">
      <table class="data-table" :aria-label="t('Delivery Schedules')">
        <thead>
          <tr>
            <th scope="col">{{ t('RECIPIENT ↕') }}</th>
            <th scope="col">{{ t('FREQUENCY ↕') }}</th>
            <th scope="col">{{ t('REPORT ↕') }}</th>
            <th scope="col">{{ t('STATUS ↕') }}</th>
            <th scope="col">{{ t('ACTIONS') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="schedule in visibleSchedules" :key="schedule.scheduleId">
            <td>{{ schedule.emailAddress }}</td>
            <td>{{ t(frequencyLabel(schedule.frequency)) }}</td>
            <td>{{ t(schedule.reportName) }}</td>
            <td><span class="status-tag" :class="schedule.isActive ? 'active' : 'inactive'">{{ t(schedule.isActive ? 'Enabled' : 'Disabled') }}</span></td>
            <td class="table-actions">
              <button v-if="isDemoMode" type="button" @click="schedule.isActive = !schedule.isActive">{{ t(schedule.isActive ? 'Disable' : 'Enable') }}</button>
              <button type="button" @click="removeSchedule(schedule.scheduleId)">{{ t('Remove') }}</button>
            </td>
          </tr>
          <tr v-if="schedules.length === 0">
            <td colspan="5" class="empty-cell">{{ t('No matching records') }}<br /><small>{{ t('Try a different search or clear the filters.') }}</small></td>
          </tr>
        </tbody>
      </table>
    </div>
    <DataPager v-model:page="schedulePage" v-model:page-size="schedulePageSize" :total-items="schedules.length" />
  </section>
  <div class="portal-meta">
    <span>{{ t(isDemoMode ? '◷ Local demo · External integrations are reference-only' : 'Connected to LMS API') }}</span>
    <span>{{ t('Macau SAR · GMT+8') }}</span>
  </div>
</template>

<style>
/* 报表预览弹窗：表格列多，弹窗加宽；标题与底部按钮固定，中间内容滚动。 */
.dialog.report-dialog { width: min(1180px, 100%); display: flex; flex-direction: column; overflow: hidden; }
.report-dialog .dialog-header, .report-dialog .dialog-footer { flex-shrink: 0; }
.report-dialog-body { min-height: 0; overflow: auto; padding: 0; }
.report-dialog-note { margin: 0; padding: 12px 19px; border-bottom: 1px solid #e6e7ec; background: #fafbfc; color: #626c7e; font-size: 11px !important; }
.report-dialog-body .form-error { padding: 0 19px 10px; }

/* 打印 / 另存 PDF：只输出弹窗里的报表表格，隐藏页面、遮罩背景、分页与按钮。 */
@media print {
  body * { visibility: hidden; }
  .modal-backdrop { position: static; background: none; padding: 0; }
  .report-preview, .report-preview * { visibility: visible; }
  .report-preview { position: absolute; inset: 0 auto auto 0; width: 100%; max-height: none; box-shadow: none; }
  .report-dialog-body { overflow: visible; }
  .report-preview button, .report-preview .data-pager, .report-preview .dialog-footer { display: none; }
}
</style>
