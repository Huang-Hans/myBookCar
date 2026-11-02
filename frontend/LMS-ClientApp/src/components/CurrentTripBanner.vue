<script setup lang="ts">
import { currentCustomerTripId, currentCustomerTripLabel, dismissTripBanner, isTripBannerVisible } from '@/data/customerNoticeStore'
import { orderDetailsRoute, reuseOrderDetailsPage } from '@/data/orderDetailsRoute'

// 当前行程横幅：固定在底部菜单上方，首页、订单、账户共用。数据来自 customerNoticeStore，
// 由推送与页面刷新保持最新；行程完成或取消后横幅自动消失。右侧 × 可关闭，状态推进后重新出现。
function openCurrentTrip(): void {
  if (!currentCustomerTripId.value) return
  if (reuseOrderDetailsPage(currentCustomerTripId.value)) return
  uni.navigateTo({ url: orderDetailsRoute(currentCustomerTripId.value) })
}
</script>

<template>
  <view v-if="isTripBannerVisible" class="trip-banner">
    <button class="trip-banner-open" @tap="openCurrentTrip">
      <image class="trip-banner-icon" src="/static/icons/notification.svg" mode="aspectFit" />
      <text class="trip-banner-label">{{ currentCustomerTripLabel }}</text>
      <text class="trip-banner-chevron">›</text>
    </button>
    <button class="trip-banner-close" :aria-label="$t('Close')" @tap="dismissTripBanner">×</button>
  </view>
</template>

<style scoped lang="scss">
.trip-banner { position:fixed; right:0; bottom:calc(56px + env(safe-area-inset-bottom)); left:0; z-index:19; display:flex; height:52px; box-sizing:border-box; align-items:stretch; border-top:1px solid #f1cf91; border-left:3px solid #efa522; background:#fff4d9; box-shadow:0 -3px 14px #0000000d; }
.trip-banner button { margin:0; border:0; border-radius:0; background:transparent; line-height:normal; }
.trip-banner button::after { display:none; }
.trip-banner-open { display:flex; min-width:0; flex:1; padding:0 4px 0 17px; align-items:center; justify-content:flex-start; gap:10px; color:#171717; font-size:13px; font-weight:600; }
.trip-banner-icon { width:18px; height:18px; flex:none; }
.trip-banner-label { flex:1; overflow:hidden; text-align:left; text-overflow:ellipsis; white-space:nowrap; }
.trip-banner-chevron { color:#9fa8b7; font-size:22px; font-weight:400; }
.trip-banner-close { display:flex; width:44px; flex:none; padding:0; align-items:center; justify-content:center; color:#9fa8b7; font-size:22px; font-weight:400; }
</style>
