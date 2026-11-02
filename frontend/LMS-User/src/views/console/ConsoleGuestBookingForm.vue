<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { Check, Search, UserRound } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { ApiRequestError, apiErrorMessage, type ApiFieldError } from '../../api/http'
import { currentUser, isDemoMode } from '../../api/session'
import { lms, mapGuest, mapGuestProfile, mapTrip, type BookingProductCode, type ConsoleBookingOptions, type CreateTripRequest, type PropertyView } from '../../api/lms'
import type { Booking, Guest } from '../../data/demo'
import { useDemoStore } from '../../stores/demo'

const emit = defineEmits<{
  (event: 'created', booking: Booking): void
  (event: 'busy-change', busy: boolean): void
}>()
const { t, confirmationLanguage } = useLocale()
const store = useDemoStore()
const canBook = computed(() => isDemoMode || Boolean(currentUser.value?.permissions.includes('bookings:write')))
const search = ref('')
const searchPage = ref(1)
const searchTotal = ref(0)
const results = ref<Guest[]>([])
const hasSearched = ref(false)
const isSearching = ref(false)
const isSelecting = ref(false)
const isSaving = ref(false)
const isLoading = ref(false)
const selectedGuest = ref<Guest | null>(null)
const properties = ref<PropertyView[]>([])
const error = ref('')
const directoryError = ref('')
const searchError = ref('')
const fieldErrors = ref<ApiFieldError[]>([])
const successReference = ref('')
const createdTripId = ref('')
const bookingOptions = ref<ConsoleBookingOptions | null>(null)
const canDispatch = computed(() => isDemoMode || Boolean(currentUser.value?.permissions.includes('dispatch:write')))
const form = reactive({
  propertyId: '', pickupLocationId: '', dropoffLocationId: '', pickupLocal: '',
  productCode: 'LIMOUSINE' as BookingProductCode,
  passengers: 1, specialRequests: '',
})
let searchGeneration = 0
let guestGeneration = 0
let lastPayload = ''
let idempotencyKey = ''
const selectedProperty = computed(() => properties.value.find((item) => item.propertyId === form.propertyId))
const pickup = computed(() => store.locations.find((item) => item.locationId === form.pickupLocationId))
const dropoff = computed(() => store.locations.find((item) => item.locationId === form.dropoffLocationId))
const selectedProduct = computed(() => bookingOptions.value?.products.find((item) => item.productCode === form.productCode))
const selectedVehicle = computed(() => bookingOptions.value?.vehicles.find((item) => item.vehicleClass === selectedProduct.value?.vehicleClass))
const vehicleLabel = computed(() => {
  const labels: Record<string, string> = { BUSINESS_SEDAN: 'Standard Sedan', LUXURY_SEDAN: 'Luxury Sedan', LUXURY_MPV: 'Luxury MPV', MAYBACH: 'Maybach' }
  return labels[selectedProduct.value?.vehicleClass ?? ''] ?? '—'
})
function nextPickupLocal(): string {
  const value = new Date(Date.now() + 30 * 60_000 + 8 * 60 * 60_000)
  return value.toISOString().slice(0, 16)
}
const pickupDate = computed({ get: () => form.pickupLocal.slice(0, 10), set: (value) => { form.pickupLocal = `${value}T${form.pickupLocal.slice(11) || nextPickupLocal().slice(11)}` } })
const pickupTime = computed({ get: () => form.pickupLocal.slice(11), set: (value) => { form.pickupLocal = `${form.pickupLocal.slice(0, 10) || nextPickupLocal().slice(0, 10)}T${value}` } })
const isReady = computed(() => Boolean(selectedGuest.value && selectedProduct.value?.isBookable && selectedVehicle.value && form.propertyId && pickup.value && dropoff.value && form.pickupLocal))
function fieldError(field: string): string { return fieldErrors.value.filter((item) => item.field === field || item.field === `route.${field}`).map((item) => t(item.message)).join(' ') }
function applyDefaultProperty(): void {
  if (form.propertyId) return
  const preferred = selectedGuest.value?.propertyId ?? currentUser.value?.propertyId
  if (properties.value.some((item) => item.propertyId === preferred)) form.propertyId = preferred ?? ''
  else if (properties.value.length === 1) form.propertyId = properties.value[0]?.propertyId ?? ''
}
async function loadDirectories(): Promise<void> {
  isLoading.value = true; directoryError.value = ''
  try {
    if (isDemoMode) loadDemoOptions()
    else {
      const [values, , options] = await Promise.all([store.loadProperties(), store.loadLocations(), lms.bookingOptions()])
      properties.value = values; bookingOptions.value = options
    }
    form.productCode = bookingOptions.value?.products.find((item) => item.isBookable)?.productCode ?? 'LIMOUSINE'
    applyDefaultProperty()
  } catch (cause) { directoryError.value = apiErrorMessage(cause) }
  finally { isLoading.value = false }
}
function loadDemoOptions(): void {
  properties.value = [...new Set(store.bookings.map((item) => item.propertyName))].map((name) => ({ propertyId: name, name }))
  bookingOptions.value = {
    products: [
      { productCode: 'BUSINESS_SEDAN', vehicleClass: 'BUSINESS_SEDAN', name: 'Standard Sedan', isBookable: true, serviceType: 'ONE_WAY', routeScope: 'LOCAL' },
      { productCode: 'LIMOUSINE', vehicleClass: 'LUXURY_SEDAN', name: 'Limousine', isBookable: true, serviceType: 'ONE_WAY', routeScope: 'LOCAL' },
      { productCode: 'PARTY', vehicleClass: 'LUXURY_MPV', name: 'Party', isBookable: true, serviceType: 'ONE_WAY', routeScope: 'LOCAL' },
      { productCode: 'CROSS_BORDER_TRANSFER', vehicleClass: 'LUXURY_MPV', name: 'Cross-Border Transfer', isBookable: true, serviceType: 'ONE_WAY', routeScope: 'CROSS_BORDER' },
    ],
    vehicles: [{ vehicleClass: 'BUSINESS_SEDAN', maxPassengers: 3, maxLuggage: 2 }, { vehicleClass: 'LUXURY_SEDAN', maxPassengers: 3, maxLuggage: 2 }, { vehicleClass: 'LUXURY_MPV', maxPassengers: 6, maxLuggage: 4 }],
    isAsapAvailable: false,
  }
}
async function searchGuests(): Promise<void> {
  const generation = ++searchGeneration
  isSearching.value = true; hasSearched.value = true; searchError.value = ''; results.value = []; searchTotal.value = 0
  try {
    if (isDemoMode) { applyDemoSearch(); return }
    const response = await lms.listGuestPage({ search: search.value.trim() || undefined, page: searchPage.value, pageSize: 20 })
    if (generation !== searchGeneration) return
    results.value = response.data.map(mapGuest); searchTotal.value = response.meta?.pagination?.totalItems ?? results.value.length
  } catch (cause) { if (generation === searchGeneration) searchError.value = apiErrorMessage(cause) }
  finally { if (generation === searchGeneration) isSearching.value = false }
}
function applyDemoSearch(): void {
  const query = search.value.toLowerCase().trim()
  const matches = store.guests.filter((guest) => `${guest.fullName} ${guest.memberId ?? ''} ${guest.roomNumber}`.toLowerCase().includes(query))
  searchTotal.value = matches.length; results.value = matches.slice((searchPage.value - 1) * 20, searchPage.value * 20)
}
function startSearch(): void { if (searchPage.value !== 1) searchPage.value = 1; else void searchGuests() }
async function selectGuest(guest: Guest): Promise<void> {
  const generation = ++guestGeneration
  isSelecting.value = true; searchError.value = ''; successReference.value = ''
  try {
    const detail = isDemoMode ? guest : mapGuestProfile(await lms.guestProfile(guest.guestId))
    if (generation !== guestGeneration) return
    selectedGuest.value = detail; error.value = ''; fieldErrors.value = []; form.propertyId = ''; applyDefaultProperty()
  } catch (cause) { if (generation === guestGeneration) searchError.value = apiErrorMessage(cause) }
  finally { if (generation === guestGeneration) isSelecting.value = false }
}
function validate(): boolean {
  fieldErrors.value = []; error.value = ''; successReference.value = ''
  if (!canBook.value) { error.value = 'You do not have permission to create bookings.'; return false }
  if (!isReady.value) { error.value = 'Select a member, property, route and pickup time.'; return false }
  if (selectedGuest.value?.isBlacklisted) { error.value = 'This guest is restricted by LMS.'; return false }
  if (!Number.isInteger(form.passengers) || form.passengers < 1) fieldErrors.value.push({ field: 'passengers', message: 'Enter a positive whole number.' })
  if (form.passengers > (selectedVehicle.value?.maxPassengers ?? 0)) fieldErrors.value.push({ field: 'passengers', message: 'Passengers exceed the capacity of the selected vehicle class' })
  if (form.pickupLocationId === form.dropoffLocationId) fieldErrors.value.push({ field: 'dropoffLocationId', message: 'Must differ from pickupLocationId' })
  if (!Number.isFinite(Date.parse(`${form.pickupLocal}+08:00`))) fieldErrors.value.push({ field: 'pickupAt', message: 'Enter a valid pickup time.' })
  if (Date.parse(`${form.pickupLocal}+08:00`) <= Date.now()) fieldErrors.value.push({ field: 'pickupAt', message: 'Choose a future departure time.' })
  return fieldErrors.value.length === 0
}
function buildRequest(): CreateTripRequest {
  return {
    guestId: selectedGuest.value!.guestId, propertyId: form.propertyId,
    pickupLocationId: form.pickupLocationId, dropoffLocationId: form.dropoffLocationId,
    pickupMode: 'SCHEDULED', pickupAt: new Date(`${form.pickupLocal}+08:00`).toISOString(),
    serviceType: selectedProduct.value!.serviceType, productCode: form.productCode,
    passengers: form.passengers, luggage: 0,
    specialRequests: form.specialRequests.trim() || undefined, confirmationLanguage: confirmationLanguage.value,
  }
}
async function createBooking(request: CreateTripRequest): Promise<Booking | null> {
  const preview = await lms.previewBooking(request)
  if (!preview.canSubmit) {
    fieldErrors.value = preview.blockedReasons
    error.value = 'This booking cannot be submitted.'
    return null
  }
  const payload = JSON.stringify(request)
  if (payload !== lastPayload) { lastPayload = payload; idempotencyKey = crypto.randomUUID() }
  return mapTrip(await lms.createTrip(request, idempotencyKey))
}
function createDemoBooking(request: CreateTripRequest): Booking {
  return {
    bookingId: `TR-${Date.now()}`, guestId: request.guestId, guestName: selectedGuest.value!.fullName,
    roomNumber: selectedGuest.value!.roomNumber, propertyName: selectedProperty.value?.name ?? '',
    pickupLocation: pickup.value!.name, dropoffLocation: dropoff.value!.name,
    pickupAt: request.pickupAt ?? new Date(Date.now() + 900_000).toISOString(),
    productCode: request.productCode, vehicleClass: vehicleLabel.value, status: 'Confirmed', stageLabel: 'Awaiting driver assignment', points: 0, channel: 'CONSOLE', createdByStaffId: 'FD-SL-003',
  }
}
async function submit(): Promise<void> {
  if (isSaving.value || !validate()) return
  isSaving.value = true; emit('busy-change', true)
  try {
    const request = buildRequest()
    const booking = isDemoMode ? createDemoBooking(request) : await createBooking(request)
    if (!booking) return
    resetForm(); successReference.value = booking.tripReference || booking.bookingId; createdTripId.value = booking.bookingId
    emit('created', booking)
  } catch (cause) {
    error.value = apiErrorMessage(cause)
    if (cause instanceof ApiRequestError) fieldErrors.value = cause.errors
  } finally { isSaving.value = false; emit('busy-change', false) }
}
function resetForm(): void {
  searchGeneration++; guestGeneration++; isSearching.value = false; isSelecting.value = false
  selectedGuest.value = null; search.value = ''; results.value = []; hasSearched.value = false
  Object.assign(form, { propertyId: '', pickupLocationId: '', dropoffLocationId: '', pickupLocal: nextPickupLocal(), productCode: bookingOptions.value?.products.find((item) => item.isBookable)?.productCode ?? 'LIMOUSINE', passengers: 1, specialRequests: '' })
  createdTripId.value = ''
  fieldErrors.value = []; error.value = ''; successReference.value = ''; lastPayload = ''; idempotencyKey = ''
  applyDefaultProperty()
}
watch(searchPage, () => { void searchGuests() })
watch(search, () => { searchGeneration++; isSearching.value = false; hasSearched.value = false; results.value = []; searchTotal.value = 0 })
watch(currentUser, applyDefaultProperty)
watch(form, () => { error.value = ''; fieldErrors.value = [] }, { deep: true })
onMounted(() => { form.pickupLocal = nextPickupLocal(); void loadDirectories() })
onBeforeUnmount(() => { searchGeneration++; guestGeneration++ })
</script>

<template>
  <p v-if="directoryError" class="form-error" role="alert">{{ directoryError }} <button class="text-button" @click="loadDirectories">{{ t('Retry') }}</button></p>
  <div v-if="successReference" class="success-banner" role="status"><Check :size="16" />{{ successReference }} {{ t(isDemoMode ? 'created in this local demo. No backend request was sent.' : 'Booking created. Awaiting dispatch and driver acceptance.') }} <RouterLink v-if="canDispatch && createdTripId" :to="{ path: '/console/dispatch', query: { tripId: createdTripId } }">{{ t('Go to dispatch') }}</RouterLink></div>
  <form class="guest-booking-layout" @submit.prevent="submit">
    <fieldset class="guest-booking-fields" :disabled="isSaving || isLoading || !canBook">
      <section class="panel">
        <div class="panel-heading"><h2>{{ t('Trip information') }}</h2></div>
        <div class="guest-booking-body guest-booking-grid">
          <div class="member-section full-width">
            <div class="member-row">
              <label class="member-label" :for="selectedGuest ? undefined : 'booking-member-search'">{{ t('Passenger details') }}</label>
              <div v-if="!selectedGuest" class="search-row"><label class="input-with-icon"><Search :size="16" /><input id="booking-member-search" v-model="search" :placeholder="t('Search member name, member ID or room')" @keydown.enter.prevent="startSearch" /></label><button type="button" class="secondary-button" :disabled="isSearching" @click="startSearch">{{ t(isSearching ? 'Searching...' : 'Search') }}</button></div>
              <div v-else class="selected-member">
                <div class="member-copy">
                  <strong>{{ selectedGuest.fullName }}</strong>
                  <span class="member-tier">{{ t(selectedGuest.tier) }}</span>
                  <span class="member-meta">{{ t('Member ID') }} {{ selectedGuest.memberId || '—' }}</span>
                  <span class="member-meta" :title="t('Contact phone')">{{ selectedGuest.phoneMasked || '—' }}</span>
                  <span class="member-meta">{{ t('Room') }} {{ selectedGuest.roomNumber || '—' }}</span>
                </div>
                <button type="button" class="text-button" @click="selectedGuest = null">{{ t('CHANGE GUEST') }}</button>
              </div>
            </div>
            <p v-if="searchError" class="form-error" role="alert">{{ searchError }}</p>
            <div v-if="hasSearched && !selectedGuest" class="member-results" :aria-busy="isSearching || isSelecting">
              <button v-for="guest in results" :key="guest.guestId" type="button" class="guest-select-card" :disabled="isSelecting || isSearching" @click="selectGuest(guest)"><span class="round-avatar"><UserRound :size="16" /></span><span><strong>{{ guest.fullName }}</strong><small>{{ guest.memberId || '—' }} · {{ t('Room') }} {{ guest.roomNumber || '—' }}</small></span><span class="link-arrow">{{ t('SELECT →') }}</span></button>
              <p v-if="!isSearching && !results.length" class="muted">{{ t('No matching guests.') }}</p>
              <div v-if="searchTotal > 20" class="member-search-pager"><button type="button" class="secondary-button" :disabled="searchPage <= 1 || isSearching" :aria-label="t('Previous page')" @click="searchPage--">‹</button><span>{{ searchPage }} / {{ Math.ceil(searchTotal / 20) }}</span><button type="button" class="secondary-button" :disabled="searchPage * 20 >= searchTotal || isSearching" :aria-label="t('Next page')" @click="searchPage++">›</button></div>
            </div>
          </div>
          <label class="field-label">{{ t('Service property') }}<select v-model="form.propertyId" required><option value="">{{ t('Select property') }}</option><option v-for="property in properties" :key="property.propertyId" :value="property.propertyId">{{ property.name }}</option></select><small v-if="fieldError('propertyId')" class="field-error">{{ fieldError('propertyId') }}</small></label>
          <label class="field-label">{{ t('Service product') }}<select v-model="form.productCode" required><option v-for="option in bookingOptions?.products ?? []" :key="option.productCode" :value="option.productCode" :disabled="!option.isBookable">{{ t(option.productCode === 'LIMOUSINE' ? 'Luxury Sedan' : option.name) }}</option></select><small v-if="fieldError('productCode')" class="field-error">{{ fieldError('productCode') }}</small></label>
          <label class="field-label">{{ t('Pickup location') }}<select v-model="form.pickupLocationId" required><option value="">{{ t('Select location') }}</option><option v-for="location in store.locations" :key="location.locationId" :value="location.locationId">{{ location.name }}</option></select><small v-if="fieldError('pickupLocationId')" class="field-error">{{ fieldError('pickupLocationId') }}</small></label>
          <label class="field-label">{{ t('Drop-off location') }}<select v-model="form.dropoffLocationId" required><option value="">{{ t('Select location') }}</option><option v-for="location in store.locations" :key="location.locationId" :value="location.locationId">{{ location.name }}</option></select><small v-if="fieldError('dropoffLocationId')" class="field-error">{{ fieldError('dropoffLocationId') }}</small></label>
          <div class="booking-time-grid full-width">
            <label class="field-label">{{ t('Date') }}<input v-model="pickupDate" type="date" required /></label>
            <label class="field-label">{{ t('Time') }}<input v-model="pickupTime" type="time" required /></label>
            <label class="field-label">{{ t('Passengers') }}<input v-model.number="form.passengers" type="number" min="1" :max="selectedVehicle?.maxPassengers" step="1" required /><small v-if="fieldError('passengers')" class="field-error">{{ fieldError('passengers') }}</small></label>
            <small v-if="fieldError('pickupAt')" class="field-error full-width">{{ fieldError('pickupAt') }}</small>
          </div>
          <label class="field-label full-width">{{ t('Notes') }}<textarea v-model="form.specialRequests" rows="2" :placeholder="t('Optional passenger requests')" /></label>
        </div>
      </section>
    </fieldset>
    <aside class="panel booking-confirmation">
      <div class="panel-heading"><h2>{{ t('Booking confirmation') }}</h2><span>{{ t('Guest bookings') }}</span></div>
      <div class="guest-booking-body">
        <dl class="booking-summary"><dt>{{ t('Passenger') }}</dt><dd>{{ selectedGuest?.fullName || t('No member selected') }}</dd><dt>{{ t('Contact phone') }}</dt><dd>{{ selectedGuest?.phoneMasked || '—' }}</dd><dt>{{ t('Service property') }}</dt><dd>{{ selectedProperty?.name ?? '—' }}</dd><dt>{{ t('Route') }}</dt><dd class="summary-route"><span>{{ pickup?.name ?? '—' }}</span><span>{{ dropoff?.name ?? '—' }}</span></dd><dt>{{ t('Pickup time') }}</dt><dd>{{ form.pickupLocal.replace('T', ' ') || '—' }}</dd><dt>{{ t('Passengers') }}</dt><dd>{{ form.passengers }}</dd><dt>{{ t('Service product') }}</dt><dd>{{ selectedProduct ? t(selectedProduct.productCode === 'LIMOUSINE' ? 'Luxury Sedan' : selectedProduct.name) : '—' }}</dd><dt>{{ t('Booked by') }}</dt><dd>{{ currentUser?.name || (isDemoMode ? t('Front Desk Agent') : '—') }}</dd></dl>
        <p class="booking-rule-note">{{ t('Booking requires dispatch and driver acceptance before the trip can start.') }}</p>
        <p v-if="!canBook" class="form-error">{{ t('You do not have permission to create bookings.') }}</p>
        <div v-if="error || fieldErrors.length" class="form-error" role="alert"><p v-if="error">{{ t(error) }}</p><p v-for="(item, index) in fieldErrors" :key="index">{{ t(item.message) }}</p></div>
        <div class="booking-submit-actions"><button type="button" class="secondary-button" :disabled="isSaving" @click="resetForm">{{ t('Reset') }}</button><button type="submit" class="primary-button" :disabled="!isReady || !canBook || isSaving || isLoading || Boolean(directoryError)">{{ t(isSaving ? 'Submitting...' : 'Confirm booking') }}</button></div>
      </div>
    </aside>
  </form>
</template>

<style scoped>
.guest-booking-layout{display:grid;grid-template-columns:minmax(0,1fr) 400px;gap:12px;align-items:start;margin:0 0 16px;font-size:14px}
.guest-booking-fields{border:0;padding:0;margin:0;min-width:0}.guest-booking-fields:disabled{opacity:.65}
.guest-booking-layout :deep(.panel){margin-bottom:12px}.guest-booking-layout :deep(.panel-heading){min-height:44px;padding:10px 16px}.guest-booking-layout :deep(.panel-heading h2){font-size:16px}.guest-booking-layout :deep(.panel-heading>span){font-size:12px;color:#4149a4}
.guest-booking-body{padding:16px}.guest-booking-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px 16px}
.guest-booking-grid .field-label{display:flex;flex-direction:column;align-items:stretch;gap:6px;min-width:0;margin:0;font-size:12px;line-height:18px}.guest-booking-grid input,.guest-booking-grid select{width:100%;min-width:0}.full-width{grid-column:1/-1}
.member-section{min-width:0}
.member-row{display:grid;grid-template-columns:minmax(0,1fr);gap:6px}
.member-label{font-size:12px;font-weight:600;line-height:18px}
.member-row .search-row{display:flex;align-items:center;gap:8px;min-width:0}
.member-row .input-with-icon{flex:1;min-width:0}
.member-row .search-row input{height:34px;min-width:0}
.member-row .search-row .secondary-button{flex-shrink:0;min-width:68px;height:34px}
.member-results{display:grid;gap:6px;max-height:220px;overflow:auto;margin-top:8px}
.member-results .guest-select-card{width:100%}
.member-search-pager{display:flex;justify-content:flex-end;align-items:center;gap:12px;font-size:11px}
.selected-member{display:flex;align-items:center;gap:12px;min-width:0;min-height:34px;padding:6px 10px;border:1px solid #d9dce6;background:#fafbfe}
.member-copy{display:flex;flex:1;align-items:center;flex-wrap:wrap;gap:4px 12px;min-width:0;overflow-wrap:anywhere}
.selected-member strong{font-size:13px}.selected-member .text-button{flex-shrink:0;font-size:12px}
.member-meta{font-size:12px;color:#656c7b}.member-tier{padding:2px 6px;background:#e5e9fc;color:#4854ad;font-size:11px}
.booking-time-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px 16px;align-items:start}.field-error{font-weight:400;color:#b4313a}
.booking-confirmation{position:sticky;top:70px}.booking-summary{display:grid;grid-template-columns:110px minmax(0,1fr);gap:14px 12px;margin:0;font-size:13px;line-height:1.5}.booking-summary dt{color:#656c7b}.booking-summary dd{margin:0;overflow-wrap:anywhere;font-weight:500}.summary-route{display:grid;gap:10px;position:relative;padding-left:25px}.summary-route span::before{content:'';position:absolute;left:3px;width:10px;height:10px;border-radius:50%;background:#4149a4;margin-top:4px}.summary-route span+span::before{background:#fff;border:2px solid #4149a4;width:7px;height:7px}.booking-rule-note{color:#858b98;padding-top:16px;font-size:12px}.booking-submit-actions{display:flex;gap:8px;margin-top:12px}.booking-submit-actions button{height:38px;font-size:13px}.booking-submit-actions .secondary-button{flex:1}.booking-submit-actions .primary-button{flex:1.1}.success-banner{margin:14px 0}.search-row .input-with-icon{min-width:0}
@media(max-width:1300px){.guest-booking-layout{grid-template-columns:minmax(0,1fr) 360px;gap:12px}}
@media(max-width:900px){.guest-booking-layout{grid-template-columns:1fr}.booking-confirmation{position:static}.booking-summary{grid-template-columns:130px minmax(0,1fr)}}
@media(max-width:550px){.guest-booking-grid,.booking-time-grid{grid-template-columns:1fr}.guest-booking-body{padding:14px}.member-row{grid-template-columns:1fr;gap:6px}.selected-member{align-items:flex-start}.guest-booking-layout{gap:0}.full-width{grid-column:auto}}
</style>
