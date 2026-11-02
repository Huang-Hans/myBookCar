<script setup lang="ts">
import { t } from '@/i18n'
import { computed, ref } from 'vue'
const uniApp = uni
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import RoutePair from '@/components/RoutePair.vue'
import StatePanel from '@/components/StatePanel.vue'
import { displayError, requireSession, type Pagination } from '@/api/http'
import { staffApi, type Trip } from '@/api/staff'
import { formatDateTime, isActiveTrip, statusLabel } from '@/api/presentation'

const trips = ref<Trip[]>([])
const pagination = ref<Pagination | null>(null)
const page = ref(1)
const loading = ref(false)
const error = ref('')
const active = computed(() => trips.value.filter(isActiveTrip).sort((a, b) => Date.parse(a.pickupAt) - Date.parse(b.pickupAt)))
let refreshTimer: ReturnType<typeof setInterval> | undefined
onShow(() => { if (requireSession()) { void load(); refreshTimer = setInterval(() => { void load() }, 30000) } })
onHide(() => { if (refreshTimer) clearInterval(refreshTimer) })
onUnload(() => { if (refreshTimer) clearInterval(refreshTimer) })
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try { const result = await staffApi.trips({ page: page.value, pageSize: 50 }); trips.value = result.data; pagination.value = result.meta?.pagination ?? null }
  catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}
function changePage(next: number): void { page.value = next; void load() }
</script>
<template><AppShell title="行程追蹤" active="tracking" subtitle="實時掌握進行中的行程"><view class="section-title"><text>{{ t('進行中 ·') }} {{ active.length }}</text><button class="text-button" @tap="load">{{ t('重新整理') }}</button></view><view class="hint">{{ t('行程狀態由司機及派車系統更新；點擊行程查看完整時間線。') }}</view><text v-if="loading" class="muted">{{ t('載入中…') }}</text><StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load">{{ t('重試') }}</button></StatePanel><StatePanel v-else-if="!active.length" :message="t('目前沒有進行中的行程')" /><view v-for="trip in active" :key="trip.tripId" class="card tracking-card" @tap="uniApp.navigateTo({ url: `/pages/trip-detail/index?tripId=${encodeURIComponent(trip.tripId)}` })"><view class="row"><text class="reference">{{ trip.tripReference }}</text><text class="chip">{{ statusLabel(trip.status) }}</text></view><text class="guest">{{ trip.guest.name }} · {{ formatDateTime(trip.pickupAt) }}</text><RoutePair :pickup-name="trip.route.pickupName" :dropoff-name="trip.route.dropoffName" /><view class="progress"><view class="progress-bar" :style="{ width: ['PENDING','CONFIRMED'].includes(trip.status) ? '20%' : ['DRIVER_ASSIGNED','DRIVER_EN_ROUTE'].includes(trip.status) ? '50%' : '80%' }" /></view><view class="milestones"><text>{{ t('已預約') }}</text><text>{{ t('司機出發') }}</text><text>{{ t('行程中') }}</text></view></view><view v-if="pagination && pagination.totalPages > 1" class="pager"><button class="secondary-button" :disabled="page <= 1" @tap="changePage(page - 1)">{{ t('上一頁') }}</button><text>{{ page }} / {{ pagination.totalPages }}</text><button class="secondary-button" :disabled="page >= pagination.totalPages" @tap="changePage(page + 1)">{{ t('下一頁') }}</button></view></AppShell></template>
<style scoped>
.hint { margin-bottom:18px; padding:12px 15px; border-left:3px solid #dba948; border-radius:8px; background:#fff7e9; color:#8a754b; font-size:12px; line-height:1.5; }
.row { display:flex; align-items:center; justify-content:space-between; }
.reference { font-size:14px; font-weight:700; }
.guest { display:block; margin-top:9px; color:#8991a0; font-size:12px; }
.progress { height:6px; margin-top:20px; border-radius:4px; background:#e9ebf1; overflow:hidden; }
.progress-bar { height:100%; border-radius:4px; background:#d7a64a; transform-origin:left center; animation:progress-fill 900ms cubic-bezier(.2,.75,.25,1) both; transition:width 550ms ease; }
@keyframes progress-fill { from { transform:scaleX(0); } to { transform:scaleX(1); } }
@media (prefers-reduced-motion:reduce) { .progress-bar { animation:none; transition:none; } }
.milestones { display:flex; justify-content:space-between; margin-top:7px; color:#969dad; font-size:10px; }
</style>



