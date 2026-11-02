<script setup lang="ts">
import { t } from '@/i18n'
import { computed, ref } from 'vue'
const uniApp = uni
import { onLoad, onShow } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import TripCard from '@/components/TripCard.vue'
import StatePanel from '@/components/StatePanel.vue'
import { displayError, requireSession, type Pagination } from '@/api/http'
import { staffApi, type Trip } from '@/api/staff'
import { isActiveTrip } from '@/api/presentation'
import { bookingFilterFromHash, bookingFilterFromValue, type BookingFilter } from '@/api/bookingFilter'

const filter = ref<BookingFilter>(bookingFilterFromHash(typeof window === 'undefined' ? '' : window.location.hash) ?? 'ALL')
const search = ref('')
const trips = ref<Trip[]>([])
const pagination = ref<Pagination | null>(null)
const page = ref(1)
const loading = ref(false)
const error = ref('')
const listAnimationCycle = ref(0)
const filters: Array<{ key: BookingFilter; label: string }> = [{ key: 'ALL', label: '全部' }, { key: 'ACTIVE', label: '進行中' }, { key: 'COMPLETED', label: '已完成' }, { key: 'CANCELLED', label: '已取消' }]
let hasReadRouteFilter = false
onLoad((query) => {
  const requested = bookingFilterFromValue(query?.filter)
  if (requested) { filter.value = requested; hasReadRouteFilter = true }
})
const selectedFilterIndex = computed(() => filters.findIndex((item) => item.key === filter.value))
const indicatorStyle = computed(() => ({ transform: `translateX(${selectedFilterIndex.value * 100}%)` }))
const visibleTrips = computed(() => filter.value === 'ACTIVE' ? trips.value.filter(isActiveTrip) : trips.value)
onShow(() => {
  if (!hasReadRouteFilter) {
    const requested = bookingFilterFromHash(typeof window === 'undefined' ? '' : window.location.hash)
    if (requested) filter.value = requested
    hasReadRouteFilter = true
  }
  if (requireSession()) void load()
})
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    const result = await staffApi.trips({ page: page.value, pageSize: 20, search: search.value.trim() || undefined, status: filter.value === 'ALL' || filter.value === 'ACTIVE' ? undefined : filter.value })
    trips.value = result.data; pagination.value = result.meta?.pagination ?? null; listAnimationCycle.value += 1
  } catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}
function selectFilter(value: BookingFilter): void { filter.value = value; page.value = 1; void load() }
function changePage(value: number): void { page.value = value; void load() }
</script>
<template>
  <AppShell title="預約列表" active="bookings" subtitle="查找及管理所有賓客預約">
    <view class="sticky-actions">
      <view class="search">
        <input v-model="search" class="field" :placeholder="t('搜尋訂單或賓客')" @confirm="page = 1; load()" />
        <button class="secondary-button" @tap="page = 1; load()">{{ t('搜尋') }}</button>
      </view>
      <view class="filters">
        <view class="filter-indicator" :style="indicatorStyle" />
        <button v-for="item in filters" :key="item.key" :class="{ active: filter === item.key }" :aria-pressed="filter === item.key" @tap="selectFilter(item.key)">{{ t(item.label) }}</button>
      </view>
      <view class="section-title">
        <text>{{ t('預約記錄') }}</text>
        <button class="text-button" @tap="uniApp.navigateTo({ url: '/pages/booking/index' })">{{ t('＋ 新增') }}</button>
      </view>
    </view>
    <text v-if="loading" class="muted">{{ t('載入中…') }}</text>
    <StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load">{{ t('重試') }}</button></StatePanel>
    <StatePanel v-else-if="!visibleTrips.length" :message="t('沒有符合條件的預約')" />
    <view v-for="(trip, index) in visibleTrips" :key="`${listAnimationCycle}-${trip.tripId}`" class="trip-enter" :style="{ animationDelay: `${index * 65}ms` }"><TripCard :trip="trip" /></view>
    <view v-if="pagination && pagination.totalPages > 1" class="pager">
      <button class="secondary-button" :disabled="page <= 1" @tap="changePage(page - 1)">{{ t('上一頁') }}</button>
      <text>{{ page }} / {{ pagination.totalPages }}</text>
      <button class="secondary-button" :disabled="page >= pagination.totalPages" @tap="changePage(page + 1)">{{ t('下一頁') }}</button>
    </view>
  </AppShell>
</template>
<style scoped>
.sticky-actions { position:sticky; z-index:9; top:0; margin:-20px -18px 12px; padding:20px 18px 10px; background:#f5f6f9; box-shadow:0 5px 14px #1e2e550d; }
.search { display:flex; gap:8px; margin-bottom:18px; }
.search .field { flex:1; min-width:0; }
.filters { position:relative; display:flex; gap:0; width:100%; box-sizing:border-box; margin-bottom:19px; padding:2px; border-radius:18px; background:#e9ebf2; }
.filter-indicator { position:absolute; top:2px; left:2px; width:calc((100% - 4px) / 4); height:calc(100% - 4px); border-radius:16px; background:#4f5a9f; transition:transform 240ms ease; pointer-events:none; }
.filters button { position:relative; z-index:1; flex:1; min-width:0; min-height:34px; margin:0; padding:0 4px; border:0; border-radius:16px; background:transparent; color:#778092; font-size:12px; line-height:34px; white-space:nowrap; transition:color 240ms ease; }
.filters button::after { border:0; }
.filters button.active { color:#fff; }
.trip-enter { animation:trip-rise 480ms cubic-bezier(.2,.75,.25,1) both; }
@keyframes trip-rise { from { opacity:0; transform:translateY(26px); } to { opacity:1; transform:translateY(0); } }
@media (prefers-reduced-motion:reduce) { .trip-enter { animation:none; } }
.pager { display:flex; align-items:center; justify-content:space-between; margin:18px 0; color:#6d7689; font-size:12px; }
</style>



