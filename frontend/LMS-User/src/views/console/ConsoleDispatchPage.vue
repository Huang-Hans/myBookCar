<script setup lang="ts">
import { useLocale } from '../../i18n'
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { X } from 'lucide-vue-next'
import { lms, mapTrip, type DispatchOfferView, type DispatchOrderView, type DispatchOrderSummaryView, type PropertyView } from '../../api/lms'
import { dispatchApi } from '../../api/dispatch'
import { shiftBoardDriverOptions } from '../../data/dispatchShiftDrivers'
import { apiErrorMessage, type SuccessEnvelope } from '../../api/http'
import { isDemoMode } from '../../api/session'
import { subscribeBookingCreated, subscribeStaffNotificationConnected } from '../../api/notifications'
import type { NotificationMessageV1 } from '../../api/notificationContract'
import { useRoute } from 'vue-router'

const { locale, dateLocale, t } = useLocale()
const route = useRoute()

type DispatchState = 'PENDING' | 'AWAITING_RESPONSE' | 'DRIVER_ASSIGNED' | 'NEEDS_REDISPATCH' | 'COMPLETED' | 'CANCELLED'
type Channel = 'Customer' | 'Staff booking'

interface DispatchOrder {
  tripId: string
  version: number
  canDispatch: boolean
  reference: string
  bookedAt: string
  guestName: string
  tier: string
  room: string
  memberId: string
  channel: Channel
  channelDetail: string
  pickup: string
  dropoff: string
  pickupTime: string
  pickupMode: string
  vehicle: string
  passengers: number
  bags: number
  status: DispatchState
  statusDetail: string
  property: string
  service: string
  orderStatus: string
  notes: string
  departure: string
}

interface DriverOption {
  id: string
  name: string
  vehicle: string
  plate: string
  availability: string
}

const pageSize = 10
const demoDrivers: DriverOption[] = [
  { id: 'daniel-ho', name: 'Daniel Ho', vehicle: 'Luxury MPV', plate: 'MR-28-66', availability: 'Available' },
  { id: 'eric-chan', name: 'Eric Chan', vehicle: 'Luxury MPV', plate: 'MR-16-82', availability: 'Available' },
]

type DemoOrder = Omit<DispatchOrder, 'tripId' | 'version' | 'canDispatch' | 'status'> & { status: DispatchState | 'PENDING_CONFIRMATION' }
const pictured: DemoOrder[] = [
  { reference: 'TR-20261012-048', bookedAt: '12 Oct · 14:20', guestName: 'Alexander Chen', tier: 'Diamond', room: '1808', memberId: 'MC00862941', channel: 'Customer', channelDetail: 'Customer Web', pickup: 'City of Dreams · Nuwa', dropoff: 'Macau International Airport', pickupTime: '14:45', pickupMode: 'Today · ASAP', vehicle: 'Luxury MPV', passengers: 3, bags: 2, status: 'PENDING', statusDetail: 'Confirmed', property: 'City of Dreams', service: 'Complimentary', orderStatus: 'Confirmed', notes: 'Guest requests a child seat.', departure: '12 Oct 2026 · 14:45' },
  { reference: 'TR-20261012-047', bookedAt: '12 Oct · 14:12', guestName: 'Vivian Wong', tier: 'VIP', room: '2201', memberId: 'MC00861102', channel: 'Staff booking', channelDetail: 'Sophie Leong', pickup: 'Studio City', dropoff: 'Taipa Ferry Terminal', pickupTime: '15:00', pickupMode: 'Today · Scheduled', vehicle: 'Luxury Sedan', passengers: 2, bags: 1, status: 'AWAITING_RESPONSE', statusDetail: 'Daniel Ho · 00:45', property: 'Studio City', service: 'Complimentary', orderStatus: 'Confirmed', notes: '', departure: '12 Oct 2026 · 15:00' },
  { reference: 'TR-20261012-046', bookedAt: '12 Oct · 13:58', guestName: 'James Liu', tier: 'Diamond', room: '1608', memberId: 'MC00859014', channel: 'Customer', channelDetail: 'Customer Web', pickup: 'City of Dreams · Morpheus', dropoff: 'Studio City', pickupTime: '15:10', pickupMode: 'Today · Scheduled', vehicle: 'Luxury MPV', passengers: 4, bags: 3, status: 'NEEDS_REDISPATCH', statusDetail: 'Previous invitation expired', property: 'City of Dreams', service: 'Complimentary', orderStatus: 'Confirmed', notes: '', departure: '12 Oct 2026 · 15:10' },
  { reference: 'TR-20261012-045', bookedAt: '12 Oct · 13:40', guestName: 'Sarah Zhang', tier: 'VIP', room: '1612', memberId: 'MC00857720', channel: 'Staff booking', channelDetail: 'Eric Chan', pickup: 'Altira Macau', dropoff: 'Border Gate', pickupTime: '14:30', pickupMode: 'Today · Scheduled', vehicle: 'Maybach', passengers: 1, bags: 1, status: 'DRIVER_ASSIGNED', statusDetail: 'David Chan · MS-18-08', property: 'Altira Macau', service: 'Complimentary', orderStatus: 'Confirmed', notes: '', departure: '12 Oct 2026 · 14:30' },
  { reference: 'TR-20261012-044', bookedAt: '12 Oct · 13:22', guestName: 'Michael Lam', tier: 'Diamond', room: '1903', memberId: 'MC00854118', channel: 'Customer', channelDetail: 'Customer Web', pickup: 'City of Dreams · Nuwa', dropoff: 'Macau International Airport', pickupTime: '16:00', pickupMode: 'Today · Scheduled', vehicle: 'Luxury Sedan', passengers: 2, bags: 2, status: 'PENDING_CONFIRMATION', statusDetail: 'Waitlisted · dispatch blocked', property: 'City of Dreams', service: 'Complimentary', orderStatus: 'Confirmed', notes: '', departure: '12 Oct 2026 · 16:00' },
]

const fillerNames = ['Helen Chao', 'Peter Ng', 'Ivy Fong', 'Kevin Tam', 'Grace Lei', 'Brian Sou', 'Nina Cheung', 'Oscar Kuok', 'Paula Vong', 'Ryan Ip', 'Cindy Mak', 'Leo Wong', 'Mandy Si', 'Tony Lao', 'Yuki Chan', 'Felix Ao', 'Rita Hong', 'Sam Pou', 'Una Lok', 'Will Iao', 'Zoe Un']
const fillerRoutes = [
  ['City of Dreams · Nuwa', 'Macau International Airport', 'City of Dreams'],
  ['Studio City', 'Taipa Ferry Terminal', 'Studio City'],
  ['Altira Macau', 'Border Gate', 'Altira Macau'],
  ['City of Dreams · Morpheus', 'Studio City', 'City of Dreams'],
] as const
const fillerPlan: DispatchState[] = [
  ...Array.from({ length: 7 }, () => 'PENDING' as const),
  ...Array.from({ length: 2 }, () => 'AWAITING_RESPONSE' as const),
  ...Array.from({ length: 11 }, () => 'DRIVER_ASSIGNED' as const),
  'NEEDS_REDISPATCH',
]

function fillerOrder(status: DemoOrder['status'], index: number): DemoOrder {
  const route = fillerRoutes[index % fillerRoutes.length]!
  const hour = 10 + (index % 8)
  const minute = (index * 7) % 60
  const time = `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`
  return {
    reference: `TR-20261012-${String(43 - index).padStart(3, '0')}`,
    bookedAt: `12 Oct · ${time}`,
    guestName: fillerNames[index] ?? 'Guest',
    tier: index % 2 === 0 ? 'Diamond' : 'VIP',
    room: String(1200 + index),
    memberId: `MC0086${String(1000 + index)}`,
    channel: index % 3 === 0 ? 'Staff booking' : 'Customer',
    channelDetail: index % 3 === 0 ? 'Sophie Leong' : 'Customer Web',
    pickup: route[0],
    dropoff: route[1],
    pickupTime: time,
    pickupMode: 'Today · Scheduled',
    vehicle: index % 2 === 0 ? 'Luxury MPV' : 'Luxury Sedan',
    passengers: (index % 4) + 1,
    bags: index % 3,
    status,
    statusDetail: status === 'DRIVER_ASSIGNED' ? 'David Chan · MS-18-08' : status === 'AWAITING_RESPONSE' ? 'Daniel Ho · 00:45' : status === 'NEEDS_REDISPATCH' ? 'Previous invitation expired' : status === 'PENDING_CONFIRMATION' ? 'Waitlisted · dispatch blocked' : 'Confirmed',
    property: route[2],
    service: 'Complimentary',
    orderStatus: 'Confirmed',
    notes: '',
    departure: `12 Oct 2026 · ${time}`,
  }
}

function demoOrders(): DispatchOrder[] {
  return [...pictured, ...fillerPlan.map(fillerOrder)].map((order) => ({
    ...order,
    status: boardStatus(order.status),
    orderStatus: order.status === 'PENDING_CONFIRMATION' ? 'Pending confirmation' : order.orderStatus,
    statusDetail: order.status === 'PENDING_CONFIRMATION' ? 'Pending confirmation' : order.statusDetail,
    tripId: order.reference,
    version: 0,
    canDispatch: order.status === 'PENDING' || order.status === 'NEEDS_REDISPATCH' || order.status === 'PENDING_CONFIRMATION',
  }))
}

function vehicleLabel(code: string): string {
  if (code === 'BUSINESS_SEDAN') return 'Standard Sedan'
  if (code === 'LUXURY_SEDAN') return 'Luxury Sedan'
  if (code === 'LUXURY_MPV') return 'Luxury MPV'
  if (code === 'MAYBACH') return 'Maybach'
  return code.replaceAll('_', ' ')
}
function serviceLabel(code: string): string {
  if (code === 'ONE_WAY') return 'Complimentary'
  if (code === 'AIRPORT_PICKUP') return 'Airport pickup'
  if (code === 'AIRPORT_DROPOFF') return 'Airport transfer'
  if (code === 'INTER_PROPERTY') return 'Property transfer'
  return code.replaceAll('_', ' ')
}
function orderStatusLabel(code: string, fallback: string): string {
  if (code === 'PENDING') return 'Pending confirmation'
  if (code === 'CONFIRMED') return 'Confirmed'
  if (code === 'DRIVER_ASSIGNED') return 'Driver assigned'
  if (code === 'DRIVER_EN_ROUTE') return 'Driver En Route'
  if (code === 'ARRIVED_AT_PICKUP') return 'Arrived at Pickup'
  if (code === 'IN_PROGRESS') return 'In Progress'
  if (code === 'COMPLETED') return 'Completed'
  if (code === 'CANCELLED') return 'Cancelled'
  return fallback
}

const orders = ref<DispatchOrder[]>(isDemoMode ? demoOrders() : [])
const drivers = ref<DriverOption[]>(isDemoMode ? demoDrivers : [])
const offerHistory = ref<DispatchOfferView[]>([])
const propertyOptions = ref<PropertyView[]>([])
const sourceOrders = ref<DispatchOrderView[]>([])
const orderSummary = ref<DispatchOrderSummaryView>({ total: 0, counts: {} })
const isSending = ref(false)
const isDriversLoading = ref(false)
const inviteError = ref('')
const tab = ref<'ALL' | DispatchState>('ALL')
const search = ref('')
const property = ref('')
const channel = ref<'' | Channel>('')
const page = ref(1)
const orderTotalItems = ref(0)
const orderTotalPages = ref(1)
const selected = ref<DispatchOrder | null>(null)
const selectedDriverId = ref(drivers.value[0]?.id ?? '')
const isLoading = ref(false)
const loadError = ref('')

function statusLabel(status: DispatchState): string {
  if (status === 'PENDING') return 'Unassigned'
  if (status === 'AWAITING_RESPONSE') return 'Awaiting response'
  if (status === 'DRIVER_ASSIGNED') return 'Driver assigned'
  if (status === 'NEEDS_REDISPATCH') return 'Needs redispatch'
  if (status === 'COMPLETED') return 'Completed'
  if (status === 'CANCELLED') return 'Cancelled'
  return 'Pending confirmation'
}
function actionLabel(status: DispatchState, canDispatch: boolean): { label: string; detail: string } {
  if (status === 'PENDING') return canDispatch ? { label: 'Dispatch →', detail: 'View details' } : { label: 'View →', detail: 'Awaiting confirmation' }
  if (status === 'AWAITING_RESPONSE') return { label: 'View →', detail: 'Driver response pending' }
  if (status === 'DRIVER_ASSIGNED') return { label: 'View →', detail: 'Driver assignment' }
  if (status === 'NEEDS_REDISPATCH') return { label: 'Redispatch →', detail: 'Choose another driver' }
  if (status === 'COMPLETED' || status === 'CANCELLED') return { label: 'View →', detail: 'View details' }
  return { label: 'View →', detail: 'Awaiting confirmation' }
}

const cards = [
  { status: 'PENDING' as const, title: 'Unassigned', detail: 'Awaiting driver assignment', tone: 'unassigned' },
  { status: 'AWAITING_RESPONSE' as const, title: 'Awaiting Response', detail: 'Invitation sent to driver', tone: 'awaiting' },
  { status: 'DRIVER_ASSIGNED' as const, title: 'Driver Assigned', detail: 'Accepted and preparing pickup', tone: 'assigned' },
  { status: 'NEEDS_REDISPATCH' as const, title: 'Needs Redispatch', detail: 'Declined or timed out', tone: 'redispatch' },
]
const tabs = [
  { id: 'ALL' as const, label: 'All' },
  { id: 'PENDING' as const, label: 'Unassigned' },
  { id: 'AWAITING_RESPONSE' as const, label: 'Awaiting' },
  { id: 'DRIVER_ASSIGNED' as const, label: 'Assigned' },
  { id: 'NEEDS_REDISPATCH' as const, label: 'Redispatch' },
  { id: 'COMPLETED' as const, label: 'Completed' },
  { id: 'CANCELLED' as const, label: 'Cancelled' },
]

const properties = computed(() => propertyOptions.value.length ? propertyOptions.value.map((item) => item.name) : [...new Set(orders.value.map((order) => order.property).filter(Boolean))])
const counts = computed(() => {
  const tally = Object.fromEntries(tabs.map((item) => [item.id, 0])) as Record<typeof tabs[number]['id'], number>
  if (!isDemoMode) {
    tally.ALL = orderSummary.value.total
    for (const [status, count] of Object.entries(orderSummary.value.counts)) tally[boardStatus(status)] += count
    return tally
  }
  tally.ALL = orders.value.length
  for (const order of orders.value) tally[order.status] += 1
  return tally
})
const filtered = computed(() => {
  const query = search.value.trim().toLocaleLowerCase()
  return orders.value.filter((order) => {
    const matchesTab = tab.value === 'ALL' || order.status === tab.value
    const matchesProperty = !property.value || order.property === property.value
    const matchesChannel = !channel.value || order.channel === channel.value
    const matchesQuery = !query || `${order.reference} ${order.guestName} ${order.memberId} ${order.room}`.toLocaleLowerCase().includes(query)
    return matchesTab && matchesProperty && matchesChannel && matchesQuery
  })
})
const shownTotal = computed(() => isDemoMode ? filtered.value.length : orderTotalItems.value)
const pageCount = computed(() => isDemoMode ? Math.max(1, Math.ceil(filtered.value.length / pageSize)) : Math.max(1, orderTotalPages.value))
const visibleOrders = computed(() => isDemoMode ? filtered.value.slice((page.value - 1) * pageSize, page.value * pageSize) : orders.value)
const rangeStart = computed(() => shownTotal.value === 0 ? 0 : (page.value - 1) * pageSize + 1)
const rangeEnd = computed(() => {
  if (shownTotal.value === 0) return 0
  if (isDemoMode) return Math.min(filtered.value.length, page.value * pageSize)
  return Math.min(orderTotalItems.value, (page.value - 1) * pageSize + orders.value.length)
})
const selectedDriver = computed(() => drivers.value.find((driver) => driver.id === selectedDriverId.value) ?? null)
const canInvite = computed(() => Boolean(selected.value?.canDispatch && selected.value.tripId && selectedDriver.value && !isSending.value && !isDriversLoading.value))

let suspendOrderWatch = false
watch([tab, search, property, channel], () => {
  if (suspendOrderWatch || isDemoMode) return
  if (page.value !== 1) page.value = 1
  else scheduleOrders()
})
watch(page, () => { if (suspendOrderWatch || isDemoMode) return; void loadOrders() })
watch(pageCount, (count) => { if (isDemoMode && page.value > count) page.value = count })

function countOf(status: DispatchState): string {
  return String(counts.value[status]).padStart(2, '0')
}
function formatStamp(value: string, options: Intl.DateTimeFormatOptions): string {
  return value && !Number.isNaN(Date.parse(value)) ? new Intl.DateTimeFormat(dateLocale.value, { ...options, timeZone: 'Asia/Macau' }).format(new Date(value)) : '—'
}
function tierLabel(code?: string | null): string {
  const tier = code?.toLowerCase() ?? ''
  if (tier.includes('diamond')) return 'Diamond'
  if (tier.includes('platinum')) return 'Platinum'
  if (tier.includes('gold')) return 'Gold'
  if (tier.includes('vip')) return 'VIP'
  if (tier.includes('silver')) return 'Silver'
  return code || '—'
}
function boardStatus(status: string): DispatchState {
  if (status === 'PENDING' || status === 'PENDING_CONFIRMATION') return 'PENDING'
  if (status === 'AWAITING_RESPONSE' || status === 'PROCESSING') return 'AWAITING_RESPONSE'
  if (status === 'DRIVER_ASSIGNED') return 'DRIVER_ASSIGNED'
  if (status === 'NEEDS_REDISPATCH') return 'NEEDS_REDISPATCH'
  if (status === 'COMPLETED') return 'COMPLETED'
  if (status === 'CANCELLED') return 'CANCELLED'
  return 'PENDING'
}
function offerCountdown(offer?: DispatchOfferView | null): string {
  if (!offer) return ''
  const remaining = Date.parse(offer.expiresAt) - Date.parse(offer.serverTime)
  if (!Number.isFinite(remaining) || remaining <= 0) return '00:00'
  const totalSeconds = Math.floor(remaining / 1000)
  return `${String(Math.floor(totalSeconds / 60)).padStart(2, '0')}:${String(totalSeconds % 60).padStart(2, '0')}`
}
function dispatchDetail(value: DispatchOrderView, status: DispatchState): string {
  if (value.orderStatus === 'PENDING' && status === 'PENDING') return 'Pending confirmation'
  if (status === 'AWAITING_RESPONSE') {
    const name = value.currentOffer?.driverName
    const countdown = offerCountdown(value.currentOffer)
    return name ? `${name} · ${countdown}` : 'Driver response pending'
  }
  if (status === 'DRIVER_ASSIGNED') {
    const assignment = value.assignment
    return assignment?.driverName ? `${assignment.driverName}${assignment.vehicleLabel ? ` · ${assignment.vehicleLabel}` : ''}` : 'Driver assigned'
  }
  if (status === 'NEEDS_REDISPATCH') return value.currentOffer?.status === 'DECLINED' ? 'Declined or timed out' : 'Previous invitation expired'
  if (status === 'PENDING') return 'Confirmed'
  if (status === 'COMPLETED') return 'Completed'
  if (status === 'CANCELLED') return 'Cancelled'
  return 'Pending confirmation'
}
function toOrder(value: DispatchOrderView): DispatchOrder | null {
  if (!value.guest || !value.route || !value.tripId) return null
  const status = boardStatus(value.dispatchStatus)
  const booking = mapTrip({ tripId: value.tripId, tripReference: value.tripReference, propertyId: value.propertyId, guest: { guestId: '', name: value.guest.name, tierCode: value.guest.tierCode, roomNumber: value.guest.roomNumber, memberId: value.guest.memberId }, route: { pickupLocationId: '', pickupName: value.route.pickupName, dropoffLocationId: '', dropoffName: value.route.dropoffName, isCrossBorder: value.route.isCrossBorder }, pickupAt: value.pickupAt, serviceType: value.serviceType, vehicleClass: value.vehicleClass, status: value.orderStatus, version: value.version })
  const staffBooking = value.channel === 'CONSOLE'
  return {
    tripId: value.tripId,
    version: value.version,
    canDispatch: value.canDispatch,
    reference: value.tripReference || value.tripId,
    bookedAt: formatStamp(value.createdAt, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit', hour12: false }),
    guestName: value.guest.name || '—',
    tier: tierLabel(value.guest.tierCode),
    room: value.guest.roomNumber || '—',
    memberId: value.guest.memberId || '—',
    channel: staffBooking ? 'Staff booking' : 'Customer',
    channelDetail: value.bookedBy?.name || value.bookedBy?.userId || 'Customer Web',
    pickup: value.route.pickupName || '—',
    dropoff: value.route.dropoffName || '—',
    pickupTime: formatStamp(value.pickupAt, { hour: '2-digit', minute: '2-digit', hour12: false }),
    pickupMode: value.pickupMode === 'ASAP' ? 'Today · ASAP' : 'Today · Scheduled',
    vehicle: vehicleLabel(value.vehicleClass),
    passengers: value.passengers ?? 0,
    bags: value.luggage ?? 0,
    status,
    statusDetail: dispatchDetail(value, status),
    property: booking.propertyName,
    service: value.route.isCrossBorder ? 'Cross-border' : serviceLabel(value.serviceType),
    orderStatus: orderStatusLabel(value.orderStatus, booking.status),
    notes: value.specialRequests ?? '',
    departure: formatStamp(value.pickupAt, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false }),
  }
}
function applyOrders(): void {
  orders.value = sourceOrders.value.flatMap((order) => {
    const mapped = toOrder(order)
    return mapped ? [mapped] : []
  })
}
function dispatchQuery(): { page: number; pageSize: number; search?: string; propertyId?: string; channel?: string; dispatchStatus?: string } {
  const propertyId = propertyOptions.value.find((item) => item.name === property.value)?.propertyId
  const selectedChannel = channel.value === 'Staff booking' ? 'CONSOLE' : channel.value === 'Customer' ? 'SELF_SERVICE' : undefined
  return {
    page: page.value,
    pageSize,
    search: search.value.trim() || undefined,
    propertyId,
    channel: selectedChannel,
    dispatchStatus: tab.value === 'ALL' ? undefined : tab.value,
  }
}
let orderTimer = 0
let orderRequest = 0
function scheduleOrders(): void {
  if (isDemoMode) return
  window.clearTimeout(orderTimer)
  orderTimer = window.setTimeout(() => { void loadOrders() }, 300)
}
async function loadOrders(): Promise<void> {
  if (isDemoMode) return
  const request = ++orderRequest
  const query = dispatchQuery()
  isLoading.value = true
  loadError.value = ''
  try {
    const [envelope, summary] = await Promise.all([
      lms.listDispatchOrderPage(query),
      lms.dispatchOrderSummaries({ search: query.search, propertyId: query.propertyId, channel: query.channel }),
    ])
    if (request !== orderRequest) return
    applyOrderPage(envelope)
    orderSummary.value = summary
  } catch (cause) {
    if (request === orderRequest) loadError.value = apiErrorMessage(cause)
  } finally {
    if (request === orderRequest) isLoading.value = false
  }
}
function applyOrderPage(envelope: SuccessEnvelope<DispatchOrderView[]>): void {
  sourceOrders.value = envelope.data ?? []
  applyOrders()
  const pagination = envelope.meta?.pagination
  orderTotalItems.value = pagination?.totalItems ?? sourceOrders.value.length
  orderTotalPages.value = pagination?.totalPages ?? 1
}
async function loadBoard(): Promise<void> {
  if (isDemoMode) {
    orders.value = demoOrders()
    drivers.value = demoDrivers
    return
  }
  if (!propertyOptions.value.length) {
    try { propertyOptions.value = await lms.properties() }
    catch { /* Property names stay as returned identifiers. */ }
  }
  await loadOrders()
}
let driverRequest = 0
async function openOrder(order: DispatchOrder): Promise<void> {
  const request = ++driverRequest
  selected.value = order
  inviteError.value = ''
  drivers.value = []
  offerHistory.value = []
  selectedDriverId.value = ''
  let detailView: DispatchOrderView | null = null
  if (!isDemoMode) {
    try {
      detailView = await lms.dispatchOrder(order.tripId)
      const detail = toOrder(detailView)
      if (request !== driverRequest || selected.value?.tripId !== order.tripId) return
      if (!detail) throw new Error('The dispatch order response is incomplete.')
      selected.value = detail
    } catch (cause) { if (request === driverRequest) inviteError.value = apiErrorMessage(cause); return }
    try {
      const history = await lms.listDispatchOffers(order.tripId)
      if (request !== driverRequest || selected.value?.tripId !== order.tripId) return
      offerHistory.value = history.data ?? []
    } catch (cause) { if (request === driverRequest) inviteError.value = apiErrorMessage(cause) }
  }
  if (!selected.value?.canDispatch) return
  if (isDemoMode) {
    drivers.value = demoDrivers
    selectedDriverId.value = demoDrivers[0]?.id ?? ''
    return
  }
  if (!detailView) return
  isDriversLoading.value = true
  try {
    const board = await dispatchApi.shiftBoardAt(detailView.pickupAt, detailView.propertyId || undefined)
    if (request !== driverRequest || selected.value?.tripId !== order.tripId) return
    drivers.value = shiftBoardDriverOptions(board, detailView.propertyId, detailView.vehicleClass)
    selectedDriverId.value = drivers.value[0]?.id ?? ''
  } catch (cause) {
    if (request === driverRequest) inviteError.value = apiErrorMessage(cause)
  } finally {
    if (request === driverRequest) isDriversLoading.value = false
  }
}
function closeOrder(): void {
  driverRequest += 1
  selected.value = null
  inviteError.value = ''
  isDriversLoading.value = false
}
async function sendInvitation(): Promise<void> {
  const order = selected.value
  const driver = selectedDriver.value
  if (!order || !driver || !canInvite.value) return
  if (isDemoMode) {
    order.status = 'AWAITING_RESPONSE'
    order.canDispatch = false
    order.statusDetail = `${driver.name} · 00:45`
    closeOrder()
    return
  }
  isSending.value = true
  inviteError.value = ''
  try {
    const offer = await lms.createDispatchOffer(order.tripId, driver.id, order.version)
    order.status = 'AWAITING_RESPONSE'
    order.canDispatch = false
    order.statusDetail = `${offer.driverName || driver.name} · ${offerCountdown(offer)}`
    closeOrder()
    void loadOrders()
  } catch (cause) {
    inviteError.value = apiErrorMessage(cause)
  } finally {
    isSending.value = false
  }
}
function refresh(): void {
  suspendOrderWatch = true
  search.value = ''
  property.value = ''
  channel.value = ''
  tab.value = 'ALL'
  page.value = 1
  void nextTick(() => { suspendOrderWatch = false })
  void loadOrders()
}

let stopBookingCreated: (() => void) | null = null
let stopNotificationConnected: (() => void) | null = null
let liveRefreshTimer = 0
function scheduleLiveRefresh(): void {
  window.clearTimeout(liveRefreshTimer)
  liveRefreshTimer = window.setTimeout(() => { void loadBoard() }, 150)
}
// 派车页实时联动：客户自助下单（BOOKING_CREATED）到达时防抖刷新派车列表与计数，
// 使新订单无需手动刷新即可出现在看板上；点击顶部通知仍按契约查询、刷新并定位打开订单详情。
function refreshFromBookingCreated(message: NotificationMessageV1): void {
  if (message.data.notification.targetType !== 'TRIP') return
  scheduleLiveRefresh()
}

async function openRouteTarget(value: unknown): Promise<void> {
  const tripId = typeof value === 'string' ? value : ''
  if (!tripId) return
  if (isDemoMode) {
    const order = orders.value.find((item) => item.tripId === tripId)
    if (order) await openOrder(order)
    return
  }
  try {
    const order = toOrder(await lms.dispatchOrder(tripId))
    if (!order) throw new Error('The dispatch order response is incomplete.')
    await openOrder(order)
  } catch (cause) { loadError.value = apiErrorMessage(cause) }
}

onMounted(async () => {
  if (!isDemoMode) {
    stopBookingCreated = subscribeBookingCreated(refreshFromBookingCreated)
    stopNotificationConnected = subscribeStaffNotificationConnected(scheduleLiveRefresh)
  }
  await loadBoard()
  await openRouteTarget(route.query.tripId)
})
onUnmounted(() => {
  window.clearTimeout(orderTimer)
  window.clearTimeout(liveRefreshTimer)
  stopBookingCreated?.()
  stopNotificationConnected?.()
})
watch(() => route.query.tripId, (tripId) => {
  // 已在派车页时点击通知：按契约先刷新派车数据，再查询、定位并打开订单详情。
  if (typeof tripId === 'string' && tripId) scheduleLiveRefresh()
  void openRouteTarget(tripId)
})
watch(locale, () => { if (!isDemoMode) applyOrders() })
</script>

<template>
  <section class="dispatch-board">
    <header class="dispatch-heading">
      <div>
        <h1>{{ t('Dispatch') }}</h1>
        <p>{{ t('Manage customer and staff bookings, driver invitations and acceptance.') }}</p>
      </div>
    </header>

    <div class="dispatch-stats">
      <button v-for="card in cards" :key="card.status" type="button" class="dispatch-stat" :class="{ selected: tab === card.status }" @click="tab = card.status">
        <span>{{ t(card.title) }}</span>
        <strong :class="card.tone">{{ countOf(card.status) }}</strong>
        <small>{{ t(card.detail) }}</small>
      </button>
    </div>

    <div class="dispatch-tabs" role="tablist">
      <button v-for="item in tabs" :key="item.id" type="button" role="tab" :aria-selected="tab === item.id" :class="{ active: tab === item.id }" @click="tab = item.id">{{ t(item.label) }} {{ counts[item.id] }}</button>
    </div>

    <div class="dispatch-filters">
      <input v-model="search" :placeholder="t('Search order, guest or member ID')" :aria-label="t('Search order, guest or member ID')" />
      <select v-model="property" :aria-label="t('Property')">
        <option value="">{{ t('All properties') }}</option>
        <option v-for="name in properties" :key="name" :value="name">{{ name }}</option>
      </select>
      <select v-model="channel" :aria-label="t('Channel')">
        <option value="">{{ t('All channels') }}</option>
        <option value="Customer">{{ t('Customer') }}</option>
        <option value="Staff booking">{{ t('Staff booking') }}</option>
      </select>
      <button type="button" class="dispatch-refresh" :disabled="isLoading" @click="refresh">{{ t('Refresh') }}</button>
    </div>
    <p v-if="loadError" class="form-error" role="alert">{{ loadError }}</p>

    <div class="dispatch-table-wrap">
      <table class="dispatch-table">
        <thead>
          <tr>
            <th scope="col">{{ t('Order') }}</th>
            <th scope="col">{{ t('Guest') }}</th>
            <th scope="col">{{ t('Channel') }}</th>
            <th scope="col">{{ t('Property') }}</th>
            <th scope="col">{{ t('Route') }}</th>
            <th scope="col">{{ t('Pickup') }}</th>
            <th scope="col">{{ t('Vehicle') }}</th>
            <th scope="col">{{ t('Dispatch Status') }}</th>
            <th scope="col">{{ t('Action') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="order in visibleOrders" :key="order.reference">
            <td><strong>{{ order.reference }}</strong><small>{{ order.bookedAt }}</small></td>
            <td><strong>{{ order.guestName }}</strong><small>{{ t(order.tier) }} · {{ t('Room') }} {{ order.room }}</small></td>
            <td>{{ t(order.channel) }}<small>{{ t(order.channelDetail) }}</small></td>
            <td>{{ order.property }}</td>
            <td>{{ order.pickup }}<small>→ {{ order.dropoff }}</small></td>
            <td>{{ order.pickupTime }}<small>{{ t(order.pickupMode) }}</small></td>
            <td>{{ t(order.vehicle) }}<small>{{ order.passengers }} {{ t('guests') }} · {{ order.bags }} {{ t('bags') }}</small></td>
            <td><span class="dispatch-status" :class="order.status.toLowerCase()">{{ t(statusLabel(order.status)) }}</span></td>
            <td>
              <button type="button" class="dispatch-action" @click="openOrder(order)">{{ t(actionLabel(order.status, order.canDispatch).label) }}</button>
              <small>{{ t(actionLabel(order.status, order.canDispatch).detail) }}</small>
            </td>
          </tr>
          <tr v-if="isLoading && orders.length === 0"><td colspan="9" class="empty-cell">{{ t('Loading dispatch board') }}</td></tr>
          <tr v-else-if="visibleOrders.length === 0"><td colspan="9" class="empty-cell">{{ t('No matching records') }}</td></tr>
        </tbody>
      </table>
    </div>

    <footer class="dispatch-footer">
      <span>{{ t('Showing') }} {{ rangeStart }}–{{ rangeEnd }} {{ t('of') }} {{ shownTotal }} {{ t('orders') }}</span>
      <div class="dispatch-pager">
        <button type="button" :aria-label="t('Previous page')" :disabled="page <= 1" @click="page -= 1">‹</button>
        <button v-for="number in pageCount" :key="number" type="button" :class="{ selected: number === page }" @click="page = number">{{ number }}</button>
        <button type="button" :aria-label="t('Next page')" :disabled="page >= pageCount" @click="page += 1">›</button>
      </div>
    </footer>
    <p class="dispatch-note">{{ t('Unassigned orders, including waitlisted and approval-pending bookings, can be dispatched. An invitation requires driver acceptance.') }}</p>

    <div v-if="selected" class="dispatch-backdrop">
      <aside class="dispatch-drawer" role="dialog" aria-modal="true" :aria-label="t('Trip Details')">
        <header>
          <div>
            <h2>{{ t('Trip Details') }}</h2>
            <p>{{ selected.reference }} · {{ t(selected.channel === 'Customer' ? 'Customer booking' : 'Staff booking') }}</p>
          </div>
          <span class="dispatch-badge" :class="selected.status.toLowerCase()">{{ t(statusLabel(selected.status)) }}</span>
          <button type="button" class="dispatch-close" :aria-label="t('Close')" @click="closeOrder"><X :size="16" /></button>
        </header>
        <div class="dispatch-drawer-body">
          <h3>{{ t('Order Summary') }}</h3>
          <dl class="dispatch-summary">
            <div><dt>{{ t('Pickup') }}</dt><dd>{{ selected.pickup }}</dd></div>
            <div><dt>{{ t('Drop-off') }}</dt><dd>{{ selected.dropoff }}</dd></div>
            <div><dt>{{ t('Departure') }}</dt><dd>{{ selected.departure }}</dd></div>
            <div><dt>{{ t('Property') }}</dt><dd>{{ selected.property }}</dd></div>
            <div><dt>{{ t('Service / Vehicle') }}</dt><dd>{{ t(selected.service) }} · {{ t(selected.vehicle) }}</dd></div>
            <div><dt>{{ t('Guest') }}</dt><dd>{{ selected.guestName }} · {{ t(selected.tier) }}</dd></div>
            <div><dt>{{ t('Passengers / Luggage') }}</dt><dd>{{ selected.passengers }} {{ t('passengers') }} · {{ selected.bags }} {{ t('bags') }}</dd></div>
            <div><dt>{{ t('Member / Room') }}</dt><dd>{{ selected.memberId }} · {{ t('Room') }} {{ selected.room }}</dd></div>
            <div><dt>{{ t('Order status') }}</dt><dd>{{ t(selected.orderStatus) }}</dd></div>
          </dl>
          <div v-if="selected.notes" class="dispatch-notes"><span>{{ t('Trip Notes') }}</span>{{ t(selected.notes) }}</div>
          <div v-if="offerHistory.length" class="dispatch-notes"><span>{{ t('Invitation history') }}</span><div v-for="offer in offerHistory" :key="offer.offerId">{{ offer.driverName || offer.driverId }} · {{ offer.status }}</div></div>
          <template v-if="selected.canDispatch">
            <h3>{{ t('Dispatch Driver') }}</h3>
            <p>{{ t('Choose an on-duty driver scheduled for this trip date and property.') }}</p>
            <label class="dispatch-field">{{ t('Driver') }}
              <select v-model="selectedDriverId" :disabled="isDriversLoading || drivers.length === 0" :aria-label="t('Driver')">
                <option v-if="isDriversLoading" value="">{{ t('Loading drivers') }}</option>
                <option v-else-if="drivers.length === 0" value="">{{ t('No on-duty drivers for this vehicle class') }}</option>
                <option v-for="driver in drivers" :key="driver.id" :value="driver.id">{{ driver.name }}<template v-if="driver.vehicle"> · {{ t(driver.vehicle) }}</template><template v-if="driver.plate"> · {{ driver.plate }}</template></option>
              </select>
            </label>
          </template>
          <p v-if="inviteError" class="form-error" role="alert">{{ inviteError }}</p>
        </div>
        <footer>
          <button type="button" class="secondary-button" @click="closeOrder">{{ t('Close') }}</button>
          <button v-if="selected.canDispatch" type="button" class="primary-button" :disabled="!canInvite" @click="sendInvitation">{{ t(isSending ? 'Sending invitation' : 'Send Invitation') }}</button>
        </footer>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.dispatch-board { color: #2c3142; }
.dispatch-heading { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 18px; }
.dispatch-heading h1 { margin-bottom: 6px; font-size: 28px; font-weight: 650; letter-spacing: -.4px; }
.dispatch-heading p, .dispatch-heading time { margin: 0; color: #8b91a1; font-size: 13px; }
.dispatch-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 16px; }
.dispatch-stat { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; min-height: 92px; padding: 14px 16px; border: 1px solid #e6e8ee; background: #fff; text-align: left; }
.dispatch-stat.selected { border-color: #cfd3ea; box-shadow: inset 0 0 0 1px #dfe3f4; }
.dispatch-stat span, .dispatch-stat small { color: #8d93a3; font-size: 12px; }
.dispatch-stat strong { font-size: 28px; line-height: 1.1; font-weight: 650; }
.dispatch-stat strong.unassigned { color: #c4841d; }
.dispatch-stat strong.awaiting { color: #3f4cb8; }
.dispatch-stat strong.assigned { color: #1f8a4c; }
.dispatch-stat strong.redispatch { color: #d0473c; }
.dispatch-tabs { display: flex; flex-wrap: wrap; gap: 0 22px; border-bottom: 1px solid #e6e8ee; margin-bottom: 14px; }
.dispatch-tabs button { height: 36px; padding: 0; border: 0; border-bottom: 2px solid transparent; background: transparent; color: #6f7688; font-size: 13px; }
.dispatch-tabs button.active { border-bottom-color: #3f4cb8; color: #2d3560; font-weight: 650; }
.dispatch-filters { display: grid; grid-template-columns: 1.4fr .8fr .8fr .9fr auto; gap: 10px; margin-bottom: 8px; }
.dispatch-filters input, .dispatch-filters select, .dispatch-refresh { height: 38px; border: 1px solid #e1e4ec; background: #fff; color: #4d5568; }
.dispatch-filters input { padding: 0 12px; }
.dispatch-refresh { padding: 0 16px; }
.dispatch-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.dispatch-table th { padding: 12px 8px; color: #9aa1b2; font-size: 12px; font-weight: 500; text-align: left; }
.dispatch-table td { padding: 14px 8px; border-top: 1px solid #eceef3; vertical-align: top; }
.dispatch-table strong, .dispatch-table small { display: block; }
.dispatch-table strong { font-weight: 650; }
.dispatch-table small, .dispatch-action + small { margin-top: 3px; color: #9aa1b2; font-size: 12px; }
.dispatch-status { font-weight: 650; }
.dispatch-status.pending { color: #c4841d; }
.dispatch-status.awaiting_response { color: #3f4cb8; }
.dispatch-status.driver_assigned { color: #1f8a4c; }
.dispatch-status.needs_redispatch { color: #d0473c; }
.dispatch-status.completed { color: #1f8a4c; }
.dispatch-status.cancelled { color: #8b919f; }
.dispatch-action { padding: 0; border: 0; background: transparent; color: #3f4cb8; font-weight: 650; }
.dispatch-footer { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-top: 8px; color: #8d93a3; font-size: 12px; }
.dispatch-pager { display: flex; gap: 4px; }
.dispatch-pager button { min-width: 28px; height: 28px; border: 0; background: transparent; color: #667; }
.dispatch-pager button.selected { color: #3f4cb8; font-weight: 700; }
.dispatch-pager button:disabled { opacity: .35; }
.form-error { margin: 0 0 10px; color: #d0473c; font-size: 13px; }
.dispatch-note { margin: 18px 0 0; color: #8d93a3; font-size: 12px; }
.dispatch-backdrop { position: fixed; z-index: 80; inset: var(--header-height) 0 0 var(--sidebar-width); background: rgb(244 245 248 / 72%); display: flex; justify-content: flex-end; }
.dispatch-drawer { width: min(460px, 100%); height: 100%; background: #fff; box-shadow: -8px 0 24px #1d24401f; display: flex; flex-direction: column; }
.dispatch-drawer header { display: flex; align-items: flex-start; gap: 12px; padding: 22px 20px 8px; }
.dispatch-drawer header h2 { margin: 0 0 4px; font-size: 22px; }
.dispatch-drawer header p { margin: 0; color: #8d93a3; font-size: 13px; }
.dispatch-badge { margin-left: auto; padding: 4px 8px; border-radius: 4px; background: #f8f1e3; color: #b67916; font-size: 12px; white-space: nowrap; }
.dispatch-badge.awaiting_response { background: #eef0fb; color: #3f4cb8; }
.dispatch-badge.driver_assigned { background: #e8f6ee; color: #1f8a4c; }
.dispatch-badge.needs_redispatch { background: #fdeceb; color: #d0473c; }
.dispatch-badge.completed { background: #e8f6ee; color: #1f8a4c; }
.dispatch-badge.cancelled { background: #f3f4f6; color: #6d7384; }
.dispatch-close { width: 28px; height: 28px; border: 0; background: transparent; color: #667; }
.dispatch-drawer-body { overflow: auto; padding: 8px 20px 20px; }
.dispatch-drawer-body h3 { margin: 18px 0 10px; font-size: 16px; }
.dispatch-drawer-body p { margin: 0 0 12px; color: #8d93a3; font-size: 13px; }
.dispatch-summary { display: grid; grid-template-columns: 1fr 1fr; gap: 14px 18px; margin: 0; }
.dispatch-summary dt { color: #9aa1b2; font-size: 12px; }
.dispatch-summary dd { margin: 3px 0 0; font-weight: 650; }
.dispatch-notes { margin-top: 16px; padding: 12px; background: #f6f7fa; color: #5d6476; font-size: 13px; }
.dispatch-notes span { display: block; margin-bottom: 4px; color: #9aa1b2; font-size: 12px; }
.dispatch-field { display: flex; flex-direction: column; gap: 6px; color: #6d7486; font-size: 12px; }
.dispatch-field select { height: 40px; border: 1px solid #e1e4ec; }
.dispatch-drawer footer { display: flex; justify-content: space-between; gap: 12px; padding: 14px 20px; border-top: 1px solid #eceef3; }
.dispatch-drawer footer .secondary-button, .dispatch-drawer footer .primary-button { min-width: 148px; height: 40px; }
@media (max-width: 1100px) {
  .dispatch-stats, .dispatch-filters { grid-template-columns: 1fr 1fr; }
  .dispatch-backdrop { left: 0; }
}
</style>
