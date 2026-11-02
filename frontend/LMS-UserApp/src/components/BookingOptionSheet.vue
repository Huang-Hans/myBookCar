<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { t } from '@/i18n'
import { filterCatalogOptions, type CatalogOption } from '@/api/catalogOptions'

const props = defineProps<{ open: boolean; title: string; options: CatalogOption[]; selectedId: string }>()
const emit = defineEmits<{ close: []; confirm: [id: string] }>()
const search = ref('')
const pendingId = ref('')
const visibleOptions = computed(() => filterCatalogOptions(props.options, search.value))
const canConfirm = computed(() => props.options.some((option) => option.id === pendingId.value))

watch(() => props.open, (open) => {
  if (!open) return
  search.value = ''
  pendingId.value = props.selectedId
}, { immediate: true })

function confirm(): void {
  if (canConfirm.value) emit('confirm', pendingId.value)
}
</script>

<template>
  <view v-if="open" class="option-backdrop" @tap.self="emit('close')">
    <view class="option-sheet" role="dialog" aria-modal="true" :aria-label="t(title)">
      <view class="sheet-handle" />
      <view class="sheet-header"><text>{{ t(title) }}</text><button :aria-label="t('關閉')" @tap="emit('close')">×</button></view>
      <input v-if="options.length > 6" v-model="search" class="field option-search" :placeholder="t('搜尋選項')" />
      <scroll-view class="option-list" scroll-y>
        <button v-for="option in visibleOptions" :key="option.id" class="option-row" :class="{ selected: option.id === pendingId }" :aria-pressed="option.id === pendingId" @tap="pendingId = option.id">
          <view class="option-copy"><text class="option-label">{{ option.label }}</text><text v-if="option.detail" class="option-detail">{{ option.detail }}</text></view>
          <text class="selection-mark" aria-hidden="true">{{ option.id === pendingId ? '✓' : '' }}</text>
        </button>
        <text v-if="!visibleOptions.length" class="empty-state">{{ t('沒有符合條件的選項') }}</text>
      </scroll-view>
      <button class="confirm-button" :disabled="!canConfirm" @tap="confirm">{{ t('確認') }}</button>
    </view>
  </view>
</template>

<style scoped>
.option-backdrop { position:fixed; z-index:100; inset:0; display:flex; align-items:flex-end; background:#1c234766; }
.option-sheet { display:flex; box-sizing:border-box; width:100%; max-height:calc(82dvh - env(safe-area-inset-bottom)); padding:9px 16px calc(18px + env(safe-area-inset-bottom)); flex-direction:column; border-radius:22px 22px 0 0; background:#fbfbfd; }
.sheet-handle { width:40px; height:4px; flex:none; margin:0 auto 17px; border-radius:4px; background:#d7dbe5; }
.sheet-header { display:flex; min-height:40px; flex:none; align-items:center; justify-content:space-between; border-bottom:1px solid #eceef3; color:#252d43; font-size:17px; font-weight:700; }
.sheet-header button { width:40px; height:40px; margin:0; padding:0; background:transparent; color:#8992a4; font-size:27px; }
.option-search { box-sizing:border-box; width:100%; flex:none; margin:15px 0 5px; }
.option-list { height:50dvh; min-height:0; flex:1; margin:13px 0; }
.option-row { display:flex; box-sizing:border-box; width:100%; min-height:56px; margin:0; padding:9px 14px; align-items:center; justify-content:space-between; gap:12px; border:0; border-bottom:1px solid #edf0f4; border-radius:0; background:#fff; text-align:left; }
.option-row::after { display:none; }.option-row.selected { background:#f0f2fb; }
.option-copy { display:flex; min-width:0; flex:1; flex-direction:column; gap:3px; }.option-label { overflow:hidden; color:#273149; font-size:14px; font-weight:600; text-overflow:ellipsis; white-space:nowrap; }.option-detail { overflow:hidden; color:#8790a1; font-size:11px; text-overflow:ellipsis; white-space:nowrap; }
.selection-mark { width:24px; flex:none; color:#4f5a9f; font-size:19px; font-weight:700; text-align:center; }.empty-state { display:block; padding:30px 0; color:#8790a1; font-size:13px; text-align:center; }
.confirm-button { width:100%; min-height:48px; flex:none; border-radius:24px; background:#4f5a9f; color:#fff; font-size:16px; font-weight:700; }.confirm-button[disabled] { opacity:.55; }
</style>
