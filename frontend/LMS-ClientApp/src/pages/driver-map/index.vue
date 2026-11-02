<script setup lang="ts">
import { t } from '@/i18n/locale'
import { computed, ref } from 'vue'
import { onHide, onLoad, onShow, onUnload } from '@dcloudio/uni-app'

import AmapRouteMap from '@/components/AmapRouteMap.vue'
import CustomerNotificationCenter from '@/components/CustomerNotificationCenter.vue'
import { ApiRequestError } from '@/api/http'
import { customerApi, type CustomerLocation, type CustomerTrip } from '@/api/customer'
import type { MapLocation } from '@/data/amap'
import { returnFromDriverMap } from '@/data/orderDetailsRoute'
import { mapConfig } from '../../../../map.config'
import { startCustomerNotificationHub } from '@/api/notifications'
import { useCustomerTripLocation } from '@/composables/useCustomerTripLocation'

const tripId = ref('')
const trip = ref<CustomerTrip | null>(null)
const routeMap = ref<InstanceType<typeof AmapRouteMap> | null>(null)
const pickupLocation = ref<CustomerLocation | null>(null)
const dropoffLocation = ref<CustomerLocation | null>(null)
const amapWebKey = mapConfig.webKey
const amapSecurityJsCode = mapConfig.securityJsCode
const { driverLocation, startWatching, stopWatching } = useCustomerTripLocation()
let isPageActive = false

const mapLocations = computed<[MapLocation, MapLocation]>(() => {
  if (pickupLocation.value?.coordinates && dropoffLocation.value?.coordinates) {
    return [pickupLocation.value.coordinates, dropoffLocation.value.coordinates]
  }
  return [trip.value?.summary.route.pickupName ?? '', trip.value?.summary.route.dropoffName ?? '']
})

onLoad((query) => {
  tripId.value = typeof query?.id === 'string' ? query.id : ''
  void loadTrip()
})
onShow(() => { isPageActive = true; void startCustomerNotificationHub(); syncTripWatch() })
onHide(() => { isPageActive = false; stopWatching() })
onUnload(() => { stopWatching() })

function syncTripWatch(): void {
  if (!isPageActive || !trip.value || ['COMPLETED', 'CANCELLED'].includes(trip.value.summary.status)) { stopWatching(); return }
  startWatching(trip.value.summary.tripId)
}

async function loadTrip(): Promise<void> {
  if (!tripId.value) return
  try {
    const response = await customerApi.getTrip(tripId.value)
    trip.value = response.data
    syncTripWatch()
    const locationResults = await Promise.allSettled([
      customerApi.getLocation(response.data.summary.route.pickupLocationId),
      customerApi.getLocation(response.data.summary.route.dropoffLocationId)
    ])
    if (locationResults[0].status === 'fulfilled') pickupLocation.value = locationResults[0].value.data
    if (locationResults[1].status === 'fulfilled') dropoffLocation.value = locationResults[1].value.data
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load driver map.'), icon: 'none' })
  }
}

function goBack(): void { returnFromDriverMap(tripId.value) }
</script>

<template>
  <view class="driver-map-page">
    <view class="header"><button :aria-label='$t("Back")' @tap="goBack"><image src="/static/icons/chevron-left.svg" mode="aspectFit" /></button><text>{{ $t("Driver Map") }}</text></view>
    <CustomerNotificationCenter placement="floating" />
    <view class="map-area">
      <AmapRouteMap v-if="trip" ref="routeMap" :api-key="amapWebKey" :security-js-code="amapSecurityJsCode" :origin="mapLocations[0]" :destination="mapLocations[1]" :driver-location="driverLocation" :show-address-panel="false" is-interactive focus-driver-on-load />
      <view v-else class="loading">{{ $t("Loading trip map…") }}</view>
    </view>
    <view v-if="trip" class="driver-sheet">
      <view class="sheet-handle" />
      <view class="driver-heading">
        <view class="driver-icon"><image src="/static/icons/map-vehicle.svg" mode="aspectFit" /></view>
        <view><text class="driver-name">{{ trip.driver?.name || 'Awaiting driver assignment' }}</text><text class="vehicle">{{ trip.driver?.vehicleLabel || 'Vehicle to be assigned' }}</text></view>
        <text class="status">{{ trip.assignmentStatus === 'ASSIGNED' ? 'Assigned' : 'Pending' }}</text>
      </view>
      <view class="route" :aria-label='$t("Trip addresses")'>
        <button class="address-row" :disabled="!routeMap?.isReady" :aria-label='$t("Pickup")' @tap.stop="routeMap?.focusLocation('pickup')"><text class="address-dot pickup-dot">A</text><view class="address-text"><small>{{ $t("Pickup") }}</small><text>{{ routeMap?.locationLabels.pickup || trip.summary.route.pickupName }}</text></view></button>
        <button class="address-row" :disabled="!routeMap?.isReady" :aria-label='$t("Drop-off")' @tap.stop="routeMap?.focusLocation('dropoff')"><text class="address-dot dropoff-dot">B</text><view class="address-text"><small>{{ $t("Drop-off") }}</small><text>{{ routeMap?.locationLabels.dropoff || trip.summary.route.dropoffName }}</text></view></button>
        <button class="address-row" :disabled="!routeMap?.isReady || !driverLocation" :aria-label='$t("Driver location")' @tap.stop="routeMap?.focusLocation('driver')"><text class="address-dot current-dot">▲</text><view class="address-text"><small>{{ $t("Driver location") }}</small><text>{{ driverLocation ? routeMap?.locationLabels.driver : $t('Waiting for driver GPS') }}</text></view></button>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.driver-map-page{position:relative;min-height:100vh;overflow:hidden;background:#eef2f3;color:#101820}.header{position:relative;z-index:3;display:flex;height:78px;align-items:center;justify-content:center;border-bottom:1px solid #e8ecee;background:#fff;font-size:18px}.header button{position:absolute;left:16px;display:flex;width:42px;height:44px;padding:0;align-items:center;justify-content:center;background:transparent}.header button image{width:20px;height:20px}.map-area{height:calc(100vh - 78px)}.loading{display:flex;width:100%;height:100%;align-items:center;justify-content:center;color:#697386}.driver-sheet{position:absolute;right:16px;bottom:20px;left:16px;z-index:2;max-width:398px;margin:auto;padding:8px 16px 18px;border-radius:16px;background:#fff;box-shadow:0 8px 30px rgba(16,24,32,.16)}.sheet-handle{width:38px;height:4px;margin:0 auto 14px;border-radius:4px;background:#d8dde2}.driver-heading{display:flex;align-items:center;gap:11px}.driver-icon{display:flex;width:42px;height:42px;align-items:center;justify-content:center;border-radius:9px;background:#fff4de}.driver-icon image{width:23px;height:23px}.driver-heading>view:nth-child(2){display:flex;min-width:0;flex:1;gap:4px;flex-direction:column}.driver-name{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:15px}.vehicle{overflow:hidden;color:#697386;text-overflow:ellipsis;white-space:nowrap;font-size:12px}.status{padding:5px 8px;border-radius:12px;background:#fff4de;color:#e59b1f;font-size:11px}.route{display:flex;margin-top:16px;padding-top:14px;gap:7px;flex-direction:column;border-top:1px solid #eef0f2}.address-row{display:flex;width:100%;min-height:44px;margin:0;padding:0;align-items:center;justify-content:flex-start;gap:10px;border-radius:0;background:transparent;text-align:left;line-height:normal}.address-row::after{display:none}.address-dot{display:flex;width:20px;height:20px;flex:0 0 20px;align-items:center;justify-content:center;border-radius:50%;color:#fff;font-size:10px}.pickup-dot{background:#f1ae2e}.dropoff-dot{background:#101820}.current-dot{background:#1677ff}.address-text{display:flex;min-width:0;flex:1;align-items:flex-start;gap:3px;flex-direction:column}.address-text small{color:#9299a5;font-size:11px}.address-text text{max-width:100%;overflow:hidden;color:#101820;font-size:13px;line-height:1.4;text-overflow:ellipsis;white-space:nowrap}
</style>
