<script setup lang="ts">
import { computed } from 'vue'
import { t } from '@/i18n'

interface Option { value: string; label: string; disabled?: boolean }

const props = defineProps<{ modelValue: string; options: Option[]; label: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const selectedIndex = computed(() => Math.max(0, props.options.findIndex((option) => option.value === props.modelValue)))
const indicatorStyle = computed(() => ({
  width: `calc((100% - 4px) / ${Math.max(1, props.options.length)})`,
  transform: `translateX(${selectedIndex.value * 100}%)`,
}))
</script>

<template>
  <view class="segmented-choice" role="group" :aria-label="label">
    <view class="selection-indicator" :style="indicatorStyle" aria-hidden="true" />
    <button v-for="option in options" :key="option.value" class="segment" :class="{ active: modelValue === option.value }"
      :disabled="option.disabled" :aria-pressed="modelValue === option.value" @tap="emit('update:modelValue', option.value)">{{ t(option.label) }}</button>
  </view>
</template>

<style scoped>
.segmented-choice { position:relative; display:flex; box-sizing:border-box; width:100%; gap:0; padding:2px; border-radius:18px; background:#e9ebf2; }
.selection-indicator { position:absolute; top:2px; left:2px; height:calc(100% - 4px); border-radius:16px; background:#4f5a9f; box-shadow:0 2px 6px #2c37642a; transition:transform 240ms cubic-bezier(.2,.75,.25,1); pointer-events:none; }
.segment { position:relative; z-index:1; flex:1; min-width:0; min-height:38px; margin:0; padding:0 4px; border:0; border-radius:16px; background:transparent; color:#778092; font-size:12px; line-height:38px; white-space:nowrap; transition:color 240ms ease; }
.segment::after { border:0; }
.segment.active { color:#fff; font-weight:700; }
.segment:disabled { cursor:not-allowed; opacity:.45; }
.segment:focus-visible { outline:2px solid #273570; outline-offset:-3px; }
@media (prefers-reduced-motion:reduce) { .selection-indicator,.segment { transition:none; } }
</style>
