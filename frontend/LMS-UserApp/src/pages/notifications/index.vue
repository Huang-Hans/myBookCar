<script setup lang="ts">
import { t } from '@/i18n'
import { ref } from 'vue'
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import SegmentedChoice from '@/components/SegmentedChoice.vue'
import StatePanel from '@/components/StatePanel.vue'
import { displayError, requireSession, type Pagination } from '@/api/http'
import { staffApi, type StaffNotice } from '@/api/staff'
import { formatDateTime } from '@/api/presentation'

const notices = ref<StaffNotice[]>([])
const pagination = ref<Pagination | null>(null)
const page = ref(1)
const unreadOnly = ref(false)
const filters = [{ value: 'ALL', label: '全部' }, { value: 'UNREAD', label: '未讀' }]
const loading = ref(false)
const error = ref('')
let refreshTimer: ReturnType<typeof setInterval> | undefined
onShow(() => { if (requireSession()) { void load(); refreshTimer = setInterval(() => { void load() }, 30000) } })
onHide(() => { if (refreshTimer) clearInterval(refreshTimer) })
onUnload(() => { if (refreshTimer) clearInterval(refreshTimer) })
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try { const result = await staffApi.notices(unreadOnly.value, page.value); notices.value = result.data; pagination.value = result.meta?.pagination ?? null }
  catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}
function changePage(next: number): void { page.value = next; void load() }
function selectFilter(value: string): void { unreadOnly.value = value === 'UNREAD'; page.value = 1; void load() }
async function markRead(notice: StaffNotice): Promise<void> {
  if (notice.isRead) return
  try { await staffApi.markNoticeRead(notice.notificationId); notice.isRead = true; if (unreadOnly.value) notices.value = notices.value.filter((item) => item.notificationId !== notice.notificationId) }
  catch (cause) { error.value = displayError(cause) }
}
async function markAllRead(): Promise<void> {
  try { await staffApi.markAllNoticesRead(); notices.value = unreadOnly.value ? [] : notices.value.map((item) => ({ ...item, isRead: true })) }
  catch (cause) { error.value = displayError(cause) }
}
</script>
<template><AppShell title="消息通知" subtitle="重要行程消息不遺漏"><view class="section-title"><text>{{ t('通知中心') }}</text><button class="text-button" @tap="markAllRead">{{ t('全部標為已讀') }}</button></view><view class="filter-toolbar"><view class="filter-select"><SegmentedChoice :model-value="unreadOnly ? 'UNREAD' : 'ALL'" :options="filters" :label="t('通知篩選')" @update:model-value="selectFilter" /></view><button class="refresh-button" :aria-label="t('重新整理')" :disabled="loading" @tap="load"><text aria-hidden="true">↻</text></button></view><text v-if="loading" class="muted">{{ t('載入中…') }}</text><StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load">{{ t('重試') }}</button></StatePanel><StatePanel v-else-if="!notices.length" :message="t('目前沒有通知')" /><button v-for="notice in notices" :key="notice.notificationId" class="card notice" @tap="markRead(notice)"><view class="notice-top"><text class="notice-type"><text v-if="!notice.isRead" class="dot">● </text>{{ notice.type.replaceAll('_', ' ') }}</text><text class="muted">{{ formatDateTime(notice.occurredAt) }}</text></view><text class="notice-summary">{{ notice.summary }}</text><text v-if="notice.tripReference" class="reference">{{ notice.tripReference }}</text></button><view v-if="pagination && pagination.totalPages > 1" class="pager"><button class="secondary-button" :disabled="page <= 1" @tap="changePage(page - 1)">{{ t('上一頁') }}</button><text>{{ page }} / {{ pagination.totalPages }}</text><button class="secondary-button" :disabled="page >= pagination.totalPages" @tap="changePage(page + 1)">{{ t('下一頁') }}</button></view></AppShell></template>
<style scoped>.filter-toolbar { display:flex; align-items:center; gap:12px; margin:0 0 19px; }.filter-select { flex:1; min-width:0; }.refresh-button { display:grid; flex:none; width:42px; height:42px; min-height:42px; margin:0; padding:0; place-items:center; border:1px solid #dce1ee; border-radius:50%; background:#fff; color:#4f5a9f; font-size:22px; line-height:1; box-shadow:0 2px 6px #27325d12; }.refresh-button::after { border:0; }.refresh-button[disabled] { opacity:.5; }.notice { display:flex!important; flex-direction:column; align-items:stretch; justify-content:center; width:100%; text-align:left; }.notice-top { display:flex; justify-content:space-between; align-items:center; gap:8px; }.notice-type { color:#4f5a9f; font-size:12px; font-weight:700; }.dot { color:#dba343; }.notice-summary { display:block; margin:10px 0; color:#30384e; font-size:14px; line-height:1.5; }.reference { color:#939bad; font-size:11px; }</style>


