<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import { Building2, CalendarDays, CarFront, Check, UserRound, X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { isDemoMode } from '../../api/session'
import { lms, mapTrip, tripStageLabel, type TripTimelineEventView } from '../../api/lms'
import type { Booking } from '../../data/demo'
import AmapTripMap, { type MapStop } from '../../components/AmapTripMap.vue'
import { subscribeStaffNotificationConnected, watchStaffTripLocation } from '../../api/notifications'
import { TripTrackSync, type TripTrackState } from '../../api/tripTrackSync'
import type { TripLocationState, TripLocationSubscription } from '../../../../shared/tripLocations/subscription'

const props = defineProps<{ booking: Booking; events: TripTimelineEventView[] }>()
const emit = defineEmits<{
  (event: 'close'): void
  (event: 'refresh', booking: Booking): void
}>()
const { dateLocale, t } = useLocale()
const origin = ref<MapStop>({ name: props.booking.pickupLocation })
const destination = ref<MapStop>({ name: props.booking.dropoffLocation })
const pointError = ref('')
const emptyTrack: TripTrackState = { points: [], isSharing: false, sampled: false, errorCode: null, loaded: false }
const track = ref<TripTrackState>(emptyTrack)
const watchErrorCode = ref<number | null>(null)
let locationSubscription: TripLocationSubscription | null = null
let trackSync: TripTrackSync | null = null
let stopReconnectListener: (() => void) | null = null

const isTrackHidden = computed(() => [40300, 40400].some((code) => code === track.value.errorCode || code === watchErrorCode.value))
const trackPoints = computed(() => isTrackHidden.value ? [] : track.value.points)
const showVehicle = computed(() => !isTrackHidden.value && track.value.isSharing && trackPoints.value.length > 0)
const trackHint = computed(() => {
  if (isTrackHidden.value || trackPoints.value.length > 0) return ''
  if (track.value.isSharing) return 'Waiting for location updates'
  return ''
})

function stopTracking(): void {
  locationSubscription?.dispose()
  trackSync?.dispose()
  stopReconnectListener?.()
  locationSubscription = null
  trackSync = null
  stopReconnectListener = null
  track.value = emptyTrack
  watchErrorCode.value = null
}

function receiveLocationState(sync: TripTrackSync, state: TripLocationState): void {
  watchErrorCode.value = state.errorCode
  if (state.location) sync.receiveLocation(state.location)
  if (state.hasEnded) sync.markEnded()
}

function startTracking(tripId: string): void {
  stopTracking()
  if (isDemoMode) return
  const sync = new TripTrackSync(tripId, lms.tripTrack, (state) => { track.value = state })
  trackSync = sync
  stopReconnectListener = subscribeStaffNotificationConnected(() => sync.refill())
  locationSubscription = watchStaffTripLocation(tripId, (state) => receiveLocationState(sync, state))
  sync.start()
}

watch(() => props.booking.bookingId, startTracking, { immediate: true })

// 时间线标题：有履约阶段时用阶段名（例如“司机前往中”），否则按事件类型给出可读名称。
const eventTypeLabels: Record<string, string> = { CREATED: 'Booking created', MODIFIED: 'Booking modified', CANCELLED: 'Cancelled' }
function eventTitle(event: TripTimelineEventView): string {
  const stage = event.stage ?? event.orderStatus
  return stage ? tripStageLabel(stage) : eventTypeLabels[event.type] ?? event.type
}
const guestInitial = computed(() => props.booking.guestName.trim().charAt(0).toUpperCase() || '—')

function displayTime(value: string): string {
  return value && !Number.isNaN(Date.parse(value)) ? new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Macau' }).format(new Date(value)) : '—'
}
function displayDate(value: string): string {
  return value && !Number.isNaN(Date.parse(value)) ? new Intl.DateTimeFormat(dateLocale.value, { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'Asia/Macau' }).format(new Date(value)) : '—'
}

async function loadStop(locationId: string | undefined, name: string): Promise<MapStop> {
  if (isDemoMode || !locationId) return { name }
  const location = await lms.location(locationId)
  const coordinates = location.coordinates
  if (!coordinates) return { name: location.name || name }
  return { name: location.name || name, longitude: coordinates.longitude, latitude: coordinates.latitude }
}

async function loadPoints(): Promise<void> {
  pointError.value = ''
  try {
    const [pickup, dropoff] = await Promise.all([
      loadStop(props.booking.pickupLocationId, props.booking.pickupLocation),
      loadStop(props.booking.dropoffLocationId, props.booking.dropoffLocation),
    ])
    origin.value = pickup
    destination.value = dropoff
  } catch (cause) {
    pointError.value = cause instanceof Error ? cause.message : 'Unable to load the trip locations.'
  }
}

async function refreshTrip(): Promise<void> {
  if (isDemoMode) return
  try {
    const detail = mapTrip(await lms.trip(props.booking.bookingId))
    emit('refresh', { ...props.booking, ...detail })
  } catch { /* Keep the last status on screen. */ }
}

const poll = window.setInterval(() => { void refreshTrip() }, 15000)
watch(() => props.booking.bookingId, () => { void loadPoints() }, { immediate: true })
onUnmounted(() => { stopTracking(); window.clearInterval(poll) })
</script>

<template>
  <div class="modal-backdrop">
    <section class="dialog track-dialog" role="dialog" aria-modal="true" :aria-label="t('Trip tracking')">
      <header class="track-header">
        <span class="track-header-icon" aria-hidden="true"><CarFront :size="22" /></span>
        <div class="track-header-title">
          <small>{{ t('Trip tracking') }}</small>
          <h2>{{ booking.tripReference || booking.bookingId }}</h2>
        </div>
        <span class="track-status-pill">{{ t(booking.stageLabel ?? booking.status) }}</span>
        <button class="track-close" :aria-label="t('Close')" @click="emit('close')"><X :size="18" /></button>
      </header>
      <div class="track-layout">
        <div class="track-side">
          <section class="track-card">
            <h3 class="track-card-title">{{ t('Booking information') }}</h3>
            <div class="track-card-body">
              <div class="track-guest">
                <span class="track-avatar" aria-hidden="true">{{ guestInitial }}</span>
                <strong>{{ booking.guestName }}</strong>
              </div>
              <div class="track-room"><span>{{ t('Room') }}</span><span>{{ booking.roomNumber || '—' }}</span></div>
              <div class="track-route">
                <div class="track-route-stop"><span class="track-route-badge pickup">A</span><div><small>{{ t('Pickup location') }}</small><strong>{{ booking.pickupLocation }}</strong></div></div>
                <div class="track-route-stop"><span class="track-route-badge dropoff">B</span><div><small>{{ t('Drop-off location') }}</small><strong>{{ booking.dropoffLocation }}</strong></div></div>
              </div>
              <ul class="track-meta">
                <li><CalendarDays :size="18" /><span>{{ displayDate(booking.pickupAt) }} · {{ displayTime(booking.pickupAt) }}</span></li>
                <li><CarFront :size="18" /><span>{{ t(booking.vehicleClass) }}</span></li>
                <li><Building2 :size="18" /><span>{{ booking.propertyName }}</span></li>
                <li><UserRound :size="18" /><span v-if="booking.driverName">{{ booking.driverName }}<template v-if="booking.vehicleLabel"> · {{ booking.vehicleLabel }}</template></span><span v-else class="track-meta-empty">{{ t('No driver assigned yet') }}</span></li>
              </ul>
            </div>
          </section>
          <section class="track-card track-timeline-card">
            <h3 class="track-card-title">{{ t('Trip timeline') }}</h3>
            <div class="track-card-body" tabindex="0" :aria-label="t('Trip timeline')">
              <ol v-if="events.length > 0" class="track-timeline">
                <li v-for="(event, index) in events" :key="event.eventId" :class="{ current: index === events.length - 1 }">
                  <span class="track-timeline-dot" aria-hidden="true"><Check v-if="index < events.length - 1" :size="12" :stroke-width="3" /></span>
                  <div><strong>{{ t(eventTitle(event)) }}</strong><small>{{ displayDate(event.occurredAt) }} {{ displayTime(event.occurredAt) }}</small></div>
                </li>
              </ol>
              <p v-else class="muted">{{ t('No timeline events recorded.') }}</p>
            </div>
          </section>
        </div>
        <section class="track-card track-map-card">
          <h3 class="track-card-title">{{ t('Live vehicle location') }}</h3>
          <div class="track-card-body">
            <AmapTripMap :origin="origin" :destination="destination" :track-points="trackPoints" :show-vehicle="showVehicle" />
            <p v-if="trackHint" class="muted track-map-note">{{ t(trackHint) }}</p>
            <p v-if="track.sampled && trackPoints.length > 0" class="muted track-map-note">{{ t('Track shown in sampled form') }}</p>
            <p v-if="pointError" class="form-error">{{ pointError }}</p>
          </div>
        </section>
      </div>
    </section>
  </div>
</template>

<style scoped>
.dialog.track-dialog { display: flex; width: min(1060px, 100%); height: min(800px, calc(100dvh - 40px)); flex-direction: column; overflow: hidden; border-radius: 14px; background: #fff; }
.track-header { display: flex; padding: 18px 22px; flex: none; align-items: center; gap: 14px; border-bottom: 1px solid #eceef4; }
.track-header-icon { display: grid; width: 46px; height: 46px; flex: none; place-items: center; border-radius: 50%; background: #e9eafb; color: #3a42a8; }
.track-header-title { display: flex; flex-direction: column; gap: 2px; }
.track-header-title small { color: #6b7285; font-size: 12px; }
.track-header-title h2 { margin: 0; color: #1d2333; font-size: 20px; font-weight: 700; letter-spacing: .2px; }
.track-status-pill { margin-left: 4px; padding: 6px 14px; border-radius: 8px; background: #e8edfb; color: #3f57b8; font-size: 12px; font-weight: 600; white-space: nowrap; }
.track-close { display: grid; width: 38px; height: 38px; margin-left: auto; place-items: center; border: 1px solid #e3e5ec; border-radius: 50%; background: #fff; color: #3a3f4f; cursor: pointer; }
.track-close:hover { background: #f4f5fa; }
.track-layout { display: grid; min-height: 0; padding: 18px 18px 20px; flex: 1; grid-template-columns: minmax(260px, 330px) minmax(0, 1fr); grid-template-rows: minmax(0, 1fr); gap: 12px; align-items: stretch; }
.track-side { display: flex; min-width: 0; min-height: 0; flex-direction: column; gap: 12px; }
.track-side > .track-card:first-child { flex: none; }
.track-card { display: flex; min-width: 0; flex-direction: column; overflow: hidden; border: 1px solid #e8eaf1; border-radius: 10px; background: #fff; }
.track-card-title { margin: 0; padding: 11px 16px; flex: none; background: #f3f4fb; color: #1d2333; font-size: 14px; font-weight: 700; }
.track-card-body { padding: 14px 16px; }
.track-timeline-card { min-height: 0; flex: 1; }
.track-timeline-card .track-card-body { min-height: 0; overflow-y: auto; overscroll-behavior: contain; scrollbar-width: thin; }
.track-map-card .track-card-body { min-height: 0; flex: 1; padding: 12px; overflow-y: auto; overscroll-behavior: contain; }
.track-guest { display: flex; align-items: center; gap: 12px; }
.track-guest strong { color: #1d2333; font-size: 14px; }
.track-avatar { display: grid; width: 32px; height: 32px; flex: none; place-items: center; border-radius: 50%; background: #e3e6f8; color: #3a42a8; font-size: 13px; font-weight: 700; }
.track-room { display: flex; margin: 8px 0 12px; padding-bottom: 12px; gap: 34px; border-bottom: 1px solid #eef0f4; color: #4b5266; font-size: 12px; }
.track-route { position: relative; display: flex; padding-bottom: 12px; flex-direction: column; gap: 14px; border-bottom: 1px solid #eef0f4; }
.track-route::before { position: absolute; top: 28px; left: 13px; height: 22px; border-left: 2px dotted #b9bfcc; content: ''; }
.track-route-stop { display: flex; align-items: center; gap: 14px; }
.track-route-stop div { display: flex; min-width: 0; flex-direction: column; gap: 2px; }
.track-route-stop small { color: #6b7285; font-size: 11px; }
.track-route-stop strong { color: #1d2333; font-size: 14px; }
.track-route-badge { display: grid; width: 28px; height: 28px; flex: none; place-items: center; border-radius: 50%; color: #fff; font-size: 13px; font-weight: 700; }
.track-route-badge.pickup { background: #f1ae2e; }
.track-route-badge.dropoff { background: #101820; }
.track-meta { display: flex; margin: 12px 0 0; padding: 0; flex-direction: column; gap: 10px; list-style: none; color: #2b3142; font-size: 13px; }
.track-meta li { display: flex; align-items: center; gap: 16px; }
.track-meta svg { flex: none; color: #3a42a8; }
.track-meta-empty { color: #8a91a3; }
.track-timeline { margin: 0; padding: 0; list-style: none; }
.track-timeline li { position: relative; display: flex; padding-bottom: 14px; gap: 14px; }
.track-timeline li:last-child { padding-bottom: 0; }
.track-timeline li:not(:last-child)::before { position: absolute; top: 22px; bottom: 0; left: 10px; border-left: 2px solid #3a42a8; content: ''; }
.track-timeline-dot { display: grid; width: 22px; height: 22px; flex: none; place-items: center; border-radius: 50%; background: #3a42a8; color: #fff; }
.track-timeline li.current .track-timeline-dot { border: 2px solid #1677ff; background: #fff; box-shadow: inset 0 0 0 3px #fff, inset 0 0 0 9px #1677ff; }
.track-timeline li div { display: flex; flex-direction: column; gap: 2px; }
.track-timeline strong { color: #1d2333; font-size: 13px; }
.track-timeline li.current strong { color: #1677ff; }
.track-timeline small { color: #6b7285; font-size: 12px; }
.track-map-note { margin: 8px 0 0; }
@media (min-width: 861px) and (min-height: 641px) {
  .track-map-card .track-card-body { display: flex; flex-direction: column; }
  .track-map-card :deep(.track-map-wrap),
  .track-map-card :deep(.track-map-stage) { display: flex; min-height: 0; flex: 1; flex-direction: column; }
  .track-map-card :deep(.track-map-stage .track-map) { min-height: 0; height: auto; flex: 1; }
}
@media (max-width: 860px), (max-height: 640px) {
  .dialog.track-dialog { height: auto; overflow-y: auto; }
  .track-layout { flex: none; grid-template-rows: auto; }
  .track-timeline-card { flex: none; }
  .track-timeline-card .track-card-body { max-height: 260px; }
}
@media (max-width: 860px) { .track-layout { grid-template-columns: 1fr; } }
</style>
