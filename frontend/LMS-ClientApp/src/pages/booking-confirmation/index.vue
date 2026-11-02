<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'

import AppTabBar from '@/components/AppTabBar.vue'
import { isTripBannerVisible } from '@/data/customerNoticeStore'
import { useBookingStore } from '@/stores/booking'
import { findService } from '@/data/services'
import { vehicleClassLabel, rideListIcon } from '@/data/ridePresentation'
import { orderDetailsRoute, reuseOrderDetailsPage } from '@/data/orderDetailsRoute'

const store = useBookingStore()
store.restore()
const bookingId = ref('')
onLoad((query) => {
  bookingId.value = typeof query?.id === 'string' ? query.id : ''
})
const booking = computed(() => store.bookings.find((item) => item.id === bookingId.value))
const vehicle = computed(() => vehicleClassLabel(booking.value?.vehicleClass ?? '—'))
const vehicleImage = computed(() => booking.value ? rideListIcon(booking.value.serviceId) : '')
function viewDetails(): void {
  if (!booking.value) return
  if (reuseOrderDetailsPage(booking.value.id)) return
  uni.navigateTo({ url: orderDetailsRoute(booking.value.id) })
}
function goBack(): void { uni.navigateBack() }
</script>

<template>
  <view class="confirmation-page">

    <view class="header"><button :aria-label='$t("Back")' @tap="goBack"><image src="/static/icons/chevron-left.svg" mode="aspectFit" /></button><text>{{ $t("Booking Confirmation") }}</text></view>
    <scroll-view scroll-y class="scroll" :class="{ 'with-trip-banner': isTripBannerVisible }"><view v-if="booking" class="body">
      <image class="vehicle" :src="vehicleImage" mode="aspectFit" />
      <view class="card">
        <text class="card-title">{{ $t("Your Ride Has Been Confirmed") }}</text>
        <view class="fact"><text>{{ $t("Status") }}</text><text>{{ $t("Confirmed") }}</text></view>
        <view class="fact"><text>{{ $t("Service") }}</text><text>{{ $t(findService(booking.serviceId).title.replace('\n', ' ')) }}</text></view>
        <view class="fact"><text>{{ $t("Vehicle") }}</text><text>{{ $t(vehicle) }}</text></view>
        <view class="fact"><text>{{ $t("Pickup") }}</text><text>{{ booking.pickup }}</text></view>
        <view class="fact"><text>{{ $t("Drop-Off") }}</text><text>{{ booking.destination }}</text></view>
        <view class="fact"><text>{{ $t("Departure") }}</text><text>{{ booking.date }} · {{ booking.time }}</text></view>
        <view class="fact"><text>{{ $t("Passengers") }}</text><text>{{ booking.passengers }}</text></view>
        <view class="fact"><text>{{ $t("Driver") }}</text><text>{{ $t("Details Shared Before Pickup") }}</text></view>
        <view class="fact"><text>{{ $t("Trip Notes") }}</text><text>{{ booking.note || 'No Special Instructions' }}</text></view>
      </view>
    </view></scroll-view>
    <view class="action-bar" :class="{ 'with-trip-banner': isTripBannerVisible }"><button @tap="viewDetails">{{ $t("View Trip Details") }}</button></view>
    <AppTabBar active="home" />
  </view>
</template>

<style scoped lang="scss">
.confirmation-page{min-height:100vh;background:#f8f8fa;color:#30333a}.header{display:flex;height:78px;align-items:center;justify-content:center;border-bottom:1px solid #ececf0;font-size:18px}.header button{position:absolute;left:16px;color:#a8afbd;font-size:31px}.scroll{height:calc(100vh - 134px)}.scroll.with-trip-banner{height:calc(100vh - 186px)}.body{max-width:430px;margin:auto;padding:0 16px 110px}.vehicle{display:block;width:230px;height:150px;margin:40px auto 38px}.card{padding:16px;border-radius:12px;background:#fff}.card-title{display:block;margin-bottom:16px;font-size:18px}.fact{display:flex;margin-top:12px;justify-content:space-between;gap:22px;font-size:14px;line-height:1.4}.fact text:first-child{min-width:85px;color:#949aa7}.fact text:last-child{max-width:210px;text-align:right}.action-bar{position:fixed;right:0;bottom:calc(56px + env(safe-area-inset-bottom));left:0;z-index:9;padding:10px 16px;background:#fff}.action-bar.with-trip-banner{bottom:calc(108px + env(safe-area-inset-bottom))}.action-bar button{height:51px;border-radius:27px;background:#f1ae30;color:#fff;font-size:16px;transition:transform .2s}.action-bar button:active{transform:scale(.98)}@media(prefers-reduced-motion:reduce){.action-bar button{transition:none}}
.header button{display:flex;width:42px;height:44px;padding:0;align-items:center;justify-content:center;background:transparent;line-height:normal}.vehicle{margin:20px auto 19px}.card-title{margin-bottom:11px}.fact{margin-top:11px}
.header button image{width:20px;height:20px}
</style>
