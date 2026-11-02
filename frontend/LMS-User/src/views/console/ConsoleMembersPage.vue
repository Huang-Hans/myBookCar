<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { isDemoMode, currentUser } from '../../api/session'
import { apiErrorMessage } from '../../api/http'
import { lms, mapDriver, mapGuest, mapGuestProfile, mapLocation, type BookingPreviewView, type CreateTripRequest } from '../../api/lms'
import { dispatchApi, type CouponView } from '../../api/dispatch'
import { macauDayKey, useFleetStore } from '../../stores/fleet'
import DataPager from '../../components/DataPager.vue'
import ConsoleMemberEditor from './ConsoleMemberEditor.vue'
import type { GuestProfileView, PropertyView } from '../../api/lms'

type Tier = 'Silver' | 'Gold' | 'Platinum' | 'Diamond' | 'VIP'
type DispatchMode = 'Direct dispatch' | 'Coupon dispatch' | 'Tier dispatch'
type VehicleClass = 'Standard Sedan' | 'Luxury Sedan' | 'Luxury MPV'

interface MemberCoupon { couponId: string; code: string; name: string; vehicleRestriction: string | null }
interface Member {
  guestId: string
  memberId: string
  name: string
  tier: Tier
  room: string
  property: string
  points: number
  coupons: MemberCoupon[]
  inHouse: boolean
}
interface MemberRequest {
  id: string
  memberId: string
  name: string
  mode: DispatchMode
  coupon: string
  vehicleClass: string
  pickup: string
  dropoff: string
  when: string
  passengers: number
  luggage: number
  driverName: string
  notes: string
}
interface Stop { id: string; name: string; airport: boolean }
interface DriverOption { driverId: string; name: string; property: string; vehicle: string }

const demoStopNames = [
  'City of Dreams — Main Lobby',
  'Studio City — Main Lobby',
  'Cotai · Nuwa lobby',
  'Morpheus — Main Lobby',
  'Altira Macau Lobby',
  'Macau International Airport',
  'Taipa Ferry Terminal',
  'Outer Harbour Ferry Terminal',
]
const lobbyByProperty = new Map<string, string>([
  ['City of Dreams', 'City of Dreams — Main Lobby'],
  ['Studio City', 'Studio City — Main Lobby'],
  ['Nuwa', 'Cotai · Nuwa lobby'],
  ['Morpheus', 'Morpheus — Main Lobby'],
  ['Altira', 'Altira Macau Lobby'],
])

const { confirmationLanguage, t } = useLocale()
const fleet = useFleetStore()
const search = ref('')
const tier = ref('All')
const page = ref(1)
const pageSize = ref(20)
const totalItems = ref(0)
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const editingMember = ref<GuestProfileView | null>(null)
const memberProperties = ref<PropertyView[]>([])
const isReadingMember = ref(false)
const successMessage = ref('')
const canEditMembers = computed(() => !isDemoMode && (currentUser.value?.permissions.includes('guests:write') ?? false))
const selectedId = ref('')
const mode = ref<DispatchMode | ''>('')
const couponId = ref('')
const orderKey = ref('')
const vehicleClass = ref<VehicleClass>('Luxury Sedan')
const pickup = ref('')
const dropoff = ref('')
const when = ref('')
const passengers = ref(1)
const luggage = ref(0)
const driverId = ref('')
const notes = ref('')
const formError = ref('')
const createdReference = ref('')
const offerWarning = ref('')
const requests = ref<MemberRequest[]>([])
const liveMembers = ref<Member[]>([])
const stops = ref<Stop[]>(demoStopNames.map((name) => ({ id: name, name, airport: name.includes('Airport') })))
const liveDrivers = ref<DriverOption[]>([])
const couponMap = ref(new Map<string, MemberCoupon[]>())
let requestId = 0
let timer = 0

const demoMembers = ref<Member[]>([
  { guestId: 'MC-100284', memberId: 'MC-100284', name: 'Aria Chen', tier: 'Diamond', room: '8801', property: 'City of Dreams', points: 186400, coupons: [{ couponId: 'WELCOME100', code: 'WELCOME100', name: 'Welcome', vehicleRestriction: null }], inHouse: true },
  { guestId: 'MC-100911', memberId: 'MC-100911', name: 'James Lau', tier: 'Platinum', room: '4210', property: 'Studio City', points: 74200, coupons: [], inHouse: true },
  { guestId: 'MC-101442', memberId: 'MC-101442', name: 'Sofia Ng', tier: 'Gold', room: '1908', property: 'Nuwa', points: 22800, coupons: [{ couponId: 'STUDIO15', code: 'STUDIO15', name: 'Studio', vehicleRestriction: null }], inHouse: true },
  { guestId: 'MC-100055', memberId: 'MC-100055', name: 'Helen Ip', tier: 'VIP', room: '9002', property: 'Morpheus', points: 410000, coupons: [{ couponId: 'LUXURY50', code: 'LUXURY50', name: 'Luxury Sedan', vehicleRestriction: 'LUXURY_SEDAN' }], inHouse: true },
  { guestId: 'MC-099318', memberId: 'MC-099318', name: 'Tom Vasquez', tier: 'Silver', room: '1204', property: 'Altira', points: 6400, coupons: [], inHouse: false },
])

const roster = computed(() => isDemoMode ? filteredDemo.value : liveMembers.value)
const selected = computed(() => roster.value.find((member) => member.guestId === selectedId.value) ?? null)
const selectedCoupon = computed(() => selected.value?.coupons.find((coupon) => coupon.couponId === couponId.value) ?? null)
const lockedClass = computed(() => mode.value === 'Coupon dispatch' ? classFromRestriction(selectedCoupon.value?.vehicleRestriction) : '')
const chosenClass = computed(() => {
  if (mode.value === 'Tier dispatch' && selected.value) return tierClass(selected.value.tier) || vehicleClass.value
  return lockedClass.value || vehicleClass.value
})
const eligibleDrivers = computed(() => isDemoMode ? demoDrivers.value : liveDrivers.value)
const dispatchBlocked = computed(() => {
  const member = selected.value
  if (!member?.inHouse) return true
  if (mode.value === 'Coupon dispatch' && member.coupons.length === 0) return true
  if (mode.value === 'Tier dispatch' && !tierClass(member.tier)) return true
  return false
})
const filteredDemo = computed(() => {
  const query = search.value.trim().toLowerCase()
  return demoMembers.value.filter((member) => {
    const matchesQuery = !query || `${member.memberId} ${member.name} ${member.room}`.toLowerCase().includes(query)
    const matchesTier = tier.value === 'All' || member.tier === tier.value
    return matchesQuery && matchesTier
  })
})
const demoDrivers = computed(() => {
  const property = selected.value?.property ?? ''
  return fleet.drivers
    .filter((driver) => (driver.status === 'Available' || driver.status === 'On duty') && driver.classes.includes(chosenClass.value))
    .map((driver) => {
      const shift = fleet.shifts.find((item) => item.staffId === driver.staffId && item.date === macauDayKey(0) && item.status === 'On duty')
      return { driverId: driver.staffId, name: driver.name, property: driver.property, vehicle: shift?.plate ?? '' }
    })
    .sort((left, right) => Number(right.property === property) - Number(left.property === property))
})
const metrics = computed(() => [
  { label: 'Coupon holders', value: roster.value.filter((member) => member.coupons.length > 0).length, note: 'members' },
  { label: 'Tier dispatch', value: roster.value.filter((member) => tierClass(member.tier)).length, note: 'eligible' },
])

function tierClass(level: Tier): VehicleClass | '' {
  if (level === 'VIP' || level === 'Diamond') return 'Luxury Sedan'
  if (level === 'Platinum') return 'Luxury MPV'
  if (level === 'Gold') return 'Luxury Sedan'
  return ''
}
function classFromRestriction(value?: string | null): VehicleClass | '' {
  const text = value?.toUpperCase() ?? ''
  if (text === 'BUSINESS_SEDAN') return 'Standard Sedan'
  if (text.includes('MAYBACH')) return 'Luxury Sedan'
  if (text.includes('MPV')) return 'Luxury MPV'
  if (text.includes('SEDAN')) return 'Luxury Sedan'
  return ''
}
function sameId(left: string, right: string): boolean {
  return left.trim().toLowerCase() === right.trim().toLowerCase()
}
function usableCoupon(coupon: CouponView): boolean {
  if (coupon.status !== 'ACTIVE' || coupon.redeemedCount >= coupon.issuedQuantity) return false
  const today = macauDayKey(0)
  return coupon.validFrom <= today && coupon.validTo >= today
}
function couponsFor(memberId: string, guestId: string): MemberCoupon[] {
  for (const [key, coupons] of couponMap.value) {
    if (sameId(key, memberId) || sameId(key, guestId)) return coupons
  }
  return []
}
function applyCoupons(): void {
  liveMembers.value = liveMembers.value.map((member) => ({ ...member, coupons: couponsFor(member.memberId, member.guestId) }))
}
function previewBlock(preview: BookingPreviewView): string {
  if (preview.canSubmit) return ''
  const reasons = preview.blockedReasons.filter((reason) => !`${reason.code} ${reason.field} ${reason.message}`.toLowerCase().includes('point'))
  if (reasons.length === 0) return ''
  return reasons.map((reason) => reason.message).filter(Boolean).join(' ') || 'This booking cannot be submitted.'
}
function apiVehicle(value: string): CreateTripRequest['vehicleClass'] {
  if (value === 'Standard Sedan') return 'BUSINESS_SEDAN'
  if (value === 'Luxury MPV') return 'LUXURY_MPV'
  return 'LUXURY_SEDAN'
}

async function loadCoupons(): Promise<void> {
  const collected: CouponView[] = []
  let pageNo = 1
  let pages = 1
  while (pageNo <= pages && pageNo <= 20) {
    const envelope = await dispatchApi.coupons({ page: pageNo, pageSize: 50, status: 'ACTIVE' })
    collected.push(...(envelope.data ?? []))
    pages = envelope.meta?.pagination?.totalPages ?? 1
    pageNo += 1
  }
  const next = new Map<string, MemberCoupon[]>()
  collected.filter(usableCoupon).forEach((coupon) => {
    const row = { couponId: coupon.couponId, code: coupon.code, name: coupon.name, vehicleRestriction: coupon.vehicleRestriction }
    const key = coupon.memberId.trim().toLowerCase()
    next.set(key, [...(next.get(key) ?? []), row])
  })
  couponMap.value = next
  applyCoupons()
}
async function loadStops(): Promise<void> {
  const locations = (await lms.locations()).map(mapLocation)
  stops.value = locations.map((location) => ({ id: location.locationId, name: location.name, airport: location.category === 'Airport' }))
}
async function loadLiveDrivers(): Promise<void> {
  memberProperties.value = await lms.properties()
  const envelope = await lms.listDrivers({ page: 1, pageSize: 50, view: 'AVAILABLE' })
  liveDrivers.value = (envelope.data ?? []).map((item) => {
    const driver = mapDriver({ ...item, languages: item.languages ?? [] })
    return { driverId: driver.driverId, name: driver.fullName, property: driver.propertyName, vehicle: driver.vehicleName }
  })
}
async function loadMembers(): Promise<void> {
  if (isDemoMode) return
  const current = ++requestId
  isLoading.value = true
  loadError.value = ''
  try {
    const envelope = await lms.listGuestPage({
      page: page.value,
      pageSize: pageSize.value,
      search: search.value.trim() || undefined,
      tier: tier.value === 'All' ? undefined : tier.value.toUpperCase(),
    })
    if (current !== requestId) return
    liveMembers.value = (envelope.data ?? []).map((guest) => {
      const mapped = mapGuest(guest)
      return { guestId: mapped.guestId, memberId: mapped.memberId ?? '', name: mapped.fullName, tier: mapped.tier, room: mapped.roomNumber, property: guest.propertyId ?? '', points: 0, coupons: couponsFor(mapped.memberId ?? '', mapped.guestId), inHouse: mapped.isInHouse }
    })
    totalItems.value = envelope.meta?.pagination?.totalItems ?? liveMembers.value.length
  } catch (cause) {
    if (current !== requestId) return
    liveMembers.value = []
    totalItems.value = 0
    loadError.value = apiErrorMessage(cause)
  } finally {
    if (current === requestId) isLoading.value = false
  }
}
async function openMemberEdit(guestId: string): Promise<void> {
  if (!canEditMembers.value || isReadingMember.value) return
  isReadingMember.value = true
  loadError.value = ''; successMessage.value = ''
  try {
    const [profile, properties] = await Promise.all([lms.guestProfile(guestId), lms.properties()])
    memberProperties.value = properties
    editingMember.value = profile
  } catch (cause) { loadError.value = apiErrorMessage(cause) }
  finally { isReadingMember.value = false }
}
async function memberSaved(): Promise<void> {
  editingMember.value = null
  successMessage.value = 'Member updated.'
  await loadMembers()
}
function memberPropertyName(value: string): string {
  return memberProperties.value.find(property => property.propertyId === value)?.name ?? value
}
function schedule(): void {
  window.clearTimeout(timer)
  timer = window.setTimeout(() => { void loadMembers() }, 300)
}

async function openDispatch(next: DispatchMode): Promise<void> {
  if (!isDemoMode && next === 'Coupon dispatch') {
    try { await loadCoupons() } catch (cause) { formError.value = apiErrorMessage(cause); mode.value = next; return }
  }
  const member = selected.value
  if (!member?.inHouse) { formError.value = 'Only an in-house member can be dispatched.'; return }
  if (next === 'Coupon dispatch' && !member.memberId) { formError.value = 'This member has no member ID, so coupons cannot be matched.'; return }
  if (next === 'Coupon dispatch' && member.coupons.length === 0) { formError.value = 'This member has no coupon.'; return }
  if (next === 'Tier dispatch' && !tierClass(member.tier)) { formError.value = 'This tier does not include a complimentary vehicle.'; return }
  formError.value = ''
  orderKey.value = crypto.randomUUID()
  mode.value = next
  couponId.value = member.coupons[0]?.couponId ?? ''
  vehicleClass.value = tierClass(member.tier) || 'Luxury Sedan'
  const home = lobbyByProperty.get(memberPropertyName(member.property))
  pickup.value = stops.value.find((stop) => stop.id === home || stop.name === home)?.id ?? stops.value[0]?.id ?? ''
  dropoff.value = stops.value.find((stop) => stop.id !== pickup.value)?.id ?? ''
  when.value = ''
  passengers.value = 1
  luggage.value = 0
  driverId.value = ''
  notes.value = ''
}
function closeDispatch(): void {
  mode.value = ''
  formError.value = ''
}
async function saveRequest(): Promise<void> {
  const member = selected.value
  if (!member || !mode.value || isSaving.value) return
  const origin = stops.value.find((stop) => stop.id === pickup.value)
  const destination = stops.value.find((stop) => stop.id === dropoff.value)
  if (!origin) { formError.value = 'Pickup location is required.'; return }
  if (!destination) { formError.value = 'Drop-off location is required.'; return }
  if (origin.id === destination.id) { formError.value = 'Pickup and drop-off must be different.'; return }
  if (!when.value || Number.isNaN(new Date(when.value).getTime())) { formError.value = 'Pickup time is required.'; return }
  if (!Number.isInteger(passengers.value) || passengers.value < 1) { formError.value = 'Passenger count is required.'; return }
  const coupon = member.coupons.find((item) => item.couponId === couponId.value)
  if (mode.value === 'Coupon dispatch' && !coupon) { formError.value = 'This member has no coupon.'; return }
  const driver = eligibleDrivers.value.find((item) => item.driverId === driverId.value)
  if (!driver && eligibleDrivers.value.length > 0) { formError.value = 'Select a driver.'; return }
  if (isDemoMode) {
    requests.value.unshift({ id: `${Date.now()}`, memberId: member.memberId, name: member.name, mode: mode.value, coupon: coupon?.code ?? '', vehicleClass: chosenClass.value, pickup: origin.name, dropoff: destination.name, when: when.value.replace('T', ' '), passengers: passengers.value, luggage: Math.max(0, luggage.value), driverName: driver?.name ?? '', notes: notes.value.trim() })
    closeDispatch()
    selectedId.value = ''
    return
  }
  isSaving.value = true
  formError.value = ''
  try {
    const profile = mapGuestProfile(await lms.guestProfile(member.guestId))
    const propertyId = profile.propertyId || currentUser.value?.propertyId || ''
    if (!propertyId) { formError.value = 'A property is required before this order can be created.'; return }
    const request: CreateTripRequest = {
      guestId: member.guestId,
      propertyId,
      pickupLocationId: origin.id,
      dropoffLocationId: destination.id,
      pickupAt: new Date(when.value).toISOString(),
      pickupMode: 'SCHEDULED',
      serviceType: origin.airport ? 'AIRPORT_PICKUP' : destination.airport ? 'AIRPORT_DROPOFF' : 'ONE_WAY',
      vehicleClass: apiVehicle(chosenClass.value),
      passengers: passengers.value,
      luggage: Math.max(0, luggage.value),
      specialRequests: [mode.value === 'Coupon dispatch' && coupon ? `Coupon ${coupon.code} ${coupon.name}` : '', mode.value === 'Tier dispatch' ? `Tier ${member.tier}` : '', mode.value === 'Direct dispatch' ? 'Direct dispatch' : '', notes.value.trim()].filter(Boolean).join(' · '),
      confirmationLanguage: confirmationLanguage.value,
      couponId: mode.value === 'Coupon dispatch' ? coupon?.couponId : undefined,
    }
    const preview = await lms.previewBooking(request)
    const block = previewBlock(preview)
    if (block) { formError.value = block; return }
    const trip = await lms.createTrip(request, orderKey.value)
    createdReference.value = trip.tripReference || trip.tripId
    offerWarning.value = ''
    if (driver) {
      try {
        const order = await lms.dispatchOrder(trip.tripId)
        if (order.canDispatch) await lms.createDispatchOffer(trip.tripId, driver.driverId, order.version)
        else offerWarning.value = order.blockedReason || 'The order is on the dispatch list, but a driver cannot be invited yet.'
      } catch (cause) { offerWarning.value = apiErrorMessage(cause) }
    }
    if (request.couponId) void loadCoupons()
    closeDispatch()
    selectedId.value = ''
  } catch (cause) { formError.value = apiErrorMessage(cause) }
  finally { isSaving.value = false }
}
function resetFilters(): void {
  search.value = ''
  tier.value = 'All'
}

watch(chosenClass, () => {
  if (!eligibleDrivers.value.some((driver) => driver.driverId === driverId.value)) driverId.value = ''
})
watch([search, tier], () => {
  if (isDemoMode) return
  if (page.value !== 1) page.value = 1
  else schedule()
})
watch([page, pageSize], () => { if (!isDemoMode) schedule() }, { immediate: true })
onMounted(() => {
  if (isDemoMode) return
  void loadCoupons().catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) })
  void loadStops().catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) })
  void loadLiveDrivers().catch((cause: unknown) => { loadError.value = apiErrorMessage(cause) })
})
</script>

<template>
  <p v-if="successMessage" class="panel-note" role="status">{{ t(successMessage) }}</p>
  <p v-if="createdReference" class="panel-note">{{ t('The order is on the dispatch list.') }} {{ createdReference }}</p>
  <p v-if="offerWarning" class="form-error" role="alert">{{ t(offerWarning) }}</p>
  <p v-if="loadError" class="form-error" role="alert">{{ loadError }} <button class="text-button" @click="loadMembers()">{{ t('Retry') }}</button></p>
  <div class="ops-metrics cols-2">
    <article v-for="metric in metrics" :key="metric.label" class="ops-metric">
      <span>{{ t(metric.label) }}</span>
      <strong>{{ metric.value }}</strong>
      <small>{{ t(metric.note) }}</small>
    </article>
  </div>

  <section class="panel">
    <div class="panel-heading"><h2>{{ t('Member list') }} <span class="count-badge">{{ isDemoMode ? roster.length : totalItems }}</span></h2></div>
    <div class="history-filters">
      <label class="field-label expand-field">{{ t('NAME / MEMBER ID / ROOM') }}<input v-model="search" :placeholder="t('Search member name, member ID or room')" /></label>
      <label class="field-label">{{ t('TIER') }}<select v-model="tier"><option value="All">{{ t('All tiers') }}</option><option value="Silver">{{ t('Silver') }}</option><option value="Gold">{{ t('Gold') }}</option><option value="Platinum">{{ t('Platinum') }}</option><option value="Diamond">{{ t('Diamond') }}</option><option value="VIP">{{ t('VIP') }}</option></select></label>
      <button class="secondary-button" @click="resetFilters">{{ t('RESET') }}</button>
    </div>
    <div class="table-scroll">
      <table class="data-table console-table" :aria-label="t('Member Information')">
        <thead>
          <tr>
            <th scope="col">{{ t('MEMBER') }}</th>
            <th scope="col">{{ t('TIER') }}</th>
            <th scope="col">{{ t('ROOM') }}</th>
            <th scope="col">{{ t('PROPERTY') }}</th>
            <th scope="col">{{ t('COUPONS') }}</th>
            <th scope="col">{{ t('TIER VEHICLE') }}</th>
            <th scope="col"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="isLoading && roster.length === 0"><td colspan="7" class="empty-cell">{{ t('Loading members') }}</td></tr>
          <tr v-for="member in roster" :key="member.guestId">
            <td><strong>{{ member.name }}</strong><small>{{ member.memberId || member.guestId }}</small></td>
            <td><span class="status-tag tier-tag">{{ t(member.tier) }}</span></td>
            <td>{{ member.room || '—' }}</td>
            <td>{{ member.property ? memberPropertyName(member.property) : '—' }}</td>
            <td>{{ member.coupons.length ? member.coupons.map((coupon) => coupon.code).join(', ') : t('None') }}</td>
            <td>{{ tierClass(member.tier) ? t(tierClass(member.tier)) : t('Not included') }}</td>
            <td class="table-actions"><button @click="selectedId = member.guestId">{{ t('Dispatch →') }}</button><button v-if="canEditMembers" :disabled="isReadingMember" @click="openMemberEdit(member.guestId)">{{ t('Edit') }}</button></td>
          </tr>
          <tr v-if="!isLoading && roster.length === 0"><td colspan="7" class="empty-cell">{{ t('No matching members') }}</td></tr>
        </tbody>
      </table>
    </div>
    <div class="panel-note">{{ isDemoMode ? t('Layout sample · not connected to LMS') : t('Live member list from LMS') }}</div>
    <DataPager v-if="!isDemoMode" v-model:page="page" v-model:page-size="pageSize" :total-items="totalItems" />
  </section>

  <ConsoleMemberEditor v-if="editingMember" :key="editingMember.guestId" :member="editingMember" :properties="memberProperties" @close="editingMember = null" @saved="memberSaved" />

  <section v-if="isDemoMode && requests.length > 0" class="panel">
    <div class="panel-heading"><h2>{{ t('Dispatch drafts') }} <span class="count-badge">{{ requests.length }}</span></h2></div>
    <div class="table-scroll">
      <table class="data-table console-table" :aria-label="t('Dispatch drafts')">
        <thead><tr><th scope="col">{{ t('MEMBER') }}</th><th scope="col">{{ t('MODE') }}</th><th scope="col">{{ t('ROUTE / TIME') }}</th><th scope="col">{{ t('Passengers / Luggage') }}</th><th scope="col">{{ t('Driver') }}</th><th scope="col">{{ t('Notes') }}</th></tr></thead>
        <tbody>
          <tr v-for="request in requests" :key="request.id">
            <td><strong>{{ request.name }}</strong><small>{{ request.memberId }}</small></td>
            <td>{{ t(request.mode) }}<small>{{ request.coupon || t(request.vehicleClass) }}</small></td>
            <td>{{ request.pickup }} → {{ request.dropoff }}<small>{{ request.when }}</small></td>
            <td>{{ request.passengers }} / {{ request.luggage }}</td>
            <td>{{ request.driverName || '—' }}</td>
            <td>{{ request.notes || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <div v-if="selected && !mode" class="modal-backdrop" @click.self="selectedId = ''">
    <section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Member dispatch')">
      <header class="dialog-header"><h2>{{ selected.name }}</h2><button class="icon-button" :aria-label="t('Close')" @click="selectedId = ''"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ selected.memberId || selected.guestId }}</strong> · {{ t(selected.tier) }} · {{ t('Room') }} {{ selected.room || '—' }}</p>
        <p v-if="selected.property || selected.points">{{ memberPropertyName(selected.property) }}<template v-if="selected.points"> · {{ selected.points.toLocaleString() }} {{ t('points') }}</template></p>
        <p>{{ t('Coupons') }}: {{ selected.coupons.length ? selected.coupons.map((coupon) => `${coupon.code} · ${coupon.name}`).join(', ') : t('None') }}</p>
        <p v-if="!selected.memberId" class="muted">{{ t('This member has no member ID, so coupons cannot be matched.') }}</p>
        <p>{{ t('Tier vehicle') }}: {{ tierClass(selected.tier) ? t(tierClass(selected.tier)) : t('Not included') }}</p>
        <p v-if="formError" class="form-error">{{ t(formError) }}</p>
        <div class="member-modes">
          <button class="secondary-button" :disabled="!selected.inHouse" @click="openDispatch('Direct dispatch')">{{ t('Direct dispatch') }}</button>
          <button class="secondary-button" :disabled="!selected.inHouse" @click="openDispatch('Coupon dispatch')">{{ t('Coupon dispatch') }}</button>
          <button class="secondary-button" :disabled="!selected.inHouse || !tierClass(selected.tier)" @click="openDispatch('Tier dispatch')">{{ t('Tier dispatch') }}</button>
        </div>
      </div>
      <footer class="dialog-footer"><button class="secondary-button" @click="selectedId = ''">{{ t('CLOSE') }}</button></footer>
    </section>
  </div>

  <div v-if="selected && mode" class="modal-backdrop" @click.self="closeDispatch">
    <section class="dialog member-dispatch-dialog" role="dialog" aria-modal="true" :aria-label="t(mode)">
      <header class="dialog-header"><h2>{{ t(mode) }}</h2><button class="icon-button" :aria-label="t('Close')" @click="closeDispatch"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ selected.name }}</strong> · {{ t(selected.tier) }} · {{ t('Room') }} {{ selected.room || '—' }}</p>
        <div class="member-dispatch-grid">
          <label v-if="mode === 'Coupon dispatch'" class="field-label">{{ t('Coupon') }}<select v-model="couponId"><option v-for="coupon in selected.coupons" :key="coupon.couponId" :value="coupon.couponId">{{ coupon.code }} · {{ coupon.name }}</option></select></label>
          <label v-if="mode === 'Tier dispatch' || lockedClass" class="field-label">{{ t('Vehicle class') }}<input :value="t(chosenClass)" readonly /></label>
          <label v-else class="field-label">{{ t('Vehicle class') }}<select v-model="vehicleClass"><option value="Standard Sedan">{{ t('Standard Sedan') }}</option><option value="Luxury Sedan">{{ t('Luxury Sedan') }}</option><option value="Luxury MPV">{{ t('Luxury MPV') }}</option></select></label>
          <label class="field-label">{{ t('Pickup location') }}<select v-model="pickup"><option v-if="stops.length === 0" value="">{{ t('No locations are available.') }}</option><option v-for="stop in stops" :key="stop.id" :value="stop.id">{{ stop.name }}</option></select></label>
          <label class="field-label">{{ t('Drop-off location') }}<select v-model="dropoff"><option v-for="stop in stops" :key="stop.id" :value="stop.id">{{ stop.name }}</option></select></label>
          <label class="field-label">{{ t('Pickup time') }}<input v-model="when" type="datetime-local" /></label>
          <label class="field-label">{{ t('Passengers') }}<input v-model.number="passengers" type="number" min="1" max="7" /></label>
          <label class="field-label">{{ t('Luggage pieces') }}<input v-model.number="luggage" type="number" min="0" max="12" /></label>
          <label class="field-label span-2">{{ t('Available driver') }}
            <select v-model="driverId">
              <option value="">{{ eligibleDrivers.length ? t('Select a driver') : t('No driver is available for this vehicle class.') }}</option>
              <option v-for="driver in eligibleDrivers" :key="driver.driverId" :value="driver.driverId">{{ driver.name }} · {{ driver.property }}{{ driver.vehicle ? ` · ${driver.vehicle}` : '' }}</option>
            </select>
          </label>
          <label class="field-label span-2">{{ t('Notes') }}<textarea v-model="notes" rows="3" :placeholder="t('Notes')"></textarea></label>
        </div>
        <p v-if="formError" class="form-error">{{ t(formError) }}</p>
        <p class="muted">{{ isDemoMode ? t('Saved in this browser session. No request is sent.') : t('Creating this order sends it to the dispatch list.') }}</p>
      </div>
      <footer class="dialog-footer"><button class="secondary-button" @click="closeDispatch">{{ t('CLOSE') }}</button><button class="primary-button" :disabled="dispatchBlocked || isSaving" @click="saveRequest">{{ t(isDemoMode ? 'Save draft' : 'Create order') }}</button></footer>
    </section>
  </div>
</template>
