<script setup lang="ts">
import { t } from '@/i18n/locale'
import { computed, ref } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'

import AppTabBar from '@/components/AppTabBar.vue'
import { isTripBannerVisible } from '@/data/customerNoticeStore'
import AppBottomSheet from '@/components/AppBottomSheet.vue'
import { useClientDialog } from '@/composables/useClientDialog'
import { availableDepartureHours, availableDepartureMinutes, buildDepartureDateOptions, isFutureDeparture, nextSelectableDeparture } from '@/data/departurePicker'
import { defaultDropoffLocation } from '@/data/locationDefaults'
import { useBookingStore } from '@/stores/booking'
import type { ServiceId } from '@/types/booking'
import { locale } from '@/i18n/locale'
import { ApiRequestError, createIdempotencyKey } from '@/api/http'
import { customerApi, serviceIdToProductCode, type CreateTripRequest, type CustomerLocation, type LocationFavorites } from '@/api/customer'

const service = ref<ServiceId>('limousine')
const bookingStore = useBookingStore()
bookingStore.restore()
let lastPayload = ''
let idempotencyKey = ''
const createdBookingId = ref('')
const { dialog, alert, close, submit } = useClientDialog()
const pickupFavorites = ref<LocationFavorites | null>(null)
const dropoffFavorites = ref<LocationFavorites | null>(null)
const locations = ref<CustomerLocation[]>([])
const isAsapAvailable = ref(false)
const pickup = ref('')
const dropoff = ref('')
const pickupLocationId = ref('')
const dropoffLocationId = ref('')
const schedule = ref<'Now' | 'Schedule'>('Schedule')
const initialDeparture = nextSelectableDeparture(new Date())
const departureDates = ref(buildDepartureDateOptions(new Date(`${initialDeparture.date}T12:00:00`)))
const pickerNow = ref(new Date())
const date = ref(initialDeparture.date)
const time = ref(initialDeparture.time)
const isDeparturePickerOpen = ref(false)
const locationSheet = ref<'pickup' | 'dropoff' | null>(null)
const pendingLocation = ref('')
const draftDepartureSelection = ref<[number, number, number]>([0, 0, 0])
const hours = computed(() => availableDepartureHours(departureDates.value[draftDepartureSelection.value[0]]?.value ?? date.value, pickerNow.value))
const minutes = computed(() => availableDepartureMinutes(departureDates.value[draftDepartureSelection.value[0]]?.value ?? date.value, hours.value[draftDepartureSelection.value[1]] ?? '00', pickerNow.value))
const pickupFavorite = computed(() => pickupFavorites.value?.items.find((item) => item.isAvailable !== false) ?? null)
const dropoffFavorite = computed(() => dropoffFavorites.value?.items.find((item) => item.isAvailable !== false) ?? null)
const isCheckingRoute = ref(false)
const passengers = ref(1)
const notes = ref('')
const estimate = computed(() => service.value === 'cross-border' ? '75–90 Minutes' : '25–35 Minutes')
onLoad((query) => { service.value = ['business-sedan', 'limousine', 'party', 'cross-border'].includes(String(query?.service)) ? query!.service as ServiceId : 'limousine' })
const locationNames = computed(() => locations.value.map((item) => item.name))
onShow(() => { ensureFutureSelection(); void loadLocations() })
function ensureFutureSelection(): void {
  if (isFutureDeparture(date.value, time.value, new Date())) return
  const next = nextSelectableDeparture(new Date())
  date.value = next.date
  time.value = next.time
}
async function loadLocations(): Promise<void> {
  const crossBorderLocations = service.value === 'cross-border'
    ? customerApi.listLocations({ region: 'HONG_KONG', page: 1, pageSize: 50 })
    : Promise.resolve(null)
  const [locationResult, pickupResult, dropoffResult, offeringResult, crossBorderResult] = await Promise.allSettled([
    customerApi.listLocations({ page: 1, pageSize: 50 }), customerApi.getFavorites('PICKUP'),
    customerApi.getFavorites('DROPOFF'), customerApi.getServiceOfferings(), crossBorderLocations,
  ])
  pickupFavorites.value = pickupResult.status === 'fulfilled' ? pickupResult.value.data : null
  dropoffFavorites.value = dropoffResult.status === 'fulfilled' ? dropoffResult.value.data : null
  if (offeringResult.status === 'fulfilled') {
    isAsapAvailable.value = offeringResult.value.data.isAsapAvailable
    if (!isAsapAvailable.value) schedule.value = 'Schedule'
  }
  if (locationResult.status === 'fulfilled') {
    const allLocations = [...locationResult.value.data]
    if (crossBorderResult.status === 'fulfilled' && crossBorderResult.value) {
      for (const location of crossBorderResult.value.data) {
        if (!allLocations.some((item) => item.locationId === location.locationId)) allLocations.push(location)
      }
    }
    locations.value = allLocations
    const selectedPickup = locations.value.find((item) => item.locationId === pickupLocationId.value)
      ?? pickupFavorites.value?.items.find((item) => item.locationId === pickupLocationId.value && item.isAvailable !== false)
      ?? locations.value[0]
    pickup.value = selectedPickup?.name ?? ''
    pickupLocationId.value = selectedPickup?.locationId ?? ''
    const selectedDropoff = locations.value.find((item) => item.locationId === dropoffLocationId.value)
      ?? dropoffFavorites.value?.items.find((item) => item.locationId === dropoffLocationId.value && item.isAvailable !== false)
      ?? defaultDropoffLocation(locations.value, pickupLocationId.value, service.value)
    dropoff.value = selectedDropoff?.name ?? ''
    dropoffLocationId.value = selectedDropoff?.locationId ?? ''
  } else {
    const error = locationResult.reason instanceof ApiRequestError ? locationResult.reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load locations.'), icon: 'none' })
  }
}
function useFavoritePickup(): void {
  const item = pickupFavorite.value
  if (!item) return
  pickup.value = item.name
  pickupLocationId.value = item.locationId
}
function useFavoriteDropoff(): void {
  const item = dropoffFavorite.value
  if (!item) return
  dropoff.value = item.name
  dropoffLocationId.value = item.locationId
}
function openLocationSheet(usage: 'pickup' | 'dropoff'): void {
  locationSheet.value = usage
  pendingLocation.value = usage === 'pickup' ? pickup.value : dropoff.value
}
function confirmLocation(): void {
  const item = locations.value.find((location) => location.name === pendingLocation.value)
  if (!item) return
  if (locationSheet.value === 'pickup') { pickup.value = item.name; pickupLocationId.value = item.locationId }
  else if (locationSheet.value === 'dropoff') { dropoff.value = item.name; dropoffLocationId.value = item.locationId }
  locationSheet.value = null
}
function openDeparturePicker(): void {
  ensureFutureSelection()
  pickerNow.value = new Date()
  const earliest = nextSelectableDeparture(pickerNow.value)
  departureDates.value = buildDepartureDateOptions(new Date(`${earliest.date}T12:00:00`))
  const dateIndex = Math.max(0, departureDates.value.findIndex((option) => option.value === date.value))
  const selectableHours = availableDepartureHours(date.value, pickerNow.value)
  const hourIndex = Math.max(0, selectableHours.indexOf(time.value.slice(0, 2)))
  const selectableMinutes = availableDepartureMinutes(date.value, selectableHours[hourIndex] ?? '00', pickerNow.value)
  const minuteIndex = Math.max(0, selectableMinutes.indexOf(time.value.slice(3, 5)))
  draftDepartureSelection.value = [dateIndex, hourIndex, minuteIndex]
  isDeparturePickerOpen.value = true
}
function changeDepartureSelection(event: { detail: { value: number[] } }): void {
  const [dateIndex = 0, hourIndex = 0, minuteIndex = 0] = event.detail.value
  const [previousDate, previousHour] = draftDepartureSelection.value
  draftDepartureSelection.value = [dateIndex, dateIndex === previousDate ? hourIndex : 0,
    dateIndex !== previousDate || hourIndex !== previousHour ? 0 : minuteIndex]
}
function confirmDeparture(): void {
  const [dateIndex, hourIndex, minuteIndex] = draftDepartureSelection.value
  const chosenDate = departureDates.value[dateIndex]?.value ?? ''
  const chosenTime = `${hours.value[hourIndex] ?? ''}:${minutes.value[minuteIndex] ?? ''}`
  if (!isFutureDeparture(chosenDate, chosenTime, new Date())) {
    openDeparturePicker()
    uni.showToast({ title: t('Choose a future departure time.'), icon: 'none' })
    return
  }
  date.value = chosenDate
  time.value = chosenTime
  isDeparturePickerOpen.value = false
}
async function next(): Promise<void> {
  if (isCheckingRoute.value) return
  if (createdBookingId.value) { openConfirmation(); return }
  if (!validateBookingInput()) return
  isCheckingRoute.value = true
  try {
    const response = await customerApi.getRideOptions({
      productCode: serviceIdToProductCode(service.value), pickupLocationId: pickupLocationId.value,
      dropoffLocationId: dropoffLocationId.value, passengers: passengers.value,
    })
    if (!response.data.vehicles.some((vehicle) => vehicle.isEligible)) {
      alert(t('Route unavailable'), t('No eligible vehicle is available.'), t('OK'))
      return
    }
    await createBooking()
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    alert(t('Booking Not Created'), error ? `${error.message}\nTrace ID: ${error.traceId}` : t('Unable to create the booking.'), t('OK'))
  } finally { isCheckingRoute.value = false }
}
function validateBookingInput(): boolean {
  if (!pickup.value.trim() || !dropoff.value.trim()) {
    uni.showToast({ title: t('Select pickup and drop-off locations'), icon: 'none' })
    return false
  }
  if (!pickupLocationId.value || !dropoffLocationId.value) {
    uni.showToast({ title: t('Choose locations from the available location list.'), icon: 'none' })
    return false
  }
  if (schedule.value === 'Schedule' && !isFutureDeparture(date.value, time.value, new Date())) {
    ensureFutureSelection()
    alert(t('Invalid departure time'), t('Choose a future departure time.'), t('OK'))
    return false
  }
  return true
}
function openConfirmation(): void {
  uni.redirectTo({ url: '/pages/booking-confirmation/index?id=' + encodeURIComponent(createdBookingId.value) })
}
function bookingRequest(): CreateTripRequest {
  return {
    productCode: serviceIdToProductCode(service.value), pickupLocationId: pickupLocationId.value,
    dropoffLocationId: dropoffLocationId.value, pickupMode: schedule.value === 'Now' ? 'ASAP' as const : 'SCHEDULED' as const,
    pickupAt: schedule.value === 'Now' ? undefined : new Date(date.value + 'T' + time.value + ':00+08:00').toISOString(),
    passengers: passengers.value, luggage: 0, specialRequests: notes.value.trim() || undefined, confirmationLanguage: locale.value,
  }
}
async function createBooking(): Promise<void> {
  const request = bookingRequest()
  const payload = JSON.stringify(request)
  if (payload !== lastPayload) { lastPayload = payload; idempotencyKey = createIdempotencyKey('trip') }
  const { data } = await customerApi.createTrip(request, idempotencyKey)
  const pickupAt = new Date(data.summary.pickupAt)
  const localPickup = new Date(pickupAt.getTime() + 8 * 60 * 60_000).toISOString()
  const booking = bookingStore.createBooking({
    serviceId: service.value, pickup: data.summary.route.pickupName, destination: data.summary.route.dropoffName,
    date: localPickup.slice(0, 10), time: localPickup.slice(11, 16), passengers: data.summary.passengers,
    note: notes.value, vehicleClass: data.summary.vehicleClass,
  }, data.summary.tripId, data.summary.tripReference)
  createdBookingId.value = booking.id
  openConfirmation()
}
function goBack(): void { uni.navigateBack() }
</script>

<template>
  <view class="plan-page">

    <view class="top"><button :aria-label='$t("Back")' @tap="goBack"><image src="/static/icons/chevron-left.svg" mode="aspectFit" /></button><text>{{ $t("Plan Your Ride") }}</text></view>
    <scroll-view scroll-y class="scroll" :class="{ 'with-trip-banner': isTripBannerVisible }"><view class="body">
      <text class="subtitle">{{ $t("A Few Details, and We’ll Take Care of the Rest") }}</text>
      <view class="locations-card">
        <view class="location-row"><view><text class="muted">{{ $t("Pickup Location") }}</text><button class="location-value" :aria-label='$t("Choose pickup location")' @tap="openLocationSheet('pickup')">{{ pickup || $t("Choose pickup location") }}</button><button v-if="pickupFavorite" class="location-chip" :aria-label="`Use favorite pickup location ${pickupFavorite.name}`" @tap="useFavoritePickup">{{ pickupFavorite.name }}</button></view><button class="chevron" :aria-label='$t("Choose pickup location")' @tap="openLocationSheet('pickup')">›</button></view>
        <view class="location-row"><view><text class="muted">{{ $t("Drop-Off Location") }}</text><button class="location-value" :aria-label='$t("Choose drop-off location")' @tap="openLocationSheet('dropoff')">{{ dropoff || $t("Choose drop-off location") }}</button><button v-if="dropoffFavorite" class="location-chip" :aria-label="`Use favorite drop-off location ${dropoffFavorite.name}`" @tap="useFavoriteDropoff">{{ dropoffFavorite.name }}</button></view><button class="chevron" :aria-label='$t("Choose drop-off location")' @tap="openLocationSheet('dropoff')">›</button></view>
      </view>
      <text class="heading">{{ $t("When Are You Travelling?") }}</text>
      <view class="segment"><button :class="{ active: schedule === 'Now' }" :disabled="!isAsapAvailable" @tap="schedule = 'Now'">{{ $t("Now") }}</button><button :class="{ active: schedule === 'Schedule' }" @tap="schedule = 'Schedule'">{{ $t("Schedule") }}</button></view>
      <view v-if="schedule === 'Schedule'" class="schedule-card">
        <button class="schedule-row" @tap="openDeparturePicker"><text>{{ $t("Departure Date") }}</text><text>{{ date }} <text class="chevron">›</text></text></button>
        <button class="schedule-row" @tap="openDeparturePicker"><text>{{ $t("Departure Time") }}</text><text>{{ time }} <text class="chevron">›</text></text></button>
      </view>
      <view class="passenger-card"><view><text>{{ $t("Passengers") }}</text><text class="muted">{{ $t("Including Yourself") }}</text></view><view class="counter"><button :aria-label='$t("Remove passenger")' @tap="passengers = Math.max(1, passengers - 1)">−</button><text>{{ passengers }}</text><button :aria-label='$t("Add passenger")' @tap="passengers = Math.min(8, passengers + 1)">+</button></view></view>
      <view class="notes-card"><text>{{ $t("Trip Notes") }}</text><textarea v-model="notes" :placeholder='$t("Add Pickup Instructions or a Special Request")' /></view>
      <view class="estimate"><text>{{ $t("Estimated Journey") }}</text><text>{{ estimate }}</text></view>
    </view></scroll-view>
    <view class="action-bar" :class="{ 'with-trip-banner': isTripBannerVisible }"><button :disabled="isCheckingRoute" @tap="next">{{ $t("Confirm Booking") }}</button></view>
    <AppTabBar active="home" />
    <AppBottomSheet :open="locationSheet !== null" :title="$t(locationSheet === 'pickup' ? 'Pickup Location' : 'Drop-off Location')" :options="locationNames" :selected="pendingLocation" :search-placeholder="$t('Search locations')" :confirm-label="$t('Confirm')" :is-confirm-disabled="!locationNames.includes(pendingLocation)" @select="pendingLocation = $event" @close="locationSheet = null" @confirm="confirmLocation" />
    <AppBottomSheet :open="dialog !== null" :title="dialog?.title ?? ''" :confirm-label="dialog?.confirmLabel ?? $t('OK')" :secondary-label="dialog?.secondaryLabel" @close="close" @confirm="submit"><text>{{ dialog?.message }}</text></AppBottomSheet>
    <view v-if="isDeparturePickerOpen" class="departure-backdrop" @tap.self="isDeparturePickerOpen = false">
      <view class="departure-sheet" role="dialog" aria-modal="true" :aria-label='$t("Choose departure date and time")'>
        <view class="departure-grabber" />
        <view class="departure-header"><text>{{ $t("Departure") }}</text><button :aria-label='$t("Close")' @tap="isDeparturePickerOpen = false">×</button></view>
        <view class="departure-headings"><text>{{ $t("Date") }}</text><text>{{ $t("Hour") }}</text><text>{{ $t("Minute") }}</text></view>
        <view class="departure-picker-wrap">
          <picker-view class="departure-picker" :value="draftDepartureSelection" indicator-style="height:44px" mask-style="background:linear-gradient(to bottom,#fbfbfd 0%,rgba(251,251,253,0) 38%,rgba(251,251,253,0) 62%,#fbfbfd 100%)" @change="changeDepartureSelection">
            <picker-view-column class="departure-date-column"><view v-for="(option, index) in departureDates" :key="option.value" class="departure-option" :class="{ 'is-selected': draftDepartureSelection[0] === index }">{{ option.label }}</view></picker-view-column>
            <picker-view-column><view v-for="(hour, index) in hours" :key="hour" class="departure-option" :class="{ 'is-selected': draftDepartureSelection[1] === index }">{{ hour }}</view></picker-view-column>
            <picker-view-column><view v-for="(minute, index) in minutes" :key="minute" class="departure-option" :class="{ 'is-selected': draftDepartureSelection[2] === index }">{{ minute }}</view></picker-view-column>
          </picker-view>
          <view class="departure-selection-lines" aria-hidden="true"><view /><view /><view /></view>
        </view>
        <text class="departure-hint">{{ $t("Scroll to Choose Your Departure Times Are Local") }}</text>
        <button class="departure-confirm" @tap="confirmDeparture">{{ $t("Confirm") }}</button>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.plan-page{min-height:100vh;background:#fff;color:#101820}.top{display:flex;height:78px;align-items:center;justify-content:center;border-bottom:1px solid #ebedf0;font-size:18px}.top button{position:absolute;left:16px;width:42px;height:44px;padding:0;background:transparent;color:#a7afbe;font-size:31px}.scroll{height:calc(100vh - 134px)}.scroll.with-trip-banner{height:calc(100vh - 186px)}.body{max-width:430px;margin:auto;padding:16px 16px 110px}.subtitle{display:block;margin-bottom:16px;color:#919bad;font-size:14px}.locations-card,.schedule-card,.passenger-card,.notes-card{border-radius:13px;background:#fff;box-shadow:0 1px 9px #141b2710}.locations-card{padding:0 16px}.location-row{display:flex;min-height:116px;align-items:center;justify-content:space-between}.location-row+.location-row{border-top:1px solid #eaedf1}.location-row>view{display:flex;flex-direction:column;gap:5px}.location-row input{width:290px;height:26px;font-size:16px}.muted{color:#929bad;font-size:13px}.location-chip{align-self:flex-start;margin-top:9px;padding:6px 8px;border:1px solid #f4dbb8;border-radius:3px;color:#e99a23;font-size:12px}.chevron{color:#aeb7c5;font-size:24px}.heading{display:block;margin:12px 0;font-size:20px}.segment{display:flex;padding:4px;border-radius:9px;background:#f1f1f3}.segment button{flex:1;height:36px;color:#949dad;font-size:14px}.segment button.active{border-radius:8px;background:#fff;box-shadow:0 1px 4px #141b2714;color:#f0a225}.schedule-card{margin-top:16px;padding:0 16px}.schedule-row{display:flex;height:64px;align-items:center;justify-content:space-between;font-size:16px}.schedule-card picker+picker .schedule-row{border-top:1px solid #e9ebef}.schedule-row>text:last-child{font-size:14px}.schedule-row .chevron{margin-left:11px}.passenger-card{display:flex;min-height:78px;margin-top:16px;padding:0 16px;align-items:center;justify-content:space-between}.passenger-card>view:first-child{display:flex;flex-direction:column;gap:4px;font-size:16px}.counter{display:flex;height:37px;align-items:center;border:1px solid #e9eaf0;border-radius:8px}.counter button{width:42px;height:35px;color:#9da5b1;font-size:22px}.counter button:last-child{color:#1e2630}.counter text{min-width:36px;border-right:1px solid #eee;border-left:1px solid #eee;text-align:center}.notes-card{margin-top:16px;padding:16px;font-size:16px}.notes-card textarea{height:70px;margin-top:8px;font-size:14px}.estimate{display:flex;margin:20px 0;justify-content:space-between;color:#939baa;font-size:13px}.action-bar{position:fixed;right:0;bottom:calc(56px + env(safe-area-inset-bottom));left:0;z-index:9;padding:11px 16px;background:#fff}.action-bar.with-trip-banner{bottom:calc(108px + env(safe-area-inset-bottom))}.action-bar button{height:51px;border-radius:26px;background:#f1ad30;color:#fff;font-size:16px;transition:transform .2s}.action-bar button:active{transform:scale(.98)}@media(prefers-reduced-motion:reduce){.action-bar button{transition:none}}
.top button{display:flex;align-items:center;justify-content:center;line-height:normal}.top button image{width:20px;height:20px}
.location-chip{align-self:flex-start;width:auto;min-height:28px;margin:9px 0 0!important;justify-content:flex-start;line-height:16px!important;text-align:left}.location-chip:active{background:#fff7e9}
.schedule-card .schedule-row{width:100%;margin:0;padding:0;justify-content:space-between;background:transparent;text-align:left}.schedule-card .schedule-row+.schedule-row{border-top:1px solid #e9ebef}
.departure-backdrop{position:fixed;z-index:100;inset:0;display:flex;align-items:flex-end;justify-content:center;background:#0006;backdrop-filter:blur(2px);animation:departure-fade-in .2s ease-out}
.departure-sheet{width:100%;height:518px;max-height:calc(100vh - 44px);padding:8px 16px calc(16px + env(safe-area-inset-bottom));border-radius:16px 16px 0 0;background:#fbfbfd;box-sizing:border-box;animation:departure-slide-up .24s ease-out}
.departure-grabber{width:36px;height:4px;margin:0 auto 14px;border-radius:4px;background:#d4d7dd}
.departure-header{display:flex;height:43px;align-items:flex-start;justify-content:space-between;border-bottom:1px solid #ececf0;font-size:17px}.departure-header button{width:44px;height:38px;margin:-9px -10px 0 0;background:transparent!important;box-shadow:none;color:#9ca1aa;font-size:29px;font-weight:200}
.departure-headings{display:grid;margin-top:30px;grid-template-columns:2fr 1fr 1fr;color:#9aa0ab;text-align:center;font-size:12px}.departure-picker-wrap{position:relative}.departure-picker{width:100%;height:220px}.departure-date-column{flex:2}.departure-option{display:flex;height:44px;align-items:center;justify-content:center;color:#a5a8b1;font-size:16px}.departure-selection-lines{position:absolute;top:88px;right:0;left:0;display:grid;height:44px;grid-template-columns:2fr 1fr 1fr;gap:12px;pointer-events:none}.departure-selection-lines view{border-top:1px solid #ececf0;border-bottom:1px solid #ececf0}.departure-hint{display:block;margin-top:31px;color:#9297a1;text-align:left;font-size:12px}.departure-confirm{width:100%;height:49px;margin-top:33px;border-radius:25px;background:#f2ae2c;color:#fff;font-size:17px;font-weight:600}
@keyframes departure-fade-in{from{opacity:0}to{opacity:1}}@keyframes departure-slide-up{from{transform:translateY(100%)}to{transform:translateY(0)}}@media(prefers-reduced-motion:reduce){.departure-backdrop,.departure-sheet{animation:none}}
.location-row>view{min-width:0;flex:1}.location-value{display:flex;box-sizing:border-box;width:100%;height:26px;margin:0;padding:0;align-items:center;justify-content:flex-start;border:0;border-radius:0;background:transparent;color:#101820;font-size:16px;line-height:26px;overflow:hidden;text-align:left;text-overflow:ellipsis;white-space:nowrap}.location-value::after{display:none}.location-row>.chevron{flex:none;margin-left:12px}
.departure-option.is-selected{color:#182333;font-size:17px;font-weight:700}.departure-selection-lines view{border-top:2px solid #e8a52a;border-bottom:2px solid #e8a52a;border-radius:4px;background:#f2ae2c0d}
</style>
