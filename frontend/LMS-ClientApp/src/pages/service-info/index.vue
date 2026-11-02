<script setup lang="ts">

import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import AppTabBar from '@/components/AppTabBar.vue'
import { customerApi, serviceIdToProductCode } from '@/api/customer'
import { isTripBannerVisible } from '@/data/customerNoticeStore'

const serviceId = ref('limousine')
onLoad((query) => { serviceId.value = typeof query?.id === 'string' ? query.id : 'limousine' })
onShow(() => { void loadService() })
const apiService = ref<{ title: string; description: string } | null>(null)
async function loadService(): Promise<void> {
  try {
    const response = await customerApi.getServiceOfferings()
    const item = response.data.products.find((product) => product.productCode === serviceIdToProductCode(serviceId.value))
    apiService.value = item ? { title: item.name, description: item.description } : null
  } catch { /* Keep the presentation fallback while the error is surfaced by authenticated screens. */ }
}
const service = computed(() => ({
  'business-sedan': { title: 'Standard Sedan', heading: 'Standard Sedan', body: 'Standard sedan transfer within the same city.', image: '/static/figma/limousine.webp' },
  limousine: { title: 'Limousine', heading: 'Complimentary Local Limousine', body: 'Enjoy a Thoughtfully Arranged Luxury Sedan Transfer between Melco Resorts and Your Destination', image: '/static/figma/limousine.webp' },
  party: { title: 'Party', heading: 'Group and Party Transfers', body: 'Travel Together Comfortably in a Luxury MPV', image: '/static/figma/shuttle.webp' },
  'cross-border': { title: 'Cross-Border Transfer', heading: 'Hong Kong–Macau Transfer', body: 'An Exclusive Transfer between Hong Kong and Macau', image: '/static/figma/mpv.webp' }
})[serviceId.value] || { title: 'Limousine', heading: 'Complimentary Local Limousine', body: 'Enjoy a Thoughtfully Arranged Luxury Sedan Transfer', image: '/static/figma/limousine.webp' })
const title = computed(() => apiService.value?.title ?? service.value.title)
const description = computed(() => apiService.value?.description ?? service.value.body)
function goBack(): void { uni.navigateBack() }
function bookNow(): void { uni.navigateTo({ url: '/pages/plan-ride/index?service=' + serviceId.value }) }
</script>

<template>
  <view class="service-page" :class="{ 'has-trip-banner': isTripBannerVisible }"><view class="top"><button :aria-label='$t("Back")' @tap="goBack"><image src="/static/icons/chevron-left.svg" mode="aspectFit" /></button><text>{{ $t(title) }}</text></view><scroll-view scroll-y class="scroll"><view class="body"><text class="heading">{{ $t(service.heading) }}</text><view class="vehicle-stage"><image class="vehicle" :src="service.image" mode="aspectFit" /></view><text class="available">{{ $t("Available for Booking") }}</text><text class="description">{{ $t(description) }}</text><text class="section-title">{{ $t("Before You Travel") }}</text><text class="description">{{ $t("Please Be Ready at Your Pickup Location at the Scheduled Time.") }}</text></view></scroll-view><view class="action-bar"><button class="book-button" @tap="bookNow">{{ $t("Book Now") }}</button></view><AppTabBar active="home" /></view>
</template>

<style scoped lang="scss">
.service-page {
  --trip-banner-height: 0px;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  height: 100vh;
  height: 100dvh;
  padding-bottom: calc(56px + env(safe-area-inset-bottom) + var(--trip-banner-height));
  overflow: hidden;
  background: #fff;
}
.service-page.has-trip-banner { --trip-banner-height: 52px; }
.top { display: flex; height: 78px; flex: none; align-items: center; justify-content: center; background: #fff; font-size: 18px; font-weight: 400; }
.top button { position: absolute; left: 17px; display: flex; width: 42px; height: 44px; padding: 0; align-items: center; justify-content: center; background: transparent; color: #a8afbd; line-height: normal; }
.top button image { width: 20px; height: 20px; }
.scroll { height: 0; min-height: 0; flex: 1 1 0; }
.body { padding: 17px 16px 24px; overflow: hidden; }
.heading { display: block; margin: 0; font-size: 21px; font-weight: 400; }
.vehicle-stage { height: 243px; margin: -16px -16px 6px; overflow: hidden; }
.vehicle { display: block; width: 480px; height: 230px; margin-left: 25px; background: transparent; animation: vehicle-enter .62s cubic-bezier(.2,.75,.25,1) both; }
.available { position: relative; display: block; padding-left: 15px; color: #659875; font-size: 13px; }
.available::before { position: absolute; top: 7px; left: 0; width: 7px; height: 7px; border-radius: 50%; background: #659875; content: ''; }
.description { display: block; margin-top: 17px; color: #929bad; font-size: 15px; line-height: 1.25; }
.section-title { display: block; margin-top: 16px; font-size: 17px; font-weight: 400; }
.action-bar { position: relative; z-index: 9; flex: none; padding: 11px 16px; background: #fff; }
.book-button { box-sizing: border-box; width: 100%; height: 51px; margin: 0; border-radius: 26px; background: #f1ad30; color: #fff; font-size: 16px; transition: transform .2s; }
.book-button:active { transform: scale(.98); }
@keyframes vehicle-enter { from { opacity: 0; transform: translateX(120px); } to { opacity: 1; transform: translateX(0); } }
@media (prefers-reduced-motion: reduce) { .vehicle { animation: none; opacity: 1; transform: none; } .book-button { transition: none; } }
</style>
