<script setup lang="ts">
import { locale, t } from '@/i18n'
import { computed, ref } from 'vue'
import { onHide, onShow, onUnload } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import RoutePair from '@/components/RoutePair.vue'
import StatePanel from '@/components/StatePanel.vue'
import { displayError, idempotencyKey, requireSession, type Pagination } from '@/api/http'
import { staffApi, type DispatchCandidate, type DispatchOffer, type DispatchOrderSummary, type DispatchSummary } from '@/api/staff'
import { formatDateTime, statusLabel } from '@/api/presentation'
import { shiftBoardCandidates } from '@/api/dispatchShiftDrivers'

const orders = ref<DispatchSummary[]>([])
const summary = ref<DispatchOrderSummary | null>(null)
const offerHistory = ref<DispatchOffer[]>([])
const waitingCount = computed(() => summary.value ? (summary.value.counts?.PENDING ?? 0) + (summary.value.counts?.NEEDS_REDISPATCH ?? 0) : orders.value.filter((item) => item.canDispatch).length)
const listAnimationCycle = ref(0)
const pagination = ref<Pagination | null>(null)
const page = ref(1)
const selected = ref<DispatchSummary | null>(null)
const candidates = ref<DispatchCandidate[]>([])
const candidatesLoading = ref(false)
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const candidateError = ref('')
const offerKeys = new Map<string, string>()
let refreshTimer: ReturnType<typeof setInterval> | undefined
let orderRequestVersion = 0
onShow(() => { if (requireSession()) { void load(); refreshTimer = setInterval(() => { if (!selected.value) void load(false) }, 30000) } })
onHide(() => { if (refreshTimer) clearInterval(refreshTimer) })
onUnload(() => { if (refreshTimer) clearInterval(refreshTimer) })
async function load(animate = true): Promise<void> {
  loading.value = true; error.value = ''
  try {
    const [result, totals] = await Promise.all([staffApi.dispatchOrders(page.value), staffApi.dispatchOrderSummaries()])
    orders.value = result.data
    pagination.value = result.meta?.pagination ?? null
    summary.value = totals.data
    if (animate) listAnimationCycle.value += 1
  }
  catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}
function closeOrder(): void { orderRequestVersion += 1; selected.value = null; candidatesLoading.value = false }
function changePage(next: number): void { page.value = next; closeOrder(); void load() }
async function openOrder(order: DispatchSummary): Promise<void> {
  const requestVersion = ++orderRequestVersion
  selected.value = order; candidates.value = []; offerHistory.value = []; candidateError.value = ''
  candidatesLoading.value = true
  try {
    const detail = await staffApi.dispatchOrder(order.tripId)
    if (requestVersion !== orderRequestVersion) return
    selected.value = detail.data
    if (detail.data.canDispatch) {
      try {
        const board = await staffApi.shiftBoard(detail.data.pickupAt, detail.data.propertyId)
        if (requestVersion !== orderRequestVersion) return
        candidates.value = shiftBoardCandidates(board.data, detail.data.propertyId, detail.data.pickupAt, detail.data.vehicleClass)
      } catch (cause) { if (requestVersion === orderRequestVersion) candidateError.value = displayError(cause) }
      finally { if (requestVersion === orderRequestVersion) candidatesLoading.value = false }
    } else {
      candidatesLoading.value = false
    }
    try {
      const offers = await staffApi.dispatchOffers(order.tripId)
      if (requestVersion !== orderRequestVersion) return
      offerHistory.value = offers.data
    } catch (cause) { if (requestVersion === orderRequestVersion) candidateError.value = displayError(cause) }
  }
  catch (cause) { if (requestVersion === orderRequestVersion) candidateError.value = displayError(cause) }
  finally { if (requestVersion === orderRequestVersion) candidatesLoading.value = false }
}
async function offerDriver(driver: DispatchCandidate): Promise<void> {
  if (!selected.value || busy.value) return
  const order = selected.value
  const confirmation = await new Promise<boolean>((resolve) => uni.showModal({ title: t('確認派車'), content: locale.value === 'en' ? `Invite ${driver.name} to accept ${order.tripReference}?` : `邀請 ${driver.name} 接受 ${order.tripReference}？`, success: (result) => resolve(result.confirm), fail: () => resolve(false) }))
  if (!confirmation) return
  busy.value = true; candidateError.value = ''
  try {
    const offerIdentity = `${order.tripId}:${driver.driverId}:${order.version}`
    let key = offerKeys.get(offerIdentity)
    if (!key) { key = idempotencyKey('dispatch'); offerKeys.set(offerIdentity, key) }
    await staffApi.offerDriver(order.tripId, driver.driverId, order.version, key)
    offerKeys.delete(offerIdentity)
    uni.showToast({ title: t('已發送司機邀請'), icon: 'success' })
    closeOrder()
    await load()
  } catch (cause) { candidateError.value = displayError(cause) }
  finally { busy.value = false }
}
</script>
<template><AppShell title="派車服務" active="dispatch" subtitle="管理司機邀請及派車狀態"><view class="section-title"><text>{{ t('派車訂單') }}</text><button class="text-button" @tap="load()">{{ t('重新整理') }}</button></view><view class="summary"><text class="summary-number">{{ waitingCount }}</text><view><text class="summary-title">{{ t('等待派車') }}</text><text class="summary-sub">{{ t('選擇訂單，邀請合適的司機') }}</text></view></view><text v-if="loading" class="muted">{{ t('載入中…') }}</text><StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load()">{{ t('重試') }}</button></StatePanel><StatePanel v-else-if="!orders.length" :message="t('目前沒有派車訂單')" /><view v-for="(order, index) in orders" :key="`${listAnimationCycle}-${order.tripId}`" class="card order" :style="{ animationDelay: `${index * 65}ms` }" @tap="openOrder(order)"><view class="row"><text class="reference">{{ order.tripReference }}</text><text class="chip">{{ statusLabel(order.dispatchStatus) }}</text></view><text class="muted">{{ order.guest.name }} · {{ formatDateTime(order.pickupAt) }}</text><RoutePair :pickup-name="order.route.pickupName" :dropoff-name="order.route.dropoffName" /><text v-if="order.assignment?.driverName" class="assignment">{{ t('司機：') }} {{ order.assignment.driverName }} · {{ order.assignment.vehicleLabel }}</text><text v-else-if="order.currentOffer?.driverName" class="assignment">{{ t('等待') }} {{ order.currentOffer.driverName }} {{ t('回覆') }}</text><text v-else-if="!order.canDispatch && order.blockedReason" class="muted">{{ order.blockedReason }}</text><text v-else-if="order.canDispatch" class="link">{{ t('選擇司機 ›') }}</text></view><view v-if="selected" class="overlay" @tap="closeOrder"><view class="sheet" @tap.stop><view class="sheet-handle" /><view class="sheet-header"><text class="card-title">{{ selected.tripReference }}</text><button class="close" :aria-label="t('關閉')" @tap="closeOrder">×</button></view><text class="muted">{{ selected.route.pickupName }} → {{ selected.route.dropoffName }}</text><text class="section-title">{{ t(selected.canDispatch ? '可邀請司機' : '目前不可派車') }}</text><text v-if="candidateError" class="error-text">{{ candidateError }}</text><view v-if="offerHistory.length" class="offer-history"><text class="muted">{{ t('邀請記錄') }}</text><text v-for="offer in offerHistory" :key="offer.offerId" class="muted">{{ offer.driverName || offer.driverId }} · {{ offer.status }}</text></view><text v-if="!selected.canDispatch" class="muted">{{ selected.blockedReason || statusLabel(selected.dispatchStatus) }}</text><text v-else-if="candidatesLoading" class="muted">{{ t('載入中…') }}</text><StatePanel v-else-if="!candidates.length && !candidateError" :message="t('目前沒有可用司機')" /><button v-for="driver in candidates" :key="driver.driverId" class="driver" :disabled="busy" @tap="offerDriver(driver)"><view><text class="driver-name">{{ driver.name }}</text><text class="muted">{{ driver.vehicleLabel || t('車輛待確認') }} · {{ t('當值') }}</text></view><text class="link">{{ t('邀請 ›') }}</text></button></view></view><view v-if="pagination && pagination.totalPages > 1" class="pager"><button class="secondary-button" :disabled="page <= 1" @tap="changePage(page - 1)">{{ t('上一頁') }}</button><text>{{ page }} / {{ pagination.totalPages }}</text><button class="secondary-button" :disabled="page >= pagination.totalPages" @tap="changePage(page + 1)">{{ t('下一頁') }}</button></view></AppShell></template>
<style scoped>.summary { display:flex; align-items:center; gap:17px; margin-bottom:22px; padding:20px; border-radius:17px; background:#4f5a9f; color:#fff; }.summary-number { font-size:40px; font-weight:700; }.summary-title,.summary-sub { display:block; }.summary-title { font-size:17px; font-weight:700; }.summary-sub { margin-top:5px; color:#d9dcee; font-size:11px; }.row { display:flex; align-items:center; justify-content:space-between; }.reference { font-size:14px; font-weight:700; }.assignment { display:block; color:#4b6d63; font-size:12px; }.link { color:#4f5a9f; font-size:12px; font-weight:700; }.overlay { position:fixed; z-index:20; inset:0; display:flex; align-items:flex-end; background:#1c234766; }.sheet { width:100%; max-height:75vh; padding:13px 20px calc(30px + env(safe-area-inset-bottom)); overflow:auto; border-radius:24px 24px 0 0; background:#fff; }.sheet-handle { width:40px; height:4px; margin:0 auto 19px; border-radius:2px; background:#d7dbe5; }.sheet-header { display:grid; grid-template-columns:minmax(0, 1fr) 35px; align-items:center; width:100%; min-height:35px; gap:12px; }.sheet-header .card-title { min-width:0; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }.close { justify-self:end; width:35px; height:35px; margin:0; color:#8992a4; font-size:28px; }.driver { justify-content:space-between; width:100%; min-height:65px; border-bottom:1px solid #edf0f3; text-align:left; }.driver > view { display:flex; flex-direction:column; gap:6px; }.driver-name { font-size:14px; font-weight:700; }.order { animation:order-rise 480ms cubic-bezier(.2,.75,.25,1) both; } @keyframes order-rise { from { opacity:0; transform:translateY(26px); } to { opacity:1; transform:translateY(0); } } @media (prefers-reduced-motion:reduce) { .order { animation:none; } }.offer-history { display:flex; flex-direction:column; gap:5px; margin-top:12px; }</style>


