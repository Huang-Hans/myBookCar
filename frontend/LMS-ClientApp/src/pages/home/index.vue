<script setup lang="ts">

import { computed, ref } from 'vue'
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AppTabBar from '@/components/AppTabBar.vue'
import { resolveCitySelection } from '@/data/citySelection'
import { setCurrentCustomerTrip } from '@/data/customerNoticeStore'
import { consumeTabRefresh } from '@/data/tabNavigation'
import { ApiRequestError } from '@/api/http'
import { customerApi, productCodeToServiceId, type CustomerSummary, type ServiceOfferings } from '@/api/customer'
import { onCustomerNotificationsConnected, startCustomerNotificationHub, subscribeCustomerNotifications } from '@/api/notifications'
import type { NotificationMessageV1 } from '@/api/notificationContract'
import { t } from '@/i18n/locale'

const city = ref('Macau')
const offerings = ref<ServiceOfferings | null>(null)
const customer = ref<CustomerSummary | null>(null)
const isLoading = ref(false)
const isCitySheetOpen = ref(false)
const pendingCity = ref(city.value)
const fallbackServices = [
  { id: 'business-sedan', title: 'Standard Sedan', description: 'Standard sedan transfer within the same city.', image: '/static/figma/limousine.webp' },
  { id: 'limousine', title: 'Limousine', description: 'Private Rides in Macau', image: '/static/figma/limousine.webp' },
  { id: 'party', title: 'Party', description: 'Group and Party Transfers', image: '/static/figma/shuttle.webp' },
  { id: 'cross-border', title: 'Cross-Border Transfer', description: 'Hong Kong–Macau Rides', image: '/static/figma/mpv.webp' }
]
const cities = computed(() => offerings.value?.cities.map((item) => item.name) ?? ['Macau', 'Hong Kong'])
const services = computed(() => offerings.value?.products.map((item) => ({
  id: productCodeToServiceId(item.productCode), title: t(item.name), description: t(item.description),
  image: item.productCode === 'PARTY' ? '/static/figma/shuttle.webp' : item.productCode === 'CROSS_BORDER_TRANSFER' ? '/static/figma/mpv.webp' : '/static/figma/limousine.webp',
  isBookable: item.isBookable,
})) ?? fallbackServices.map((item) => ({ ...item, title: t(item.title), description: t(item.description), isBookable: true })))
let isPageActive = false
const unsubscribeNotifications = subscribeCustomerNotifications(handleCustomerNotification)
const unsubscribeReconnect = onCustomerNotificationsConnected(() => { if (isPageActive) return loadHome() })
onShow(() => {
  isPageActive = true
  void startCustomerNotificationHub()
  // Data loads when the Home bottom-navigation tab is tapped (or on the first open), not on every page show.
  if (consumeTabRefresh('home') || !customer.value) void loadHome()
})
onHide(() => {
  isPageActive = false
  isCitySheetOpen.value = false
})
onUnload(() => { unsubscribeNotifications(); unsubscribeReconnect() })
async function handleCustomerNotification(message: NotificationMessageV1): Promise<void> {
  if (!isPageActive || !['DRIVER_ASSIGNED', 'TRIP_STATUS_CHANGED'].includes(message.data.eventType)) return
  if (message.data.notification.targetType !== 'TRIP') return
  await refreshTripNotice()
}
// Trip updates arrive over SignalR; only the customer summary (current trip notice) needs refreshing,
// the service offerings are fetched again only on explicit tab navigation or hub reconnect recovery.
async function refreshTripNotice(): Promise<void> {
  try { customer.value = (await customerApi.getSelf()).data; setCurrentCustomerTrip(customer.value.currentTrip) }
  catch { /* A failed background refresh stays silent; the next tab tap reloads the page. */ }
}
async function loadHome(): Promise<void> {
  isLoading.value = true
  try {
    const [selfResponse, offeringResponse] = await Promise.all([customerApi.getSelf(), customerApi.getServiceOfferings()])
    customer.value = selfResponse.data
    setCurrentCustomerTrip(selfResponse.data.currentTrip)
    offerings.value = offeringResponse.data
    city.value = offeringResponse.data.cities[0]?.name ?? city.value
  } catch (reason) {
    const message = reason instanceof ApiRequestError ? `${reason.message} · ${reason.traceId}` : 'Unable to load services.'
    uni.showToast({ title: message, icon: 'none', duration: 3500 })
  } finally { isLoading.value = false }
}
function openCitySheet(): void {
  pendingCity.value = city.value
  isCitySheetOpen.value = true
}
function closeCitySheet(): void { isCitySheetOpen.value = false }
function selectCityOption(selectedCity: string): void {
  pendingCity.value = resolveCitySelection(cities.value, selectedCity, pendingCity.value)
}
function confirmCitySelection(): void {
  city.value = pendingCity.value
  closeCitySheet()
}
function selectService(id: string, isBookable: boolean): void {
  if (customer.value && !customer.value.canBook) { uni.showToast({ title: customer.value.bookingBlockReason ?? t('Your account cannot create a booking.'), icon: 'none' }); return }
  if (!isBookable) { uni.showToast({ title: t('This service is not currently bookable.'), icon: 'none' }); return }
  uni.navigateTo({ url: '/pages/service-info/index?id=' + id })
}
</script>

<template>
  <view class="client-home">

    <view class="header"><image src="/static/figma/melco-logo-black.png" mode="aspectFit" /></view>
    <scroll-view :scroll-y="!isCitySheetOpen" class="home-scroll">
      <view class="content">
        <text class="heading">{{ $t("Where Are You Now?") }}</text>
        <button
          class="city-row"
          aria-haspopup="dialog"
          :aria-expanded="isCitySheetOpen"
          @tap="openCitySheet"
        >
          <text>{{ $t("City") }}</text>
          <view class="city-value"><text>{{ city }}</text><text class="chevron" aria-hidden="true">›</text></view>
        </button>
        <text class="heading service-heading">{{ $t("Choose a Service") }}</text>
        <text class="subheading">{{ $t("Select the Right Ride for Your Journey") }}</text>
        <text v-if="isLoading" class="subheading">{{ $t("Loading available services…") }}</text>
        <button
          v-for="(service, index) in services"
          :key="service.id"
          class="service-card"
          :style="{ animationDelay: `${index * 180}ms` }"
          :disabled="!service.isBookable"
          @tap="selectService(service.id, service.isBookable)"
        >
          <view class="service-copy">
            <text class="service-title">{{ service.title }}</text>
            <text class="service-description">{{ service.description }}</text>
          </view>
          <view class="service-art"><image :src="service.image" mode="aspectFit" /></view>
        </button>
      </view>
    </scroll-view>
    <AppTabBar active="home" />
    <view v-if="isCitySheetOpen" class="city-sheet-backdrop">
      <button class="city-sheet-dismiss" :aria-label='$t("Close city selection")' @tap="closeCitySheet" />
      <view class="city-sheet" role="dialog" aria-modal="true" aria-labelledby="city-sheet-title">
        <view class="sheet-grip" aria-hidden="true"><view class="sheet-handle" /></view>
        <view class="sheet-toolbar">
          <text id="city-sheet-title" class="sheet-title">{{ $t("Select City") }}</text>
          <button class="sheet-close" :aria-label='$t("Close")' @tap="closeCitySheet">×</button>
        </view>
        <view class="sheet-options">
          <button
            v-for="option in cities"
            :key="option"
            class="sheet-option"
            :aria-pressed="pendingCity === option"
            @tap="selectCityOption(option)"
          >
            <text>{{ option }}</text>
            <text v-if="pendingCity === option" class="option-check" aria-hidden="true">✓</text>
          </button>
        </view>
        <button class="sheet-confirm" @tap="confirmCitySelection">{{ $t("Confirm") }}</button>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.client-home { min-height:100vh; background:#f8f8fa; color:#171717; }.header { display:flex; height:78px; align-items:center; justify-content:center; border-bottom:1px solid #ebebf0; background:#fbfbff; }.header image { width:178px; height:62px; }.home-scroll { height:calc(100vh - 157px); }.content { padding:22px 16px 75px; }.heading { display:block; font-size:19px; }.city-row { display:flex; width:100%; height:53px; margin-top:22px; padding:0 16px; align-items:center; justify-content:space-between; border:0; border-radius:12px; background:#fff; color:#171717; font-size:16px; font-weight:400; line-height:normal; text-align:left; }.city-row::after { display:none; }.city-value { display:flex; align-items:center; }.chevron { margin-left:7px; color:#9fa8b7; font-size:22px; }.service-heading { margin-top:23px; }.subheading { display:block; margin:5px 0 23px; color:#919ab0; font-size:15px; }.service-card { position:relative; display:flex; width:100%; height:110px; margin:0 0 24px; padding:16px; align-items:center; border-radius:12px; background:#fff; overflow:visible; text-align:left; }.service-copy { display:flex; min-width:0; flex:1; flex-direction:column; }.service-title { font-size:20px; line-height:22px; }.service-description { margin-top:6px; color:#919ab0; font-size:12px; line-height:16px; }.service-art { position:relative; isolation:isolate; width:42%; height:120px; flex:0 0 42%; transform:translateY(-22px) scale(1.08); }.service-art::before { position:absolute; z-index:-1; bottom:8px; left:3%; width:94%; height:40%; border-radius:50%; background:#f5f4f2; transform:rotate(-12deg); content:''; }.service-art image { width:100%; height:100%; mix-blend-mode:multiply; }
.service-card{justify-content:flex-start}
.service-card { animation:service-card-enter .48s cubic-bezier(.2,.75,.25,1) both; }
@keyframes service-card-enter { from { opacity:0; transform:translateY(28px); } to { opacity:1; transform:translateY(0); } }
.city-sheet-backdrop { position:fixed; z-index:100; inset:0; display:flex; align-items:flex-end; animation:sheet-dim .24s ease-out; }
.city-sheet-dismiss { position:absolute; inset:0; display:block; width:100%; height:100%; margin:0; padding:0; border:0; border-radius:0; background:#0005; backdrop-filter:blur(2px); }
.city-sheet-dismiss::after { display:none; }
.city-sheet { position:relative; z-index:1; width:100%; max-height:80vh; max-height:80dvh; padding:10px 12px calc(34px + env(safe-area-inset-bottom)); border-radius:20px 20px 0 0; background:#fbfbfd; box-sizing:border-box; color:#171717; overscroll-behavior:contain; animation:sheet-up .24s cubic-bezier(.2,.8,.2,1); }
.sheet-grip { padding:0 0 4px; }
.sheet-handle { width:36px; height:5px; margin:0 auto 8px; border-radius:99px; background:#c7c7cc; }
.sheet-toolbar { display:flex; min-height:44px; margin-bottom:20px; align-items:center; justify-content:space-between; gap:12px; border-bottom:1px solid #d1d1d6; }
.sheet-title { flex:1; font-size:17px; font-weight:500; line-height:22px; text-align:left; }
.sheet-close { display:flex; width:44px; height:44px; min-height:44px; padding:0; flex-shrink:0; align-items:center; justify-content:center; border:0; background:transparent; color:#8e8e93; font-size:28px; font-weight:300; line-height:44px; }
.sheet-close:focus-visible { outline:2px solid #a6741c; outline-offset:-2px; }
.sheet-close::after,.sheet-option::after,.sheet-confirm::after { display:none; }
.sheet-options { max-height:42vh; max-height:42dvh; margin:16px 0 0; overflow-y:auto; overscroll-behavior:contain; }
.sheet-option { display:flex; width:100%; min-height:54px; padding:14px 0; align-items:center; justify-content:space-between; gap:12px; border:0; border-radius:0; background:transparent; color:#171717; font-size:17px; font-weight:400; line-height:22px; text-align:left; }
.sheet-option + .sheet-option { border-top:1px solid #d1d1d6; }
.option-check { color:#a6741c; font-size:21px; font-weight:600; }
.sheet-confirm { display:flex; width:100%; height:50px; min-height:50px; margin-top:34px; padding:0 20px; align-items:center; justify-content:center; border:0; border-radius:25px; background:#f5ad27; color:#fff; font-size:17px; font-weight:400; line-height:22px; }
@keyframes sheet-up { from { transform:translateY(100%); } to { transform:translateY(0); } }
@keyframes sheet-dim { from { background:transparent; } to { background:#0005; } }
@media (prefers-reduced-motion:reduce) {
  .city-sheet-backdrop,.city-sheet { animation:none; }
  .service-card { animation:none; opacity:1; transform:none; }
}
</style>
