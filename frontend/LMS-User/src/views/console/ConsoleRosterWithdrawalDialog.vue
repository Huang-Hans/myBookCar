<script setup lang="ts">
import { computed, ref } from 'vue'
import { X } from 'lucide-vue-next'
import { dispatchApi, type ShiftPeriod, type ShiftView } from '../../api/dispatch'
import type { ShiftWithdrawalView, WithdrawShiftsRequest } from '../../api/contracts/shiftWithdrawals'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { useLocale } from '../../i18n'

const props = defineProps<{ shifts: ShiftView[]; businessDate: string; period: ShiftPeriod; propertyId: string; scopeLabel: string; isWholePeriod: boolean }>()
const emit = defineEmits<{ close: []; saved: [result: ShiftWithdrawalView]; refresh: [] }>()
const { t } = useLocale()
const isSaving = ref(false)
const error = ref('')
const needsRefresh = ref(false)
const title = computed(() => props.isWholePeriod ? 'Withdraw period shifts' : 'Withdraw shift')
// Freeze the confirmed snapshot; retries must never target newly created replacement shifts.
const request: WithdrawShiftsRequest = {
  businessDate: props.businessDate, period: props.period, propertyId: props.propertyId || null, isWholePeriod: props.isWholePeriod,
  targets: props.shifts.map(shift => ({ shiftId: shift.shiftId, version: shift.version })),
}
const hasValidCount = computed(() => request.targets.length > 0 && request.targets.length <= 1000)
const canSubmit = computed(() => hasValidCount.value && !needsRefresh.value && !isSaving.value)
function periodLabel(value: ShiftPeriod): string { return ({ MORNING: 'Morning', AFTERNOON: 'Afternoon', NIGHT: 'Night' })[value] }
function handleError(cause: unknown): void {
  needsRefresh.value = cause instanceof ApiRequestError && [404, 409].includes(cause.status)
  error.value = needsRefresh.value ? t('The roster has changed. Refresh and confirm the withdrawal again.') : apiErrorMessage(cause)
}
async function confirm(): Promise<void> {
  if (!canSubmit.value) return
  isSaving.value = true
  error.value = ''
  try { emit('saved', await dispatchApi.withdrawShifts(request)) }
  catch (cause) { handleError(cause) }
  finally { isSaving.value = false }
}
</script>

<template>
  <div class="modal-backdrop" @keydown.esc="!isSaving && emit('close')">
    <section class="dialog small-dialog" role="dialog" aria-modal="true" aria-labelledby="roster-withdrawal-title" :aria-busy="isSaving">
      <header class="dialog-header"><h2 id="roster-withdrawal-title">{{ t(title) }}</h2><button class="icon-button" :disabled="isSaving" :aria-label="t('Close')" @click="emit('close')"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ businessDate }}</strong> · {{ t(periodLabel(period)) }} · {{ scopeLabel }}</p>
        <p v-if="isWholePeriod">{{ t('Withdraw all driver shifts in this period and property scope? Other periods will be kept.') }}</p>
        <p v-else>{{ t('Withdraw only this driver shift? Other shifts will be kept.') }}</p>
        <p>{{ t('Shifts to withdraw') }}: <strong>{{ shifts.length }}</strong></p>
        <ul class="withdrawal-targets"><li v-for="shift in shifts" :key="shift.shiftId">{{ shift.driverName || shift.driverId }} · {{ t(periodLabel(shift.period)) }}<small v-if="shift.vehiclePlate"> · {{ shift.vehiclePlate }}</small></li></ul>
        <p class="muted">{{ t('The roster history is retained. Driver and vehicle slots will be released for scheduling again.') }}</p>
        <p v-if="shifts.length > 1000" class="form-error" role="alert">{{ t('Withdraw at most 1000 shifts at a time. Filter by property or withdraw individual shifts.') }}</p>
        <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      </div>
      <footer class="dialog-footer"><button class="secondary-button" :disabled="isSaving" @click="emit('close')">{{ t('Cancel') }}</button><button v-if="needsRefresh" class="primary-button" :disabled="isSaving" @click="emit('refresh')">{{ t('Refresh roster') }}</button><button v-else class="primary-button withdrawal-confirm" :disabled="isSaving || !canSubmit" @click="confirm">{{ t(isSaving ? 'Saving' : 'Confirm withdrawal') }}</button></footer>
    </section>
  </div>
</template>

<style scoped>
.withdrawal-targets { max-height: 220px; overflow-y: auto; padding-left: 20px; line-height: 1.9; }
.withdrawal-confirm { background: #b23b45; border-color: #b23b45; }
</style>
