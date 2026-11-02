<script setup lang="ts">
import { locale, t } from '@/i18n'
import { computed, ref, watch } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import BookingDeparturePicker from '@/components/BookingDeparturePicker.vue'
import BookingOptionSheet from '@/components/BookingOptionSheet.vue'
import SegmentedChoice from '@/components/SegmentedChoice.vue'
import { loadCatalogOptions, type CatalogOption } from '@/api/catalogOptions'
import { isFutureMacauDeparture, nextMacauDeparture } from '@/api/bookingDeparture'
import { displayError, idempotencyKey, requireSession } from '@/api/http'
import { staffApi, type BookingInput, type BookingOptions, type BookingPreview, type BookingProductCode, type Guest, type Location, type Property } from '@/api/staff'
import { toUtc } from '@/api/presentation'

const properties = ref<Property[]>([])
const locations = ref<Location[]>([])
const guests = ref<Guest[]>([])
const guestSearch = ref('')
const selectedGuest = ref<Guest | null>(null)
const propertyId = ref('')
const pickupLocationId = ref('')
const dropoffLocationId = ref('')
const initialDeparture = nextMacauDeparture(new Date())
const date = ref(initialDeparture.date)
const time = ref(initialDeparture.time)
const isDeparturePickerOpen = ref(false)
type OptionSheetKind = 'property' | 'pickup' | 'dropoff'
const optionSheetKind = ref<OptionSheetKind | null>(null)
const selectedProductCode = ref<BookingProductCode | ''>('')
const bookingOptions = ref<BookingOptions | null>(null)
const passengers = ref('1')
const preview = ref<BookingPreview | null>(null)
const isConfirmationOpen = ref(false)
const error = ref('')
const propertyError = ref('')
const locationError = ref('')
const vehicleError = ref('')
const optionsLoading = ref(false)
const busy = ref(false)
const submissionKey = ref(idempotencyKey('staffTrip'))
const localizedProductNames: Record<string, string> = {
  'Standard Sedan': '普通轎車', 'Luxury Sedan': '豪華轎車', 'Luxury MPV': '豪華 MPV',
  'Limousine': '禮賓車', 'Maybach': '邁巴赫', 'Party': '聚會用車', 'Cross-Border Transfer': '跨境接送',
}
const propertyOptions = computed<CatalogOption[]>(() => properties.value.map((item) => ({ id: item.propertyId, label: item.name })))
const locationOptions = computed<CatalogOption[]>(() => locations.value.map((item) => ({ id: item.locationId, label: item.name, detail: `${item.region} · ${item.address}` })))
const vehicleOptions = computed(() => bookingOptions.value?.products.map((item) => {
  const name = item.productCode === 'LIMOUSINE' ? 'Luxury Sedan' : item.name
  return { value: item.productCode, label: locale.value === 'en' ? name : localizedProductNames[name] ?? name, disabled: !item.isBookable }
}) ?? [])
const selectedProduct = computed(() => bookingOptions.value?.products.find((item) => item.productCode === selectedProductCode.value) ?? null)
const optionSheetOptions = computed(() => optionSheetKind.value === 'property' ? propertyOptions.value : locationOptions.value)
const optionSheetTitle = computed(() => optionSheetKind.value === 'property' ? '選擇物業' : optionSheetKind.value === 'pickup' ? '選擇上車地點' : '選擇目的地')
const optionSheetSelectedId = computed(() => optionSheetKind.value === 'property' ? propertyId.value : optionSheetKind.value === 'pickup' ? pickupLocationId.value : dropoffLocationId.value)
watch([selectedGuest, propertyId, pickupLocationId, dropoffLocationId, date, time, selectedProductCode, passengers], () => {
  preview.value = null
  submissionKey.value = idempotencyKey('staffTrip')
})

onShow(() => {
  if (!isFutureMacauDeparture(date.value, time.value, new Date())) {
    const next = nextMacauDeparture(new Date())
    date.value = next.date
    time.value = next.time
  }
  if (requireSession()) void loadOptions()
})

async function loadOptions(): Promise<void> {
  if (optionsLoading.value) return
  optionsLoading.value = true
  propertyError.value = ''
  locationError.value = ''
  vehicleError.value = ''
  const [propertyResult, locationResult, bookingOptionsResult] = await Promise.allSettled([
    loadCatalogOptions(staffApi.properties), loadCatalogOptions(staffApi.locations), staffApi.bookingOptions(),
  ])
  if (propertyResult.status === 'fulfilled') {
    properties.value = propertyResult.value
    if (!properties.value.some((item) => item.propertyId === propertyId.value)) {
      propertyId.value = properties.value.length === 1 ? properties.value[0]!.propertyId : ''
    }
    if (!properties.value.length) propertyError.value = t('暫無可選物業')
  } else {
    propertyError.value = displayError(propertyResult.reason)
  }
  if (locationResult.status === 'fulfilled') {
    locations.value = locationResult.value
    if (!locations.value.some((item) => item.locationId === pickupLocationId.value)) pickupLocationId.value = ''
    if (!locations.value.some((item) => item.locationId === dropoffLocationId.value)) dropoffLocationId.value = ''
    if (!locations.value.length) locationError.value = t('暫無可選地點')
  } else {
    locationError.value = displayError(locationResult.reason)
  }
  if (bookingOptionsResult.status === 'fulfilled') {
    bookingOptions.value = bookingOptionsResult.value.data
    const firstBookableProduct = bookingOptions.value.products.find((item) => item.isBookable)
    if (!bookingOptions.value.products.some((item) => item.productCode === selectedProductCode.value && item.isBookable)) {
      selectedProductCode.value = firstBookableProduct?.productCode ?? ''
    }
    if (!firstBookableProduct) vehicleError.value = t('暫無可選車型')
  } else {
    vehicleError.value = displayError(bookingOptionsResult.reason)
  }
  optionsLoading.value = false
}

async function searchGuests(): Promise<void> {
  selectedGuest.value = null
  guests.value = []
  preview.value = null
  if (!guestSearch.value.trim()) { error.value = t('請輸入賓客姓名或會員編號'); return }
  try { guests.value = (await staffApi.guests(guestSearch.value.trim())).data; error.value = '' }
  catch (cause) { error.value = displayError(cause) }
}

function selectGuest(guest: Guest): void { selectedGuest.value = guest; guests.value = []; preview.value = null }
function openOptionSheet(kind: OptionSheetKind): void {
  if (optionsLoading.value || !(kind === 'property' ? propertyOptions.value.length : locationOptions.value.length)) return
  optionSheetKind.value = kind
}
function confirmOption(id: string): void {
  const kind = optionSheetKind.value
  if (kind === 'property' && propertyOptions.value.some((item) => item.id === id)) propertyId.value = id
  else if (kind === 'pickup' && locationOptions.value.some((item) => item.id === id)) pickupLocationId.value = id
  else if (kind === 'dropoff' && locationOptions.value.some((item) => item.id === id)) dropoffLocationId.value = id
  optionSheetKind.value = null
}
function selectDeparture(value: { date: string; time: string }): void {
  date.value = value.date
  time.value = value.time
  isDeparturePickerOpen.value = false
}

function buildInput(): BookingInput | null {
  const pickupAt = toUtc(date.value, time.value)
  const passengerCount = Number(passengers.value)
  if (!selectedGuest.value || !propertyId.value || !pickupLocationId.value || !dropoffLocationId.value || !pickupAt) {
    error.value = t('請完成賓客、物業、路線及出發時間'); return null
  }
  if (pickupLocationId.value === dropoffLocationId.value) { error.value = t('上車地點與目的地不可相同'); return null }
  if (!isFutureMacauDeparture(date.value, time.value, new Date())) { error.value = t('出發時間須在未來'); return null }
  if (!selectedProduct.value?.isBookable) { error.value = t('請選擇可用車型'); return null }
  if (!Number.isInteger(passengerCount) || passengerCount < 1 || passengerCount > 20) {
    error.value = t('請輸入有效的乘客數量'); return null
  }
  return {
    guestId: selectedGuest.value.guestId, propertyId: propertyId.value, pickupLocationId: pickupLocationId.value,
    dropoffLocationId: dropoffLocationId.value, pickupAt, pickupMode: 'SCHEDULED', serviceType: selectedProduct.value.serviceType,
    productCode: selectedProduct.value.productCode, vehicleClass: selectedProduct.value.vehicleClass, passengers: passengerCount, luggage: 0,
    confirmationLanguage: locale.value === 'en' ? 'EN' : 'ZH_HANT',
  }
}

async function checkBooking(): Promise<void> {
  const input = buildInput(); if (!input) return
  busy.value = true; error.value = ''; preview.value = null; isConfirmationOpen.value = false
  try {
    preview.value = (await staffApi.previewBooking(input)).data
    if (preview.value.canSubmit) isConfirmationOpen.value = true
  }
  catch (cause) { error.value = displayError(cause) }
  finally { busy.value = false }
}
async function submit(): Promise<void> {
  if (busy.value || !isConfirmationOpen.value || !preview.value?.canSubmit) return
  const input = buildInput(); if (!input) return
  busy.value = true; error.value = ''
  try {
    const trip = (await staffApi.createBooking(input, submissionKey.value)).data
    isConfirmationOpen.value = false
    uni.showToast({ title: t('預約已建立'), icon: 'success' })
    uni.redirectTo({ url: `/pages/trip-detail/index?tripId=${encodeURIComponent(trip.tripId)}` })
  } catch (cause) { error.value = displayError(cause) }
  finally { busy.value = false }
}
</script>

<template>
  <AppShell title="快速預約" subtitle="為賓客安排專屬行程">
    <view class="card">
      <text class="card-title">{{ t('01 · 賓客資料') }}</text>
      <text class="field-label">{{ t('搜尋賓客') }}</text>
      <view class="search-row"><input v-model="guestSearch" class="field" :placeholder="t('姓名或會員編號')" @confirm="searchGuests" /><button class="secondary-button" @tap="searchGuests">{{ t('搜尋') }}</button></view>
      <view v-if="selectedGuest" class="selected"><text>{{ selectedGuest.name }}</text><text class="muted">{{ selectedGuest.memberId || selectedGuest.guestId }}</text></view>
      <button v-for="guest in guests" :key="guest.guestId" class="guest-option" @tap="selectGuest(guest)"><text>{{ guest.name }}</text><text class="muted">{{ guest.memberId || guest.guestId }}</text></button>
      <text class="field-label">{{ t('所屬物業') }}</text>
      <button class="field-picker select-trigger" :aria-label="t('選擇物業')" :disabled="optionsLoading || !properties.length" @tap="openOptionSheet('property')"><text>{{ properties.find(item => item.propertyId === propertyId)?.name || t(optionsLoading ? '載入中…' : '選擇物業') }}</text><text aria-hidden="true">›</text></button>
      <view v-if="propertyError" class="option-error"><text class="error-text">{{ propertyError }}</text><button class="text-button" :disabled="optionsLoading" @tap="loadOptions">{{ t('重試') }}</button></view>
    </view>

    <view class="card">
      <text class="card-title">{{ t('02 · 行程安排') }}</text>
      <text class="field-label">{{ t('上車地點') }}</text>
      <button class="field-picker select-trigger" :aria-label="t('選擇上車地點')" :disabled="optionsLoading || !locations.length" @tap="openOptionSheet('pickup')"><text>{{ locations.find(item => item.locationId === pickupLocationId)?.name || t(optionsLoading ? '載入中…' : '選擇上車地點') }}</text><text aria-hidden="true">›</text></button>
      <view v-if="locationError" class="option-error"><text class="error-text">{{ locationError }}</text><button class="text-button" :disabled="optionsLoading" @tap="loadOptions">{{ t('重試') }}</button></view>
      <text class="field-label">{{ t('目的地') }}</text>
      <button class="field-picker select-trigger" :aria-label="t('選擇目的地')" :disabled="optionsLoading || !locations.length" @tap="openOptionSheet('dropoff')"><text>{{ locations.find(item => item.locationId === dropoffLocationId)?.name || t(optionsLoading ? '載入中…' : '選擇目的地') }}</text><text aria-hidden="true">›</text></button>
      <text class="field-label">{{ t('出發日期與時間') }}</text>
      <button class="field-picker select-trigger" :aria-label="t('選擇出發日期與時間')" @tap="isDeparturePickerOpen = true"><text>{{ date }} {{ time }} · {{ t('澳門時間') }}</text><text aria-hidden="true">›</text></button>
    </view>

    <view class="card">
      <text class="card-title">{{ t('03 · 車輛與乘客') }}</text>
      <text class="field-label">{{ t('車型') }}</text>
      <SegmentedChoice v-if="vehicleOptions.length" v-model="selectedProductCode" :options="vehicleOptions" :label="t('車型')" />
      <text v-else class="muted">{{ t(optionsLoading ? '載入中…' : '暫無可選車型') }}</text>
      <view v-if="vehicleError" class="option-error"><text class="error-text">{{ vehicleError }}</text><button class="text-button" :disabled="optionsLoading" @tap="loadOptions">{{ t('重試') }}</button></view>
      <text class="field-label">{{ t('乘客') }}</text><input v-model="passengers" class="field" type="number" />
    </view>

    <text v-if="error" class="error-text" role="alert">{{ error }}</text>
    <view v-if="preview && !preview.canSubmit" class="card preview"><text class="card-title">{{ t('預約預覽') }}</text><text v-if="preview.willWaitlist" class="warning">{{ t('此預約可能進入候補') }}</text><text v-if="preview.requiresApproval" class="warning">{{ t('此預約需要審批') }}</text><text v-for="reason in preview.blockedReasons" :key="reason.code" class="error-text">{{ reason.field }} · {{ reason.message }}</text><text class="muted">{{ t('所需積分：') }} {{ preview.points.required }}</text></view>
    <button class="primary-button" :disabled="busy" @tap="checkBooking">{{ t(busy ? (isConfirmationOpen ? '提交中…' : '檢查中…') : '檢查預約') }}</button>
    <view v-if="isConfirmationOpen && preview" class="confirmation-backdrop" role="presentation" @tap.self="isConfirmationOpen = false">
      <view class="confirmation-dialog" role="dialog" aria-modal="true" :aria-label="t('預約確認')">
        <text class="card-title">{{ t('預約確認') }}</text>
        <view class="confirmation-facts">
          <view><text>{{ t('賓客') }}</text><text>{{ selectedGuest?.name || '—' }}</text></view>
          <view><text>{{ t('所屬物業') }}</text><text>{{ properties.find(item => item.propertyId === propertyId)?.name || '—' }}</text></view>
          <view><text>{{ t('上車地點') }}</text><text>{{ locations.find(item => item.locationId === pickupLocationId)?.name || '—' }}</text></view>
          <view><text>{{ t('目的地') }}</text><text>{{ locations.find(item => item.locationId === dropoffLocationId)?.name || '—' }}</text></view>
          <view><text>{{ t('出發日期與時間') }}</text><text>{{ date }} {{ time }} · {{ t('澳門時間') }}</text></view>
          <view><text>{{ t('車型') }}</text><text>{{ vehicleOptions.find(item => item.value === selectedProductCode)?.label || '—' }}</text></view>
          <view><text>{{ t('乘客') }}</text><text>{{ passengers }}</text></view>
        </view>
        <text v-if="preview.willWaitlist" class="warning">{{ t('此預約可能進入候補') }}</text>
        <text v-if="preview.requiresApproval" class="warning">{{ t('此預約需要審批') }}</text>
        <text class="muted">{{ t('所需積分：') }} {{ preview.points.required }}</text>
        <text v-if="error" class="error-text" role="alert">{{ error }}</text>
        <view class="confirmation-actions">
          <button class="secondary-button" :disabled="busy" @tap="isConfirmationOpen = false">{{ t('返回修改') }}</button>
          <button class="primary-button" :disabled="busy || !preview.canSubmit" @tap="submit">{{ t(busy ? '提交中…' : '確認預約') }}</button>
        </view>
      </view>
    </view>
    <BookingDeparturePicker :open="isDeparturePickerOpen" :date="date" :time="time" @close="isDeparturePickerOpen = false" @confirm="selectDeparture" />
    <BookingOptionSheet :open="optionSheetKind !== null" :title="optionSheetTitle" :options="optionSheetOptions" :selected-id="optionSheetSelectedId" @close="optionSheetKind = null" @confirm="confirmOption" />
  </AppShell>
</template>

<style scoped>
.search-row { display:flex; gap:8px; }.search-row .field { flex:1; min-width:0; }
.selected { display:flex; justify-content:space-between; margin-top:10px; padding:12px; border-radius:10px; background:#f1f3fa; color:#414d8d; font-size:13px; }
.guest-option { justify-content:space-between; width:100%; min-height:42px; padding:0 10px; border-bottom:1px solid #eff0f3; color:#252d45; font-size:13px; }
.option-error { display:flex; align-items:center; gap:10px; }.option-error .error-text { flex:1; }.option-error .text-button { flex:none; margin:0; }
.select-trigger { box-sizing:border-box; width:100%; margin:0; justify-content:space-between; color:#252c40; text-align:left; }
.select-trigger::after { display:none; }.select-trigger[disabled] { color:#8d96a8; opacity:1; }
.select-trigger text:first-child { min-width:0; flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; text-align:left; }
.select-trigger text:last-child { flex:none; margin-left:10px; color:#8d96a8; font-size:22px; }
.warning { display:block; margin:10px 0; color:#b07824; font-size:12px; }.preview { border-color:#cbd1eb; }
.confirmation-backdrop { position:fixed; inset:0; z-index:80; display:flex; align-items:center; justify-content:center; box-sizing:border-box; padding:20px; background:rgba(17,24,39,.48); }
.confirmation-dialog { box-sizing:border-box; width:min(560px,100%); max-height:82vh; overflow-y:auto; padding:20px; border-radius:16px; background:#fff; box-shadow:0 16px 48px rgba(16,24,40,.2); }
.confirmation-facts { display:grid; gap:0; margin:14px 0; }
.confirmation-facts view { display:flex; justify-content:space-between; gap:14px; padding:10px 0; border-bottom:1px solid #edf0f5; font-size:13px; }
.confirmation-facts view text:first-child { flex:none; color:#7d879a; }.confirmation-facts view text:last-child { min-width:0; color:#202941; text-align:right; overflow-wrap:anywhere; }
.confirmation-actions { display:grid; grid-template-columns:1fr 1fr; gap:10px; margin-top:18px; }.confirmation-actions button { margin:0; }
</style>
