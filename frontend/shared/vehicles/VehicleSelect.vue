<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import type { VehicleOption, VehicleOptionsPage } from './contracts'

const props = defineProps<{
  modelValue?: string | null
  propertyId: string
  driverId?: string
  currentLabel?: string
  disabled?: boolean
  loadPage: (propertyId: string, page: number) => Promise<VehicleOptionsPage>
  translate: (text: string) => string
  errorMessage: (cause: unknown) => string
}>()
const emit = defineEmits<{ 'update:modelValue': [value: string | null]; busy: [value: boolean] }>()
const vehicles = ref<VehicleOption[]>([])
const isLoading = ref(false)
const error = ref('')
let requestNumber = 0

async function readAll(propertyId: string): Promise<VehicleOption[]> {
  const items: VehicleOption[] = []
  for (let page = 1; ; page += 1) {
    const result = await props.loadPage(propertyId, page)
    items.push(...result.data.filter(vehicle => vehicle.propertyId === propertyId))
    if (page >= (result.meta?.pagination?.totalPages ?? page)) return items
  }
}
async function load(): Promise<void> {
  const current = ++requestNumber
  vehicles.value = []; error.value = ''
  if (!props.propertyId) { isLoading.value = false; emit('busy', false); return }
  isLoading.value = true; emit('busy', true)
  try {
    const items = await readAll(props.propertyId)
    if (current === requestNumber) vehicles.value = items
  } catch (cause) { if (current === requestNumber) error.value = props.errorMessage(cause) }
  finally { if (current === requestNumber) { isLoading.value = false; emit('busy', false) } }
}
function select(event: Event): void {
  emit('update:modelValue', (event.target as HTMLSelectElement).value || null)
}
watch(() => props.propertyId, (_propertyId, previous) => {
  if (previous !== undefined) emit('update:modelValue', null)
  void load()
}, { immediate: true })
onBeforeUnmount(() => { requestNumber += 1 })
</script>

<template>
  <select :value="modelValue ?? ''" :disabled="disabled || !propertyId || isLoading || !!error" @change="select">
    <option value="">{{ translate('Unassigned') }}</option>
    <option v-if="modelValue && !vehicles.some(vehicle => vehicle.vehicleId === modelValue)" :value="modelValue">{{ currentLabel || modelValue }}</option>
    <option v-for="vehicle in vehicles" :key="vehicle.vehicleId" :value="vehicle.vehicleId" :disabled="!!vehicle.driverId && vehicle.driverId !== driverId">{{ vehicle.plate }} · {{ vehicle.model }}</option>
  </select>
  <small v-if="isLoading" role="status">{{ translate('Loading vehicles') }}</small>
  <small v-else-if="error" class="form-error" role="alert">{{ error }} <button type="button" class="text-button" :disabled="disabled" @click="load">{{ translate('Retry') }}</button></small>
  <small v-else-if="!propertyId">{{ translate('Select a property.') }}</small>
  <small v-else-if="vehicles.length === 0">{{ translate('No vehicles for this property.') }}</small>
  <small v-else-if="vehicles.some(vehicle => vehicle.driverId && vehicle.driverId !== driverId)">{{ translate('Vehicles bound to another driver cannot be selected.') }}</small>
</template>
