<script setup lang="ts">
import { useLocale } from '../i18n'
import { computed } from 'vue'
const { t } = useLocale()
const props = defineProps<{ totalItems: number; page: number; pageSize: number }>()
const emit = defineEmits<{
  (event: 'update:page', value: number): void
  (event: 'update:pageSize', value: number): void
}>()
const totalPages = computed(() => Math.max(1, Math.ceil(props.totalItems / props.pageSize)))
const firstItem = computed(() => props.totalItems === 0 ? 0 : (props.page - 1) * props.pageSize + 1)
const lastItem = computed(() => Math.min(props.totalItems, props.page * props.pageSize))
</script>

<template>
  <div class="data-pager">
    <span>{{ firstItem }}–{{ lastItem }} {{ t('of') }} {{ totalItems }} {{ t('records') }}</span>
    <div class="pager-controls">
      <select :aria-label="t('Page size')" :value="pageSize" @change="emit('update:pageSize', Number(($event.target as HTMLSelectElement).value)); emit('update:page', 1)"><option :value="10">{{ t('10 / page') }}</option><option :value="20">{{ t('20 / page') }}</option><option :value="50">{{ t('50 / page') }}</option></select>
      <button :aria-label="t('Previous page')" :disabled="page <= 1" @click="emit('update:page', page - 1)">‹</button>
      <button v-for="number in totalPages" :key="number" :class="{ selected: number === page }" @click="emit('update:page', number)">{{ number }}</button>
      <button :aria-label="t('Next page')" :disabled="page >= totalPages" @click="emit('update:page', page + 1)">›</button>
    </div>
  </div>
</template>
