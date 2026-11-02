<script setup lang="ts">
import { locale, t } from '@/i18n'
import { computed, ref } from 'vue'
const uniApp = uni
import { onShow } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import TripCard from '@/components/TripCard.vue'
import StatePanel from '@/components/StatePanel.vue'
import { displayError, requireSession, type CurrentUser } from '@/api/http'
import { staffApi, type DispatchSummary, type Trip } from '@/api/staff'
import { isActiveTrip } from '@/api/presentation'
import { openHomeOverview, openHomeShortcut, type HomeOverview } from './shortcutNavigation'

const user = ref<CurrentUser | null>(null)
const trips = ref<Trip[]>([])
const dispatch = ref<DispatchSummary[]>([])
const error = ref('')
const loading = ref(false)
const activeTrips = computed(() => trips.value.filter(isActiveTrip))
const pendingDispatch = computed(() => dispatch.value.filter((item) => item.canDispatch))
const greeting = computed(() => user.value ? `${locale.value === 'en' ? 'Hello, ' : '你好，'}${user.value.name}` : '員工出行服務總覽')
onShow(() => { if (requireSession()) void load() })
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    const [self, bookingResult] = await Promise.all([staffApi.currentUser(), staffApi.trips({ pageSize: 50 })])
    user.value = self.data; trips.value = bookingResult.data
    try { dispatch.value = (await staffApi.dispatchOrders()).data } catch { dispatch.value = [] }
  } catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}
function openQuickBooking(): void {
  openHomeShortcut('booking', uniApp)
}
function openDispatch(): void {
  openHomeShortcut('dispatch', uniApp)
}
function openOverview(target: HomeOverview): void {
  openHomeOverview(target, uniApp)
}
</script>
<template>
  <AppShell title="工作台" active="home" :subtitle="greeting">
    <view class="section-title"><text>{{ t('近期概況') }}</text><button class="text-button" @tap="load">{{ t('重新整理') }}</button></view>
    <view class="stats">
      <!-- #ifdef H5 -->
      <a class="stat" href="#/pages/tracking/index"><text class="number gold">{{ activeTrips.length }}</text><text>{{ t('進行中行程') }}</text></a>
      <a class="stat" href="#/pages/dispatch/index"><text class="number purple">{{ pendingDispatch.length }}</text><text>{{ t('待派車') }}</text></a>
      <a class="stat" href="#/pages/bookings/index?filter=COMPLETED"><text class="number green">{{ trips.filter(item => item.status === 'COMPLETED').length }}</text><text>{{ t('已完成') }}</text></a>
      <!-- #endif -->
      <!-- #ifndef H5 -->
      <button class="stat" @click="openOverview('tracking')"><text class="number gold">{{ activeTrips.length }}</text><text>{{ t('進行中行程') }}</text></button>
      <button class="stat" @click="openOverview('dispatch')"><text class="number purple">{{ pendingDispatch.length }}</text><text>{{ t('待派車') }}</text></button>
      <button class="stat" @click="openOverview('completed')"><text class="number green">{{ trips.filter(item => item.status === 'COMPLETED').length }}</text><text>{{ t('已完成') }}</text></button>
      <!-- #endif -->
    </view>
    <text class="muted scope-note">{{ t('基於最近 50 個行程及 20 個派車單') }}</text>
    <view class="section-title"><text>{{ t('快捷操作') }}</text></view>
    <view class="shortcuts">
      <!-- #ifdef H5 -->
      <a class="shortcut primary shortcut-from-left" href="#/pages/booking/index"><image class="shortcut-icon" src="/static/icons/tabs/yuyuedan-white.svg" mode="aspectFit" /><text>{{ t('快速預約') }}</text><text class="small">{{ t('為賓客建立行程') }}</text></a>
      <a class="shortcut shortcut-from-right" href="#/pages/dispatch/index"><image class="shortcut-icon" src="/static/icons/tabs/zhipaipaiche-active.svg" mode="aspectFit" /><text>{{ t('派車服務') }}</text><text class="small">{{ t('處理待派車訂單') }}</text></a>
      <!-- #endif -->
      <!-- #ifndef H5 -->
      <button class="shortcut primary shortcut-from-left" @click="openQuickBooking"><image class="shortcut-icon" src="/static/icons/tabs/yuyuedan-white.svg" mode="aspectFit" /><text>{{ t('快速預約') }}</text><text class="small">{{ t('為賓客建立行程') }}</text></button>
      <button class="shortcut shortcut-from-right" @click="openDispatch"><image class="shortcut-icon" src="/static/icons/tabs/zhipaipaiche-active.svg" mode="aspectFit" /><text>{{ t('派車服務') }}</text><text class="small">{{ t('處理待派車訂單') }}</text></button>
      <!-- #endif -->
    </view>
    <view class="section-title"><text>{{ t('最新行程') }}</text><button class="text-button" @tap="uniApp.reLaunch({ url: '/pages/bookings/index' })">{{ t('查看全部 ›') }}</button></view>
    <text v-if="loading" class="muted">{{ t('載入中…') }}</text>
    <StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load">{{ t('重試') }}</button></StatePanel>
    <StatePanel v-else-if="!trips.length" :message="t('暫無行程，從快速預約開始。')" />
    <view v-for="(trip, index) in trips.slice(0, 3)" :key="trip.tripId" class="trip-enter" :style="{ animationDelay: `${index * 110}ms` }"><TripCard :trip="trip" /></view>
  </AppShell>
</template>
<style scoped>
.scope-note { display:block; margin:-16px 0 22px 2px; }
.stats { display:flex; gap:9px; margin-bottom:26px; }
.stat { position:relative; display:flex; box-sizing:border-box; flex:1; flex-direction:column; align-items:center; justify-content:center; gap:8px; min-width:0; margin:0; padding:17px 10px; border:1px solid #e9eaf0; border-radius:15px; background:#fff; color:#7d8699; font-size:11px; line-height:1.4; text-align:center; text-decoration:none; cursor:pointer; touch-action:manipulation; pointer-events:auto; }
.stat::after { border:0; }
.stat:focus-visible { outline:2px solid #4f5a9f; outline-offset:2px; }
.number { font-size:28px; font-weight:700; }
.gold { color:#d6a044; }
.purple { color:#4f5a9f; }
.green { color:#298d70; }
.shortcuts { display:flex; gap:10px; margin-bottom:26px; }
.shortcut { position:relative; display:flex; box-sizing:border-box; flex:1; align-items:center; justify-content:center; flex-direction:column; text-align:center; text-decoration:none; min-height:132px; gap:8px; padding:17px; border:1px solid #e2e4ee; border-radius:16px; background:#fff; color:#252d45; font-size:16px; font-weight:700; cursor:pointer; touch-action:manipulation; pointer-events:auto; }
.shortcut.primary { border:0; background:#505b9d; color:#fff; }
.shortcut-icon { display:block; flex:none; width:28px; height:28px; }
.small { opacity:.7; font-size:11px; font-weight:400; }
.trip-enter { animation:trip-rise 520ms cubic-bezier(.2,.75,.25,1) both; }
@keyframes trip-rise { from { opacity:0; transform:translateY(26px); } to { opacity:1; transform:translateY(0); } }
@media (prefers-reduced-motion:reduce) { .trip-enter { animation:none; } }
</style>





