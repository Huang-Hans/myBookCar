<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { locale, t } from '@/i18n'
import { displayError, idempotencyKey } from '@/api/http'
import { staffApi, type DispatchCandidate, type DispatchOffer, type DispatchSummary } from '@/api/staff'
import { shiftBoardCandidates } from '@/api/dispatchShiftDrivers'
import { statusLabel } from '@/api/presentation'
import StatePanel from '@/components/StatePanel.vue'

const props = defineProps<{ tripId: string }>()
const emit = defineEmits<{ close: []; offered: [] }>()
const order = ref<DispatchSummary | null>(null)
const candidates = ref<DispatchCandidate[]>([])
const offers = ref<DispatchOffer[]>([])
const isLoading = ref(true)
const isBusy = ref(false)
const error = ref('')
const offerKeys = new Map<string, string>()
let isDisposed = false

onMounted(() => { void load() })
onUnmounted(() => { isDisposed = true })

function close(): void { if (!isBusy.value) emit('close') }

async function load(): Promise<void> {
  isLoading.value = true
  error.value = ''
  try {
    const detail = await staffApi.dispatchOrder(props.tripId)
    if (isDisposed) return
    order.value = detail.data
    const history = staffApi.dispatchOffers(props.tripId)
      .then((result) => { if (!isDisposed) offers.value = result.data })
      .catch(() => { /* Offer history is supplementary; driver selection remains available. */ })
    if (detail.data.canDispatch) {
      const board = await staffApi.shiftBoard(detail.data.pickupAt, detail.data.propertyId)
      if (isDisposed) return
      candidates.value = shiftBoardCandidates(board.data, detail.data.propertyId, detail.data.pickupAt, detail.data.vehicleClass)
    }
    await history
  } catch (cause) {
    if (!isDisposed) error.value = displayError(cause)
  } finally {
    if (!isDisposed) isLoading.value = false
  }
}

async function offerDriver(driver: DispatchCandidate): Promise<void> {
  const current = order.value
  if (!current?.canDispatch || isBusy.value) return
  const content = locale.value === 'en'
    ? `Invite ${driver.name} to accept ${current.tripReference}?`
    : `邀請 ${driver.name} 接受 ${current.tripReference}？`
  const confirmed = await new Promise<boolean>((resolve) => uni.showModal({
    title: t('確認派車'), content,
    success: (result) => resolve(result.confirm), fail: () => resolve(false),
  }))
  if (!confirmed || isDisposed) return
  isBusy.value = true
  error.value = ''
  const identity = `${current.tripId}:${driver.driverId}:${current.version}`
  let key = offerKeys.get(identity)
  if (!key) { key = idempotencyKey('dispatch'); offerKeys.set(identity, key) }
  try {
    await staffApi.offerDriver(current.tripId, driver.driverId, current.version, key)
    offerKeys.delete(identity)
    if (isDisposed) return
    uni.showToast({ title: t('已發送司機邀請'), icon: 'success' })
    emit('offered')
  } catch (cause) {
    if (!isDisposed) error.value = displayError(cause)
  } finally {
    if (!isDisposed) isBusy.value = false
  }
}
</script>

<template>
  <view class="dispatch-overlay" @tap="close" @keydown.esc="close">
    <view class="dispatch-sheet" role="dialog" :aria-label="t('派車操作')" aria-modal="true" @tap.stop>
      <view class="sheet-handle" />
      <view class="sheet-heading">
        <text class="card-title">{{ order?.tripReference || t('派車操作') }}</text>
        <button class="sheet-close" :aria-label="t('關閉')" :disabled="isBusy" @tap="close">×</button>
      </view>
      <text v-if="order" class="muted">{{ order.route.pickupName }} → {{ order.route.dropoffName }}</text>
      <text v-if="isLoading" class="muted sheet-state">{{ t('載入中…') }}</text>
      <template v-else>
        <text class="sheet-section">{{ t(order?.canDispatch ? '可邀請司機' : '目前不可派車') }}</text>
        <text v-if="error" class="error-text">{{ error }}</text>
        <text v-if="order && !order.canDispatch" class="muted sheet-state">{{ order.blockedReason || statusLabel(order.dispatchStatus) }}</text>
        <StatePanel v-else-if="order && !candidates.length && !error" :message="t('目前沒有可用司機')" />
        <button v-for="driver in order?.canDispatch ? candidates : []" :key="driver.driverId" class="driver-option" :disabled="isBusy" @tap="offerDriver(driver)">
          <view class="driver-copy"><text class="driver-name">{{ driver.name }}</text><text class="muted">{{ driver.vehicleLabel || t('車輛待確認') }} · {{ t('當值') }}</text></view>
          <text class="driver-action">{{ t('邀請 ›') }}</text>
        </button>
        <view v-if="offers.length" class="offer-history"><text class="sheet-section">{{ t('邀請記錄') }}</text><text v-for="offer in offers" :key="offer.offerId" class="muted">{{ offer.driverName || offer.driverId }} · {{ offer.status }}</text></view>
        <button v-if="!order && error" class="secondary-button" @tap="load">{{ t('重試') }}</button>
      </template>
    </view>
  </view>
</template>

<style scoped>
.dispatch-overlay { position:fixed; z-index:20; inset:0; display:flex; align-items:flex-end; background:#1c234766; }
.dispatch-sheet { box-sizing:border-box; width:100%; max-height:78vh; max-height:78dvh; padding:13px 20px calc(30px + env(safe-area-inset-bottom)); overflow:auto; border-radius:24px 24px 0 0; background:#fff; }
.sheet-handle { width:40px; height:4px; margin:0 auto 19px; border-radius:2px; background:#d7dbe5; }
.sheet-heading { display:flex; min-height:35px; align-items:center; justify-content:space-between; gap:12px; }
.sheet-close { width:35px; height:35px; margin:0; padding:0; color:#8992a4; font-size:28px; line-height:1; }
.sheet-section { display:block; margin:22px 0 10px; color:#252d43; font-size:14px; font-weight:700; }
.sheet-state { display:block; margin-top:12px; }
.driver-option { display:flex; box-sizing:border-box; width:100%; min-height:65px; margin:0; padding:10px 0; align-items:center; justify-content:space-between; border:0; border-bottom:1px solid #edf0f3; border-radius:0; background:#fff; text-align:left; }
.driver-copy { display:flex; flex-direction:column; gap:6px; }
.driver-name { color:#252d43; font-size:14px; font-weight:700; }
.driver-action { color:#4f5a9f; font-size:12px; font-weight:700; }
.offer-history { display:flex; flex-direction:column; gap:5px; margin-top:12px; }
.error-text { display:block; color:#b23a48; font-size:12px; }
</style>
