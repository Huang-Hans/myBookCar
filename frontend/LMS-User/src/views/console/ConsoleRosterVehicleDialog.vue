<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { dispatchApi, type ShiftView } from '../../api/dispatch'
import { lms, type DriverView } from '../../api/lms'
import VehicleSelect from '../../../../shared/vehicles/VehicleSelect.vue'

const props = defineProps<{ shift: ShiftView }>()
const emit = defineEmits<{ close: []; saved: [] }>()
const { t } = useLocale()
const driver = ref<DriverView | null>(null)
const vehicleId = ref<string | null>(null)
const isLoading = ref(false)
const isVehiclesLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const saveError = ref('')
let requestNumber = 0
const canSave = computed(() => !!driver.value && !isLoading.value && !isVehiclesLoading.value && !isSaving.value
  && !loadError.value && vehicleId.value !== (driver.value.assignedVehicleId ?? null))

async function load(): Promise<void> {
  const current = ++requestNumber
  isLoading.value = true; loadError.value = ''
  try {
    const result = await lms.driver(props.shift.driverId)
    if (current !== requestNumber) return
    driver.value = result; vehicleId.value = result.assignedVehicleId ?? null
  } catch (cause) { if (current === requestNumber) loadError.value = apiErrorMessage(cause) }
  finally { if (current === requestNumber) isLoading.value = false }
}
async function save(): Promise<void> {
  if (!canSave.value || !driver.value) return
  isSaving.value = true; saveError.value = ''
  try {
    await lms.updateDriverVehicle(driver.value.driverId, vehicleId.value, driver.value.version)
    emit('saved')
  } catch (cause) {
    const issue = cause instanceof ApiRequestError ? cause.errors.find(item => item.field === 'assignedVehicleId') : undefined
    saveError.value = issue ? t(issue.message) : apiErrorMessage(cause)
  } finally { isSaving.value = false }
}
function close(): void { if (!isSaving.value) emit('close') }
onMounted(() => { void load() })
onBeforeUnmount(() => { requestNumber += 1 })
</script>

<template>
  <div class="modal-backdrop">
    <form class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Change driver vehicle')" @submit.prevent="save">
      <header class="dialog-header"><h2>{{ t('Change driver vehicle') }}</h2><button type="button" class="icon-button" :disabled="isSaving" :aria-label="t('Close')" @click="close"><X :size="18" /></button></header>
      <div class="dialog-body">
        <p><strong>{{ driver?.name || shift.driverName || shift.driverId }}</strong></p>
        <p>{{ t('The vehicle follows the driver. This change applies to all of this driver\'s shifts.') }}</p>
        <label v-if="driver" class="field-label">{{ t('Assigned vehicle') }}<VehicleSelect v-model="vehicleId" :property-id="driver.propertyId" :driver-id="driver.driverId" :current-label="driver.assignedVehicle" :disabled="isSaving || isLoading" :load-page="(propertyId, page) => dispatchApi.vehicles({ propertyId, page, pageSize: 50 })" :translate="t" :error-message="apiErrorMessage" @busy="isVehiclesLoading = $event" /></label>
        <p v-if="isLoading" role="status">{{ t('Loading') }}</p>
        <p v-else-if="loadError" class="form-error" role="alert">{{ loadError }} <button type="button" class="text-button" :disabled="isSaving" @click="load">{{ t('Retry') }}</button></p>
        <p v-if="saveError" class="form-error" role="alert">{{ saveError }}</p>
      </div>
      <footer class="dialog-footer"><button type="button" class="secondary-button" :disabled="isSaving" @click="close">{{ t('CLOSE') }}</button><button type="submit" class="primary-button" :disabled="!canSave">{{ t(isSaving ? 'Saving' : 'Save') }}</button></footer>
    </form>
  </div>
</template>
