<script setup lang="ts">
import { t } from '@/i18n/locale'
import { computed, ref } from 'vue'
import { onHide, onLoad, onShow, onUnload } from '@dcloudio/uni-app'

import AppTabBar from '@/components/AppTabBar.vue'
import AppBottomSheet from '@/components/AppBottomSheet.vue'
import { useClientDialog } from '@/composables/useClientDialog'
import { useCustomerTripLocation } from '@/composables/useCustomerTripLocation'
import AmapRouteMap from '@/components/AmapRouteMap.vue'
import { useBookingStore } from '@/stores/booking'
import { vehicleClassLabel } from '@/data/ridePresentation'
import { findService } from '@/data/services'
import { customerStageTimes, customerTripStageIndex, customerTripStages } from '@/data/tripStages'
import { returnFromOrderDetails } from '@/data/orderDetailsRoute'
import { ApiRequestError } from '@/api/http'
import { customerApi, customerTripStatusLabel, productCodeToServiceId, type CustomerLocation, type CustomerTrip } from '@/api/customer'
import type { AmapRouteMetrics, MapLocation } from '@/data/amap'
import type { Booking } from '@/types/booking'
import { onCustomerNotificationsConnected, startCustomerNotificationHub, subscribeCustomerNotifications } from '@/api/notifications'
import type { NotificationMessageV1 } from '@/api/notificationContract'
import { localeTag } from '@/i18n/locale'
import { mapConfig } from '../../../../map.config'
import { isMockDriverPhoneEnabled, resolveContactPhoneNumber, type ContactOption } from './contactOptions'

const { dialog, alert, confirm, close, submit } = useClientDialog()
const { driverLocation, startWatching, stopWatching } = useCustomerTripLocation()

const store = useBookingStore()
store.restore()
const bookingId = ref('')
const isOrderDetailsExpanded = ref(false)
const trip = ref<CustomerTrip | null>(null)
const pickupLocation = ref<CustomerLocation | null>(null)
const dropoffLocation = ref<CustomerLocation | null>(null)
const amapWebKey = mapConfig.webKey
const amapSecurityJsCode = mapConfig.securityJsCode
const mapMetrics = ref<AmapRouteMetrics>({ coveredKm: 0, distanceToPickupKm: 0, speedKmh: 0, etaMinutes: 0, hasCurrentPosition: false })
let isPageActive = false
const unsubscribeNotifications = subscribeCustomerNotifications(handleCustomerNotification)
const unsubscribeReconnect = onCustomerNotificationsConnected(() => { if (isPageActive) return loadTrip() })
onLoad((query) => { bookingId.value = typeof query?.id === 'string' ? query.id : ''; void loadTrip() })
onShow(() => { isPageActive = true; void startCustomerNotificationHub(); syncTripWatch() })
onHide(() => { isPageActive = false; stopWatching() })
onUnload(() => { stopWatching(); unsubscribeNotifications(); unsubscribeReconnect() })
function syncTripWatch(): void {
  if (!isPageActive || !trip.value || ['COMPLETED', 'CANCELLED'].includes(trip.value.summary.status)) { stopWatching(); return }
  startWatching(trip.value.summary.tripId)
}
async function handleCustomerNotification(message: NotificationMessageV1): Promise<void> {
  if (!isPageActive || !['DRIVER_ASSIGNED', 'TRIP_STATUS_CHANGED'].includes(message.data.eventType)) return
  if (message.data.notification.targetType !== 'TRIP' || message.data.notification.targetId !== bookingId.value) return
  await loadTrip()
}
async function loadTrip(): Promise<void> {
  if (!bookingId.value) return
  try {
    const response = await customerApi.getTrip(bookingId.value)
    trip.value = response.data
    syncTripWatch()
    pickupLocation.value = null
    dropoffLocation.value = null
    const locationResults = await Promise.allSettled([
      customerApi.getLocation(response.data.summary.route.pickupLocationId),
      customerApi.getLocation(response.data.summary.route.dropoffLocationId)
    ])
    if (locationResults[0].status === 'fulfilled') pickupLocation.value = locationResults[0].value.data
    if (locationResults[1].status === 'fulfilled') dropoffLocation.value = locationResults[1].value.data
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load trip.'), icon: 'none' })
  }
}
const booking = computed<Booking | undefined>(() => trip.value ? {
  id: trip.value.summary.tripId, serviceId: productCodeToServiceId(trip.value.summary.productCode), pickup: trip.value.summary.route.pickupName,
  destination: trip.value.summary.route.dropoffName, date: trip.value.summary.pickupAt.slice(0, 10), time: new Date(trip.value.summary.pickupAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
  passengers: trip.value.summary.passengers, note: trip.value.specialRequests ?? '', status: trip.value.summary.group === 'ACTIVE' ? 'confirmed' : trip.value.summary.group === 'COMPLETED' ? 'completed' : 'cancelled', createdAt: trip.value.summary.createdAt,
  reference: trip.value.summary.tripReference,
} : store.bookings.find((item) => item.id === bookingId.value))
const mapLocations = computed<[MapLocation, MapLocation]>(() => {
  if (pickupLocation.value?.coordinates && dropoffLocation.value?.coordinates) {
    return [pickupLocation.value.coordinates, dropoffLocation.value.coordinates]
  }
  return [booking.value?.pickup ?? '', booking.value?.destination ?? '']
})
const vehicle = computed(() => trip.value?.driver?.vehicleLabel || vehicleClassLabel(trip.value?.summary.vehicleClass ?? ''))
// 与司机端进行中行程的行程进度一致：按固定阶段展示，当前阶段高亮，之前的已完成。
const currentStageIndex = computed(() => trip.value ? customerTripStageIndex(trip.value.summary.stage) : -1)
const stageTimes = computed<ReadonlyArray<string | null>>(() => trip.value ? customerStageTimes(trip.value.timeline) : [])
function formatStageTime(value: string): string {
  return new Date(value).toLocaleString(localeTag(), { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}
const contactOptions: ContactOption[] = ['Call Driver', 'Contact Support', 'Chat with Operations']
function cancel(): void {
  if (!booking.value) return
  const id = booking.value.id
  confirm(t('Cancel Booking?'), t('The cancellation policy will be checked before submission.'), t('Cancel Booking'), t('Keep Booking'), () => cancelConfirmed(id))
}
async function cancelConfirmed(id: string): Promise<void> {
  try {
    const policy = await customerApi.getCancellationPolicy(id)
    if (!policy.data.canCancel) { uni.showToast({ title: policy.data.blockedReason ?? t('This trip cannot be cancelled.'), icon: 'none' }); return }
    await customerApi.cancelTrip(id)
    store.cancelBooking(id)
    goBack()
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    alert(t('Cancellation Failed'), error ? `${error.message}\nTrace ID: ${error.traceId}` : t('Unable to cancel booking.'), t('OK'))
  }
}
function goBack(): void { returnFromOrderDetails(bookingId.value) }
async function callContact(option: ContactOption): Promise<void> {
  if (option === 'Call Driver' && !isMockDriverPhoneEnabled && (!bookingId.value || !trip.value?.driver?.canContact)) {
    uni.showToast({ title: t('Driver contact is unavailable.'), icon: 'none' })
    return
  }

  try {
    const phoneNumber = await resolveContactPhoneNumber(option, bookingId.value, customerApi.getDriverContact)
    uni.makePhoneCall({
      phoneNumber,
      fail: () => uni.showToast({ title: t('Unable to open the phone dialer.'), icon: 'none' })
    })
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Contact phone number is unavailable.'), icon: 'none' })
  }
}
function viewDriver(): void {
  if (!bookingId.value) return
  uni.navigateTo({ url: '/pages/driver-map/index?id=' + encodeURIComponent(bookingId.value) })
}
</script>

<template>
  <view class="details-page">

    <view class="header"><button :aria-label='$t("Back")' @tap="goBack"><image src="/static/icons/chevron-left.svg" mode="aspectFit" /></button><text>{{ $t("Trip Details") }}</text></view>
    <scroll-view scroll-y class="scroll"><view v-if="booking" class="body">
      <view class="map-card">
        <view class="map-preview"><AmapRouteMap :api-key="amapWebKey" :security-js-code="amapSecurityJsCode" :origin="mapLocations[0]" :destination="mapLocations[1]" :pickup-name="booking.pickup" :dropoff-name="booking.destination" :driver-location="driverLocation" is-compact-preview is-interactive @metrics="mapMetrics = $event" /></view>
        <view class="map-mileage"><view><strong>{{ mapMetrics.coveredKm.toFixed(1) }}</strong><small>{{ $t("km Covered") }}</small></view><view><strong>{{ mapMetrics.hasCurrentPosition ? mapMetrics.distanceToPickupKm.toFixed(1) : '—' }}</strong><small>{{ $t("km to Pickup") }}</small></view><view><strong>{{ mapMetrics.hasCurrentPosition && mapMetrics.etaMinutes ? `${mapMetrics.etaMinutes} min` : '—' }}</strong><small>{{ $t('ETA to Pickup') }}</small></view></view>
        <button class="driver-button" @tap="viewDriver"><image src="/static/icons/map-vehicle.svg" mode="aspectFit" />{{ $t("View Driver") }}</button>
      </view>
      <text class="section-title">{{ $t("Order Summary") }}</text>
      <view class="facts">
        <view><text>{{ $t("Order No.") }}</text><text>{{ booking.reference || '—' }}</text></view>
        <view><text>{{ $t("Status") }}</text><text>{{ trip ? $t(customerTripStatusLabel(trip.summary.status)) : booking.status }}</text></view>
        <view><text>{{ $t("Pickup") }}</text><text>{{ booking.pickup }}</text></view>
        <view><text>{{ $t("Drop-Off") }}</text><text>{{ booking.destination }}</text></view>
        <template v-if="isOrderDetailsExpanded">
        <view><text>{{ $t("Service") }}</text><text>{{ findService(booking.serviceId).title.replace('\n', ' ') }}</text></view>
        <view><text>{{ $t("Vehicle") }}</text><text>{{ $t(vehicle) }}</text></view>
        <view><text>{{ $t("Driver") }}</text><text>{{ trip?.driver?.name || 'Awaiting assignment' }}</text></view>
        <view><text>{{ $t("Vehicle") }}</text><text>{{ trip?.driver?.vehicleLabel || 'To be assigned' }}</text></view>
        <view><text>{{ $t("Departure") }}</text><text>{{ booking.date }} · {{ booking.time }}</text></view>
        <view><text>{{ $t("Passengers") }}</text><text>{{ booking.passengers }}</text></view>
        </template>
      </view>
      <button class="facts-toggle" :aria-expanded="isOrderDetailsExpanded" @tap="isOrderDetailsExpanded = !isOrderDetailsExpanded">{{ $t(isOrderDetailsExpanded ? 'Hide Full Details' : 'Show Full Details') }} <image class="facts-toggle-icon" src="/static/icons/chevron-down.svg" mode="aspectFit" :class="{ 'is-expanded': isOrderDetailsExpanded }" aria-hidden="true" /></button>
      <template v-if="isOrderDetailsExpanded"><text class="section-title">{{ $t("Trip Notes") }}</text><text class="note">{{ booking.note || 'No Special Instructions' }}</text></template>
      <text class="section-title">{{ $t("Contact Options") }}</text><button v-for="option in contactOptions" :key="option" class="action" @click="callContact(option)">{{ $t(option) }}{{ option === 'Call Driver' && isMockDriverPhoneEnabled ? ` (${t('Demo')})` : '' }} <text>›</text></button>
      <text class="section-title">{{ $t("Status Tracker") }}</text><view v-if="trip && trip.summary.status !== 'CANCELLED'" class="trip-stages"><view v-for="(stage, index) in customerTripStages" :key="stage" class="trip-stage" :class="{ completed: index < currentStageIndex, current: index === currentStageIndex, pending: index > currentStageIndex }"><text class="trip-stage-number">{{ index < currentStageIndex ? '✓' : String(index + 1) }}</text><text class="trip-stage-label">{{ $t(stage) }}</text><text v-if="stageTimes[index]" class="trip-stage-time">{{ formatStageTime(stageTimes[index]!) }}</text></view></view><text v-else-if="trip" class="stage">●　{{ $t(customerTripStatusLabel(trip.summary.status)) }}</text>
      <button v-if="booking.status === 'confirmed'" class="cancel" @tap="cancel">{{ $t("Cancel Booking") }}</button>
    </view></scroll-view>
    <AppTabBar active="home" />
    <AppBottomSheet :open="dialog !== null" :title="dialog?.title ?? ''" :confirm-label="dialog?.confirmLabel ?? $t('OK')" :secondary-label="dialog?.secondaryLabel" @close="close" @confirm="submit"><text>{{ dialog?.message }}</text></AppBottomSheet>
  </view>
</template>

<style scoped lang="scss">
.details-page{min-height:100vh;background:#fff;color:#101820}.header{display:flex;height:78px;align-items:center;justify-content:center;border-bottom:1px solid #ebedf0;font-size:18px}.header button{position:absolute;left:16px;width:42px;height:44px;padding:0;background:transparent}.header button image{width:20px;height:20px}.scroll{height:calc(100vh - 134px)}.body{max-width:430px;margin:auto;padding:16px 16px 105px}.map-card{overflow:hidden;border:1px solid #e8ecee;border-radius:12px;background:#fff;box-shadow:0 1px 14px #00000005}.map-preview{position:relative;height:280px;overflow:hidden;background:#fafaf6}.map-mileage{display:flex;height:76px;align-items:center}.map-mileage view{display:flex;min-width:0;flex:1;align-items:center;flex-direction:column;gap:4px;border-right:1px solid #e6e6e6}.map-mileage view:last-child{border:0}.map-mileage strong{font-size:18px}.map-mileage small{font-size:9px}.driver-button{display:flex;width:100%;height:43px;padding:0 14px;align-items:center;justify-content:center;gap:6px;border-top:1px solid #f4f4f4;background:#fff;color:#eaa42b;font-size:14px}.driver-button image{width:20px;height:20px}.section-title{display:block;margin:18px 0 12px;font-size:18px}.facts>view{display:flex;min-height:30px;justify-content:space-between;gap:24px;font-size:14px;line-height:1.3}.facts>view text:first-child{min-width:100px;color:#939cad}.facts>view text:last-child{max-width:230px;text-align:right}.note{display:block;margin:18px 0;color:#222;font-size:14px}.action{display:flex;width:100%;min-height:51px;padding:0 12px;align-items:center;justify-content:space-between;border-bottom:1px solid #eee;background:#fff;text-align:left;font-size:14px}.action text{color:#a9b0bd}.stage{display:block;padding:9px 0;color:#eaa32b;font-size:14px}.trip-stages{display:flex;flex-direction:column}.trip-stage{display:flex;min-height:34px;align-items:center;gap:10px;color:#eaa32b;font-size:14px}.trip-stage-number{display:flex;width:24px;height:24px;flex:0 0 24px;align-items:center;justify-content:center;border-radius:4px;background:#f1ac2b;color:#fff;font-size:12px}.trip-stage-label{flex:1;line-height:1.3}.trip-stage-time{margin-left:auto;color:#9299a5;font-size:12px}.trip-stage.pending{color:#9299a5}.trip-stage.pending .trip-stage-number{background:#f1f1f3;color:#9299a5}.trip-stage.completed{color:#9299a5}.trip-stage.completed .trip-stage-number{background:#fdf3dd;color:#eaa32b}.trip-stage.current{font-weight:600}.cancel{width:100%;height:47px;margin-top:20px;border:1px solid #e9a42c;border-radius:8px;background:#fff;color:#eaa32b;font-size:14px}
.header button{display:flex;align-items:center;justify-content:center;line-height:normal}
.facts-toggle{display:flex;width:100%;height:42px;margin-top:8px;padding:0;align-items:center;justify-content:center;gap:7px;border:0;border-top:1px solid #eef0f3;background:#fff;color:#eaa42b;font-size:13px}.facts-toggle-icon{width:12px;height:12px;transition:transform .18s ease}.facts-toggle-icon.is-expanded{transform:rotate(180deg)}
</style>
