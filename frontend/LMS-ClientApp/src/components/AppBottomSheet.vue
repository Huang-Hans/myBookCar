<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  open: boolean
  title: string
  options?: readonly string[]
  selected?: string
  searchPlaceholder?: string
  confirmLabel?: string
  secondaryLabel?: string
  isConfirmDisabled?: boolean
}>(), { options: () => [], selected: '', confirmLabel: 'Confirm', isConfirmDisabled: false })

const emit = defineEmits<{
  close: []
  select: [value: string]
  confirm: []
}>()

const search = ref('')
const visibleOptions = computed(() => props.options.filter((option) => option.toLocaleLowerCase().includes(search.value.trim().toLocaleLowerCase())))
watch(() => props.open, (isOpen) => { if (isOpen) search.value = '' })
</script>

<template>
  <view v-if="open" class="sheet-backdrop">
    <button class="sheet-dismiss" :aria-label="$t('Close')" @tap="emit('close')" />
    <view class="sheet" role="dialog" aria-modal="true" :aria-label="title">
      <view class="sheet-grip" aria-hidden="true"><view class="sheet-handle" /></view>
      <view class="sheet-toolbar">
        <text class="sheet-title">{{ title }}</text>
        <button class="sheet-close" :aria-label="$t('Close')" @tap="emit('close')">×</button>
      </view>
      <input v-if="searchPlaceholder" v-model="search" class="sheet-search" :aria-label="searchPlaceholder" :placeholder="searchPlaceholder" />
      <scroll-view v-if="options.length" scroll-y class="sheet-options">
        <button v-for="option in visibleOptions" :key="option" class="sheet-option" :aria-pressed="selected === option" @tap="emit('select', option)">
          <text>{{ option }}</text><text v-if="selected === option" class="option-check" aria-hidden="true">✓</text>
        </button>
      </scroll-view>
      <view v-else class="sheet-content"><slot /></view>
      <button v-if="secondaryLabel" class="sheet-secondary" @tap="emit('close')">{{ secondaryLabel }}</button>
      <view class="sheet-footer"><button class="sheet-confirm" :disabled="isConfirmDisabled" @tap="emit('confirm')">{{ confirmLabel }}</button></view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.sheet-backdrop{position:fixed;z-index:1000;inset:0;display:flex;align-items:flex-end;animation:sheet-dim .24s ease-out}
.sheet-dismiss{position:absolute;inset:0;display:block;width:100%;height:100%;margin:0;padding:0;border:0;border-radius:0;background:#0005;backdrop-filter:blur(2px)}
.sheet{position:relative;z-index:1;box-sizing:border-box;width:100%;max-height:80dvh;padding:10px 12px calc(34px + env(safe-area-inset-bottom));border-radius:20px 20px 0 0;background:#f8f8fa;color:#171717;overscroll-behavior:contain;animation:sheet-up .24s cubic-bezier(.2,.8,.2,1)}
.sheet-grip{padding:0 0 4px}.sheet-handle{width:36px;height:5px;margin:0 auto 8px;border-radius:99px;background:#c7c7cc}
.sheet-toolbar{display:flex;min-height:44px;margin-bottom:20px;align-items:center;justify-content:space-between;gap:12px;border-bottom:1px solid #d1d1d6}
.sheet-title{flex:1;font-size:17px;font-weight:500;line-height:22px;text-align:left}.sheet-close{display:flex;width:44px;height:44px;min-height:44px;padding:0;align-items:center;justify-content:center;border:0;background:transparent;color:#8e8e93;font-size:28px;font-weight:300}
.sheet-search{box-sizing:border-box;width:100%;height:44px;padding:0 12px;border:0;border-radius:10px;background:#e5e5ea;font-size:16px}
.sheet-options{max-height:42dvh;margin:16px 0 0;overflow-y:auto;overscroll-behavior:contain}.sheet-option{display:flex;box-sizing:border-box;width:100%;min-height:54px;padding:14px 0;align-items:center;justify-content:space-between;gap:12px;border:0;border-radius:0;background:transparent;color:#171717;font-size:17px;text-align:left}.sheet-option+.sheet-option{border-top:1px solid #d1d1d6}.option-check{color:#a6741c;font-size:21px;font-weight:600}
.sheet-content{color:#8e8e93;text-align:center;font-size:15px;line-height:1.5}.sheet-content :deep(text){display:block;padding:4px 0 12px}.sheet-secondary{width:100%;min-height:54px;margin-top:8px;border-top:1px solid #d1d1d6;background:transparent;color:#a6741c;font-size:17px}
.sheet-footer{position:sticky;bottom:0;padding-top:16px;background:#f8f8fa}.sheet-confirm{display:flex;width:100%;height:50px;min-height:50px;align-items:center;justify-content:center;border:0;border-radius:25px;background:#f5ad27;color:#fff;font-size:17px;font-weight:400}.sheet-confirm:disabled{opacity:.4}
.sheet-dismiss::after,.sheet-close::after,.sheet-option::after,.sheet-secondary::after,.sheet-confirm::after{display:none}
@keyframes sheet-up{from{transform:translateY(100%)}to{transform:translateY(0)}}@keyframes sheet-dim{from{opacity:0}to{opacity:1}}@media(prefers-reduced-motion:reduce){.sheet-backdrop,.sheet{animation:none}}
</style>
