<script setup lang="ts">
import { t } from '@/i18n/locale'

import { computed, ref } from 'vue'
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AppTabBar from '@/components/AppTabBar.vue'
import { isTripBannerVisible } from '@/data/customerNoticeStore'
import AppBottomSheet from '@/components/AppBottomSheet.vue'
import { useClientDialog } from '@/composables/useClientDialog'
import { bookingEmptyTitle, type BookingFilter } from '@/data/bookingEmptyState'
import { consumeTabRefresh, markTabRefresh } from '@/data/tabNavigation'
import { orderDetailsRoute, reuseOrderDetailsPage } from '@/data/orderDetailsRoute'
import { vehicleClassLabel } from '@/data/ridePresentation'
import { findService } from '@/data/services'
import type { Booking } from '@/types/booking'
import { ApiRequestError } from '@/api/http'
import { customerApi, productCodeToServiceId, type TripGroup } from '@/api/customer'
import { onCustomerNotificationsConnected, startCustomerNotificationHub, subscribeCustomerNotifications } from '@/api/notifications'
import type { NotificationMessageV1 } from '@/api/notificationContract'
import { localeTag } from '@/i18n/locale'

const filter = ref<BookingFilter>('In Progress')
const filterOptions: readonly BookingFilter[] = ['In Progress', 'Completed', 'Cancelled']
const activeFilterIndex = computed(() => filterOptions.indexOf(filter.value))
const { dialog, alert, confirm, close, submit } = useClientDialog()
const remoteBookings = ref<Booking[]>([])
const bookings = computed(() => remoteBookings.value)
const group = computed<TripGroup>(() => filter.value === 'In Progress' ? 'ACTIVE' : filter.value === 'Completed' ? 'COMPLETED' : 'CANCELLED')
let isPageActive = false
let hasLoaded = false
const unsubscribeNotifications = subscribeCustomerNotifications(handleCustomerNotification)
const unsubscribeReconnect = onCustomerNotificationsConnected(() => { if (isPageActive) return loadBookings() })
onShow(() => {
  isPageActive = true
  void startCustomerNotificationHub()
  // Data loads when the Bookings bottom-navigation tab is tapped (or on the first open), not on every page show.
  if (consumeTabRefresh('bookings') || !hasLoaded) { hasLoaded = true; void loadBookings() }
})
onHide(() => { isPageActive = false })
onUnload(() => { unsubscribeNotifications(); unsubscribeReconnect() })
async function handleCustomerNotification(message: NotificationMessageV1): Promise<void> {
  if (!isPageActive || !['DRIVER_ASSIGNED', 'TRIP_STATUS_CHANGED'].includes(message.data.eventType)) return
  if (message.data.notification.targetType !== 'TRIP') return
  await loadBookings()
}
async function loadBookings(): Promise<void> {
  try {
    const response = await customerApi.listTrips(group.value)
    remoteBookings.value = response.data.map((trip) => ({
      id: trip.tripId, serviceId: productCodeToServiceId(trip.productCode), pickup: trip.route.pickupName,
      destination: trip.route.dropoffName, date: trip.pickupAt.slice(0, 10), time: new Date(trip.pickupAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      passengers: trip.passengers, note: '', status: trip.group === 'ACTIVE' ? 'confirmed' : trip.group === 'COMPLETED' ? 'completed' : 'cancelled', createdAt: trip.createdAt,
      reference: trip.tripReference, vehicleClass: trip.vehicleClass,
    }))
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load bookings.'), icon: 'none' })
  }
}
function selectFilter(value: typeof filter.value): void { filter.value = value; void loadBookings() }
function cancel(id: string): void {
  confirm(t('Cancel Booking?'), t('The cancellation policy will be checked before submission.'), t('Cancel Booking'), t('Keep Booking'), () => cancelConfirmed(id))
}
async function cancelConfirmed(id: string): Promise<void> {
  try {
    const policy = await customerApi.getCancellationPolicy(id)
    if (!policy.data.canCancel) { uni.showToast({ title: policy.data.blockedReason ?? t('This trip cannot be cancelled.'), icon: 'none' }); return }
    await customerApi.cancelTrip(id)
    await loadBookings()
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    alert(t('Cancellation Failed'), error ? `${error.message}\nTrace ID: ${error.traceId}` : t('Unable to cancel booking.'), t('OK'))
  }
}
function details(id: string): void {
  if (reuseOrderDetailsPage(id)) return
  uni.navigateTo({ url: orderDetailsRoute(id) })
}
function exploreServices(): void {
  markTabRefresh('home')
  uni.reLaunch({ url: '/pages/home/index' })
}
</script>

<template>
  <view class="bookings-page">
    <view class="header">{{ $t("Bookings") }}</view>
    <scroll-view scroll-y class="scroll" :class="{ 'with-trip-banner': isTripBannerVisible }">
      <view class="body">
        <view class="filters">
          <view class="filter-indicator" :style="{ transform: `translateX(${activeFilterIndex * 100}%)` }" aria-hidden="true" />
          <button v-for="item in filterOptions" :key="item" :class="{ active: filter === item }" :aria-pressed="filter === item" @tap="selectFilter(item)">{{ $t(item) }}</button>
        </view>
        <view v-if="!bookings.length" class="empty">
          <image class="empty-icon" src="/static/icons/empty-bookings.svg" mode="aspectFit" />
          <text class="empty-title">{{ $t(bookingEmptyTitle(filter)) }}</text>
          <text class="empty-description">{{ $t("Your Next Great Journey Is Just a Few Taps Away") }}</text>
          <button class="explore-button" @tap="exploreServices">{{ $t("Explore Services") }}</button>
        </view>
        <view
          v-for="(booking, index) in bookings"
          :key="booking.id"
          class="booking-card"
          :style="{ animationDelay: `${index * 100}ms` }"
          @tap="details(booking.id)"
        >
          <view class="card-top"><text class="service">{{ $t(findService(booking.serviceId).title.replace('\n', ' ')) }}</text><text class="badge">{{ $t(booking.status === 'confirmed' ? 'Confirmed' : booking.status) }}</text></view>
          <view class="fact order-ref"><text>{{ $t("Order No.") }}</text><text>{{ booking.reference || '—' }}</text></view>
          <text class="label">{{ $t("Vehicle") }}</text>
          <text class="vehicle">{{ $t(vehicleClassLabel(booking.vehicleClass ?? '—')) }}</text>
          <view class="divider" />
          <view class="fact"><text>{{ $t("Order Placed") }}</text><text>{{ new Date(booking.createdAt).toLocaleString(localeTag(), { dateStyle: 'medium', timeStyle: 'short' }) }}</text></view>
          <view class="fact"><text>{{ $t("Pickup") }}</text><text>{{ booking.pickup }}</text></view>
          <view class="fact"><text>{{ $t("Drop-Off") }}</text><text>{{ booking.destination }}</text></view>
          <button v-if="booking.status === 'confirmed'" class="cancel" @tap.stop="cancel(booking.id)">{{ $t("Cancel Booking") }}</button>
        </view>
      </view>
    </scroll-view>
    <AppTabBar active="bookings" />
    <AppBottomSheet :open="dialog !== null" :title="dialog?.title ?? ''" :confirm-label="dialog?.confirmLabel ?? $t('OK')" :secondary-label="dialog?.secondaryLabel" @close="close" @confirm="submit"><text>{{ dialog?.message }}</text></AppBottomSheet>
  </view>
</template>

<style scoped lang="scss">
.bookings-page { min-height:100vh; background:#f8f8fa; }.header { display:flex; height:78px; align-items:center; justify-content:center; background:#fff; font-size:18px; font-weight:600; }.scroll { height:calc(100vh - 134px); } .scroll.with-trip-banner { height:calc(100vh - 186px); }.body { padding:24px 16px 80px; }.filters { display:flex; padding:4px; border-radius:9px; background:#eee; }.filters button { flex:1; min-height:43px; border-radius:7px; color:#666; font-size:13px; }.filters button.active { background:#f1ad2f; color:#fff; }.booking-card { margin-top:20px; padding:19px 16px; border-radius:12px; background:#fff; box-shadow:0 1px 12px rgba(0,0,0,.025); }.card-top,.fact { display:flex; justify-content:space-between; gap:8px; }.service { font-size:18px; font-weight:600; }.badge { color:#e99a11; font-size:12px; }.label { display:block; margin:17px 0 6px; color:#9299ab; font-size:12px; }.vehicle { display:block; font-size:14px; }.divider { height:1px; margin:15px 0; background:#eee; }.fact { margin:10px 0; font-size:13px; }.fact text:first-child { color:#8990a0; }.fact text:last-child { max-width:65%; text-align:right; }.cancel { width:100%; height:45px; margin-top:13px; border:1px solid #efad2d; border-radius:8px; color:#e99b12; font-size:14px; }.empty { margin-top:40px; color:#9299ab; text-align:center; }
.bookings-page .header{background:#f8f8fa}.bookings-page .body{padding-top:48px}.filters{height:35px;padding:2px;background:#f1f1f3}.filters button{min-height:31px;color:#919bab;font-size:13px}.filters button.active{background:#fff;box-shadow:0 1px 4px #20202013;color:#f09c1c}.booking-card{margin-top:24px;padding:16px;box-shadow:none}.service{font-size:18px;font-weight:400}.badge{padding:3px 6px;border-radius:3px;background:#fff6e7}.label{float:left;width:33%;margin:14px 0 0;font-size:14px}.vehicle{min-height:34px;margin:14px 0 0;text-align:right;line-height:1.4}.divider{display:none}.fact{margin-top:14px;font-size:14px}.fact.order-ref{margin-top:9px;font-size:13px}.fact text:last-child{max-width:67%}.cancel{width:auto;height:37px;margin:14px 0 0 auto;padding:0 14px;border:0;border-radius:4px;background:#fcf1f1;color:#c66;font-size:13px}
.bookings-page .header{font-weight:400}.divider+.fact{margin-top:9px}.cancel{width:124px;margin-top:6px;padding-right:8px;padding-left:8px;white-space:nowrap}
.empty{display:flex;margin:0;padding:106px 0 0;align-items:center;flex-direction:column;text-align:center}
.empty-icon{display:block;width:36px;height:36px}
.empty-title{margin-top:18px;color:#171717;font-size:20px;line-height:25px}
.empty-description{margin-top:12px;color:#9299ab;font-size:13px;line-height:18px}
.explore-button{display:flex;width:100%;height:50px;min-height:50px;margin-top:45px;padding:0 20px;align-items:center;justify-content:center;border:0;border-radius:25px;background:#f1ad2f;color:#fff;font-size:17px;font-weight:400;line-height:22px}
.explore-button::after{display:none}
.filters{position:relative}
.filter-indicator{position:absolute;top:2px;bottom:2px;left:2px;width:calc((100% - 4px)/3);border-radius:7px;background:#fff;box-shadow:0 1px 4px #20202013;pointer-events:none;transition:transform .28s cubic-bezier(.2,.75,.25,1)}
.filters button,.filters button.active{position:relative;z-index:1;min-width:0;margin:0;padding:0;background:transparent;box-shadow:none}
.filters button::after{display:none}
.booking-card{animation:booking-card-enter .46s cubic-bezier(.2,.75,.25,1) both}
@keyframes booking-card-enter{from{opacity:0;transform:translateY(-24px)}to{opacity:1;transform:translateY(0)}}
@media(prefers-reduced-motion:reduce){.booking-card{animation:none;opacity:1;transform:none}.filter-indicator{transition:none}}
</style>
