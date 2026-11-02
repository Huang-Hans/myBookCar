<script setup lang="ts">
import { computed, ref } from 'vue'
import { onHide, onLoad, onShow, onUnload } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import RoutePair from '@/components/RoutePair.vue'
import StatePanel from '@/components/StatePanel.vue'
import TripTrackMap, { type MapStop } from '@/components/TripTrackMap.vue'
import TripDispatchSheet from '@/components/TripDispatchSheet.vue'
import { displayError, requireSession } from '@/api/http'
import { staffApi, type Trip, type TripEvent } from '@/api/staff'
import { TripTrackPoller, type TripTrackState } from '@/api/tripTrackPoller'
import type { TripMapMetrics } from '@/api/tripMapRoute'
import { formatDateTime, statusLabel } from '@/api/presentation'
import { t } from '@/i18n'

const tripId = ref('')
const trip = ref<Trip | null>(null)
const events = ref<TripEvent[]>([])
const error = ref('')
const loading = ref(false)
const origin = ref<MapStop>({ name: '' })
const destination = ref<MapStop>({ name: '' })
const propertyName = ref('')
const emptyTrack: TripTrackState = { points: [], isSharing: false, sampled: false, isLoaded: false, isHidden: false }
const track = ref<TripTrackState>(emptyTrack)
const mapMetrics = ref<TripMapMetrics>({ coveredKm: 0, distanceToPickupKm: 0, speedKmh: 0, etaMinutes: 0 })
const visiblePoints = computed(() => track.value.isHidden ? [] : track.value.points)
const showVehicle = computed(() => track.value.isSharing && visiblePoints.value.length > 0)
const canOpenDispatch = computed(() => trip.value?.status === 'CONFIRMED')
const isDispatchSheetOpen = ref(false)

let isVisible = false
let generation = 0
let poller: TripTrackPoller | null = null

onLoad((query) => { tripId.value = typeof query?.tripId === 'string' ? query.tripId : '' })
onShow(() => { isVisible = true; if (requireSession() && tripId.value) void load() })
onHide(() => { isVisible = false; generation++; stopTracking() })
onUnload(() => { isVisible = false; generation++; stopTracking() })

function stopTracking(): void {
  poller?.stop()
  poller = null
}

function openDispatch(): void {
  if (!canOpenDispatch.value || !trip.value) return
  isDispatchSheetOpen.value = true
}

function dispatchOffered(): void { isDispatchSheetOpen.value = false; void load() }

function startTracking(current: Trip): void {
  stopTracking()
  track.value = { ...emptyTrack }
  mapMetrics.value = { coveredKm: 0, distanceToPickupKm: 0, speedKmh: 0, etaMinutes: 0 }
  poller = new TripTrackPoller(current.tripId, staffApi.tripTrack, (state) => { track.value = state },
    () => ['COMPLETED', 'CANCELLED', 'NO_SHOW'].includes(trip.value?.status ?? ''))
  poller.start()
}

async function loadStop(locationId: string, name: string): Promise<MapStop> {
  if (!locationId) return { name }
  try {
    const location = (await staffApi.location(locationId)).data
    return { name: location.name || name, latitude: location.coordinates?.latitude, longitude: location.coordinates?.longitude }
  } catch { return { name } }
}

async function load(): Promise<void> {
  const currentGeneration = ++generation
  loading.value = !trip.value
  error.value = ''
  try {
    const result = await staffApi.trip(tripId.value)
    if (currentGeneration !== generation || !isVisible) return
    const current = result.data
    trip.value = current
    propertyName.value = ''
    origin.value = { name: current.route.pickupName }
    destination.value = { name: current.route.dropoffName }
    startTracking(current)
    const [eventResult, pickup, dropoff, propertyResult] = await Promise.all([
      staffApi.tripEvents(current.tripId).catch(() => null),
      loadStop(current.route.pickupLocationId, current.route.pickupName),
      loadStop(current.route.dropoffLocationId, current.route.dropoffName),
      staffApi.property(current.propertyId).catch(() => null),
    ])
    if (currentGeneration !== generation || !isVisible) return
    if (eventResult) events.value = eventResult.data
    origin.value = pickup
    destination.value = dropoff
    propertyName.value = propertyResult?.data.name ?? ''
  } catch (cause) {
    if (currentGeneration === generation) error.value = displayError(cause)
  } finally {
    if (currentGeneration === generation) loading.value = false
  }
}
</script>

<template>
  <AppShell title="行程詳情" subtitle="預約及行程進度">
    <text v-if="loading" class="muted">{{ t('載入中…') }}</text>
    <StatePanel v-else-if="error || !trip" :message="error || t('找不到行程')" kind="error">
      <button class="secondary-button" @tap="load">{{ t('重試') }}</button>
    </StatePanel>
    <template v-else>
      <view class="card trip-summary" :class="{ 'is-dispatchable': canOpenDispatch }" :role="canOpenDispatch ? 'button' : undefined" :aria-label="canOpenDispatch ? t('派車操作') : undefined" :tabindex="canOpenDispatch ? 0 : undefined" @tap="openDispatch" @keydown.enter="openDispatch">
        <view class="row"><text class="card-title">{{ trip.tripReference }}</text><text class="chip">{{ statusLabel(trip.status) }}</text></view>
        <text class="muted">{{ formatDateTime(trip.pickupAt) }} · {{ trip.serviceType.replaceAll('_', ' ') }}</text>
        <RoutePair :pickup-name="trip.route.pickupName" :dropoff-name="trip.route.dropoffName" />
        <text v-if="canOpenDispatch" class="dispatch-link">{{ t('選擇司機 ›') }}</text>
      </view>
      <TripDispatchSheet v-if="isDispatchSheetOpen && trip" :trip-id="trip.tripId" @close="isDispatchSheetOpen = false" @offered="dispatchOffered" />
      <view class="card map-card">
        <view class="map-heading"><text class="card-title">{{ t('行程地圖') }}</text></view>
        <TripTrackMap :origin="origin" :destination="destination" :track-points="visiblePoints" :show-vehicle="showVehicle" :show-approach-route="trip.status === 'DRIVER_EN_ROUTE' && showVehicle" @metrics="mapMetrics = $event" />
        <view class="map-mileage">
          <view><text class="metric-value">{{ mapMetrics.coveredKm.toFixed(1) }}</text><text class="metric-label">{{ t('已行駛公里') }}</text></view>
          <view><text class="metric-value">{{ mapMetrics.distanceToPickupKm.toFixed(1) }}</text><text class="metric-label">{{ t('距接載地點公里') }}</text></view>
          <view><text class="metric-value">{{ mapMetrics.etaMinutes }} min</text><text class="metric-label">{{ t('預計行車時間') }}</text></view>
        </view>
        <view class="map-source"><text class="source-dot">●</text><text>{{ t('高德路線預估') }}<template v-if="showVehicle"> · {{ t('實時定位') }}</template></text></view>
      </view>
      <view class="card">
        <text class="card-title">{{ t('預約資料') }}</text>
        <view class="detail"><text>{{ t('賓客') }}</text><text>{{ trip.guest.name }}</text></view>
        <view class="detail"><text>{{ t('車型') }}</text><text>{{ trip.vehicleClass.replaceAll('_', ' ') }}</text></view>
        <view class="detail"><text>{{ t('物業') }}</text><text>{{ propertyName || trip.propertyId }}</text></view>
        <view class="detail"><text>{{ t('出發時間') }}</text><text>{{ formatDateTime(trip.pickupAt) }}</text></view>
        <view class="detail"><text>{{ t('訂單狀態') }}</text><text>{{ statusLabel(trip.status) }}</text></view>
        <view class="detail"><text>{{ t('乘客 / 行李') }}</text><text>{{ trip.passengers ?? '—' }} / {{ trip.luggage ?? '—' }}</text></view>
      </view>
      <view class="section-title"><text>{{ t('行程時間線') }}</text><button class="text-button" @tap="load">{{ t('重新整理') }}</button></view>
      <StatePanel v-if="!events.length" :message="t('尚無行程事件')" />
      <view v-for="(event, index) in events" :key="event.eventId || index" class="event">
        <view class="event-dot" /><view><text class="event-title">{{ statusLabel(event.orderStatus || event.type) }}</text><text class="muted">{{ formatDateTime(event.occurredAt) }}</text><text v-if="event.reason" class="muted">{{ event.reason }}</text></view>
      </view>
    </template>
  </AppShell>
</template>

<style scoped>
.row,.detail { display:flex; align-items:center; justify-content:space-between; gap:10px; }
.row { margin-bottom:10px; }
.is-dispatchable { cursor:pointer; }
.is-dispatchable:focus-visible { outline:2px solid #4f5a9f; outline-offset:2px; }
.dispatch-link { display:block; margin-top:12px; color:#4f5a9f; font-size:12px; font-weight:700; text-align:right; }
.detail { padding:13px 0; border-bottom:1px solid #f0f1f4; color:#6d7585; font-size:13px; }
.detail text:last-child { color:#30384d; font-weight:600; }
.event { display:flex; gap:13px; min-height:66px; margin-left:8px; border-left:1px solid #d5d8e6; }
.event-dot { width:11px; height:11px; margin-left:-6px; border-radius:50%; background:#d4a348; }
.event > view:last-child { display:flex; flex-direction:column; gap:5px; }
.event-title { color:#2a3248; font-size:13px; font-weight:700; }
.map-card { position:relative; z-index:0; isolation:isolate; padding:0; overflow:hidden; }
.map-heading { padding:17px; background:#fff; }
.map-mileage { display:flex; min-height:76px; align-items:center; background:#fff; }
.map-mileage > view { display:flex; min-width:0; flex:1; align-items:center; flex-direction:column; gap:5px; border-right:1px solid #e6e6e6; }
.map-mileage > view:last-child { border-right:0; }
.metric-value { color:#101820; font-size:18px; font-weight:700; }
.metric-label { color:#394456; font-size:10px; text-align:center; }
.map-source { display:flex; min-height:40px; align-items:center; justify-content:center; gap:7px; border-top:1px solid #edf0f3; color:#626b79; font-size:11px; }
.source-dot { color:#f1ae2e; }
</style>
