<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { locale, t } from '@/i18n'
import { availableDepartureHours, availableDepartureMinutes, departureDateOptions, isFutureMacauDeparture, nextMacauDeparture, type DepartureDateOption } from '@/api/bookingDeparture'

const props = defineProps<{ open: boolean; date: string; time: string }>()
const emit = defineEmits<{ close: []; confirm: [value: { date: string; time: string }] }>()
const pickerNow = ref(new Date())
const dates = ref<DepartureDateOption[]>([])
const draft = ref<[number, number, number]>([0, 0, 0])
const selectedDate = computed(() => dates.value[draft.value[0]]?.value ?? '')
const hours = computed(() => availableDepartureHours(selectedDate.value, pickerNow.value))
const selectedHour = computed(() => hours.value[draft.value[1]] ?? '')
const minutes = computed(() => availableDepartureMinutes(selectedDate.value, selectedHour.value, pickerNow.value))

watch(() => props.open, (open) => { if (open) resetDraft() }, { immediate: true })

function resetDraft(): void {
  pickerNow.value = new Date()
  const earliest = nextMacauDeparture(pickerNow.value)
  const date = isFutureMacauDeparture(props.date, props.time, pickerNow.value) ? props.date : earliest.date
  const time = date === props.date && isFutureMacauDeparture(props.date, props.time, pickerNow.value) ? props.time : earliest.time
  dates.value = departureDateOptions(earliest.date, locale.value)
  const dateIndex = Math.max(0, dates.value.findIndex((option) => option.value === date))
  draft.value = [dateIndex, 0, 0]
  const hourIndex = Math.max(0, hours.value.indexOf(time.slice(0, 2)))
  draft.value = [dateIndex, hourIndex, 0]
  const minuteIndex = Math.max(0, minutes.value.indexOf(time.slice(3, 5)))
  draft.value = [dateIndex, hourIndex, minuteIndex]
}

function changeDraft(event: { detail: { value: number[] } }): void {
  const [dateIndex = 0, hourIndex = 0, minuteIndex = 0] = event.detail.value
  const [previousDate, previousHour] = draft.value
  draft.value = [dateIndex, dateIndex === previousDate ? hourIndex : 0,
    dateIndex !== previousDate || hourIndex !== previousHour ? 0 : minuteIndex]
}

function confirm(): void {
  const date = selectedDate.value
  const time = `${selectedHour.value}:${minutes.value[draft.value[2]] ?? ''}`
  if (!isFutureMacauDeparture(date, time, new Date())) {
    resetDraft()
    uni.showToast({ title: t('出發時間須在未來'), icon: 'none' })
    return
  }
  emit('confirm', { date, time })
}
</script>

<template>
  <view v-if="open" class="departure-backdrop" @tap.self="emit('close')">
    <view class="departure-sheet" role="dialog" aria-modal="true" :aria-label="t('選擇出發日期與時間')">
      <view class="sheet-handle" />
      <view class="sheet-header"><text>{{ t('出發日期與時間') }}</text><button :aria-label="t('關閉')" @tap="emit('close')">×</button></view>
      <view class="wheel-headings"><text>{{ t('日期') }}</text><text>{{ t('小時') }}</text><text>{{ t('分鐘') }}</text></view>
      <view class="wheel-wrap">
        <picker-view class="departure-wheel" :value="draft" indicator-style="height:46px" mask-style="background:linear-gradient(to bottom,#fbfbfd 0%,rgba(251,251,253,0) 38%,rgba(251,251,253,0) 62%,#fbfbfd 100%)" @change="changeDraft">
          <picker-view-column class="date-column"><view v-for="(option, index) in dates" :key="option.value" class="wheel-option" :class="{ selected: draft[0] === index }">{{ option.label }}</view></picker-view-column>
          <picker-view-column><view v-for="(hour, index) in hours" :key="hour" class="wheel-option" :class="{ selected: draft[1] === index }">{{ hour }}</view></picker-view-column>
          <picker-view-column><view v-for="(minute, index) in minutes" :key="minute" class="wheel-option" :class="{ selected: draft[2] === index }">{{ minute }}</view></picker-view-column>
        </picker-view>
        <view class="selected-band" aria-hidden="true" />
      </view>
      <text class="timezone-note">{{ t('澳門時間') }}</text>
      <button class="confirm-button" @tap="confirm">{{ t('確認') }}</button>
    </view>
  </view>
</template>

<style scoped>
.departure-backdrop { position:fixed; z-index:100; inset:0; display:flex; align-items:flex-end; background:#1c234766; }
.departure-sheet { box-sizing:border-box; width:100%; max-height:calc(100dvh - 32px); padding:9px 16px calc(18px + env(safe-area-inset-bottom)); overflow:hidden; border-radius:22px 22px 0 0; background:#fbfbfd; }
.sheet-handle { width:40px; height:4px; margin:0 auto 17px; border-radius:4px; background:#d7dbe5; }
.sheet-header { display:flex; align-items:center; justify-content:space-between; min-height:40px; border-bottom:1px solid #eceef3; color:#252d43; font-size:17px; font-weight:700; }
.sheet-header button { width:40px; height:40px; margin:0; padding:0; background:transparent; color:#8992a4; font-size:27px; }
.wheel-headings { display:grid; grid-template-columns:2fr 1fr 1fr; margin:22px 0 4px; color:#8b93a4; font-size:12px; text-align:center; }
.wheel-wrap { position:relative; }
.departure-wheel { width:100%; height:230px; }
.date-column { flex:2; }
.wheel-option { display:flex; height:46px; align-items:center; justify-content:center; color:#9aa2b0; font-size:15px; }
.wheel-option.selected { color:#273358; font-size:17px; font-weight:700; }
.selected-band { position:absolute; top:92px; right:0; left:0; height:46px; border-top:2px solid #4f5a9f; border-bottom:2px solid #4f5a9f; border-radius:7px; background:#4f5a9f0b; pointer-events:none; }
.timezone-note { display:block; margin:15px 0 20px; color:#8790a1; font-size:12px; }
.confirm-button { width:100%; min-height:48px; border-radius:24px; background:#4f5a9f; color:#fff; font-size:16px; font-weight:700; }
@media (prefers-reduced-motion:reduce) { .departure-backdrop,.departure-sheet { animation:none; } }
</style>
