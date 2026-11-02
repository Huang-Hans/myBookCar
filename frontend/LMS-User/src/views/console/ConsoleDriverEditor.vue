<script setup lang="ts">
import { computed, ref } from 'vue'
import { X } from 'lucide-vue-next'
import { useLocale } from '../../i18n'
import { ApiRequestError, apiErrorMessage } from '../../api/http'
import { lms, mapDriver, type DriverView, type PropertyView } from '../../api/lms'
import { dispatchApi } from '../../api/dispatch'
import VehicleSelect from '../../../../shared/vehicles/VehicleSelect.vue'

const props = defineProps<{ driver: DriverView; properties: PropertyView[] }>()
const emit = defineEmits<{ close: []; saved: [] }>()
const { t } = useLocale()
const previous = mapDriver(props.driver)
const draft = ref({ ...previous })
const isSaving = ref(false)
const isVehiclesLoading = ref(false)
const formError = ref('')
const hasChanges = computed(() => JSON.stringify(draft.value) !== JSON.stringify(previous))
const selectedPropertyId = computed(() => props.properties.find(property => property.name === draft.value.propertyName)?.propertyId
  ?? (draft.value.propertyName === previous.propertyName ? props.driver.propertyId : ''))

function validationError(): string {
  const required = [draft.value.fullName, draft.value.licenseNumber, draft.value.licenseExpiryDate,
    draft.value.phoneMasked, draft.value.propertyName, draft.value.languagesSpoken,
    draft.value.emergencyContact, draft.value.emergencyContactPhone]
  if (!required.every(value => value?.trim())) return 'Complete the required driver fields.'
  const languages = draft.value.languagesSpoken.split(',').map(value => value.trim().toLowerCase())
  return languages.some(value => !value) || new Set(languages).size !== languages.length
    ? 'Languages must be non-empty and must not repeat.' : ''
}
async function save(): Promise<void> {
  if (isSaving.value || isVehiclesLoading.value || !hasChanges.value) return
  formError.value = validationError()
  if (formError.value) return
  isSaving.value = true
  try { await lms.updateDriver(draft.value, previous); emit('saved') }
  catch (cause) {
    const vehicleIssue = cause instanceof ApiRequestError ? cause.errors.find(issue => issue.field === 'assignedVehicleId') : undefined
    formError.value = vehicleIssue?.message ?? apiErrorMessage(cause)
  }
  finally { isSaving.value = false }
}
function close(): void { if (!isSaving.value) emit('close') }
</script>

<template>
  <div class="modal-backdrop">
    <form class="dialog" role="dialog" aria-modal="true" :aria-label="t('Edit Driver')" @submit.prevent="save">
      <header class="dialog-header"><h2>{{ t('Edit Driver') }}</h2><button type="button" class="icon-button" :disabled="isSaving" :aria-label="t('Close')" @click="close"><X :size="18" /></button></header>
      <div class="dialog-body">
        <fieldset class="driver-edit-fields" :disabled="isSaving">
          <label class="field-label">{{ t('Name') }}<input v-model.trim="draft.fullName" required /></label>
          <label class="field-label">{{ t('Phone') }}<input v-model.trim="draft.phoneMasked" type="tel" required /></label>
          <label class="field-label">{{ t('License number') }}<input v-model.trim="draft.licenseNumber" required /></label>
          <label class="field-label">{{ t('License expiry date') }}<input v-model="draft.licenseExpiryDate" type="date" required /></label>
          <label class="field-label">{{ t('Property') }}<select v-model="draft.propertyName" required><option v-if="!properties.some(item => item.propertyId === driver.propertyId)" :value="previous.propertyName">{{ previous.propertyName }}</option><option v-for="property in properties" :key="property.propertyId" :value="property.name">{{ property.name }}</option></select></label>
          <label class="field-label">{{ t('STATUS') }}<select v-model="draft.status"><option value="Active">{{ t('Active') }}</option><option value="Inactive">{{ t('Inactive') }}</option><option value="Suspended">{{ t('Suspended') }}</option></select></label>
          <label class="field-label">{{ t('Assigned vehicle') }}<VehicleSelect v-model="draft.vehicleId" :property-id="selectedPropertyId" :driver-id="driver.driverId" :current-label="previous.vehicleName" :disabled="isSaving" :load-page="(propertyId, page) => dispatchApi.vehicles({ propertyId, page, pageSize: 50 })" :translate="t" :error-message="apiErrorMessage" @busy="isVehiclesLoading = $event" /></label>
          <label class="field-label">{{ t('Languages') }}<input v-model.trim="draft.languagesSpoken" :placeholder="t('Separate languages with commas')" required /></label>
          <label class="field-label">{{ t('Emergency contact') }}<input v-model.trim="draft.emergencyContact" required /></label>
          <label class="field-label">{{ t('Emergency contact phone') }}<input v-model.trim="draft.emergencyContactPhone" type="tel" required /></label>
        </fieldset>
        <p v-if="formError" class="form-error" role="alert">{{ t(formError) }}</p>
      </div>
      <footer class="dialog-footer"><button type="button" class="secondary-button" :disabled="isSaving" @click="close">{{ t('Cancel') }}</button><button type="submit" class="primary-button" :disabled="isSaving || isVehiclesLoading || !hasChanges">{{ t(isSaving ? 'Saving' : 'Save') }}</button></footer>
    </form>
  </div>
</template>

<style scoped>
.driver-edit-fields { border: 0; padding: 0; margin: 0; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
@media (max-width: 640px) { .driver-edit-fields { grid-template-columns: 1fr; } }
</style>
