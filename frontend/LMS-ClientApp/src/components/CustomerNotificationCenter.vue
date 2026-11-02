<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { localeTag } from '@/i18n/locale'
import { orderDetailsRoute, reuseOrderDetailsPage } from '@/data/orderDetailsRoute'
import {
  currentCustomerTripId, currentCustomerTripLabel, customerNotices, customerNotificationBadgeCount,
  latestCustomerNotice, latestCustomerNoticeAt, markAllCustomerNoticesRead,
  markCustomerNoticeRead, refreshCurrentCustomerTrip, unreadCustomerNoticeCount, type CustomerNotice,
} from '@/data/customerNoticeStore'

const props = withDefaults(defineProps<{ placement?: 'tab' | 'floating' }>(), { placement: 'tab' })
// 底部菜单里与其他菜单一致：未打开时灰色线框图标，打开通知中心时为选中的橙色；悬浮入口保持原来的橙色铃铛。
const triggerIcon = computed(() => props.placement === 'tab'
  ? `/static/icons/tab-alerts-${isOpen.value ? 'active' : 'inactive'}.svg`
  : '/static/icons/notification.svg')

const isOpen = ref(false)
const arrival = ref<CustomerNotice | null>(null)
let arrivalTimer: ReturnType<typeof setTimeout> | null = null

watch(latestCustomerNoticeAt, (receivedAt) => {
  if (arrivalTimer) clearTimeout(arrivalTimer)
  const remainingMs = 6_000 - (Date.now() - receivedAt)
  arrival.value = remainingMs > 0 ? latestCustomerNotice.value : null
  if (arrival.value) arrivalTimer = setTimeout(() => { arrival.value = null; arrivalTimer = null }, remainingMs)
}, { immediate: true })

onBeforeUnmount(() => { if (arrivalTimer) clearTimeout(arrivalTimer) })
onMounted(() => { void refreshCurrentCustomerTrip() })

function openTrip(tripId: string, eventId?: string): void {
  if (eventId) markCustomerNoticeRead(eventId)
  isOpen.value = false
  arrival.value = null
  if (reuseOrderDetailsPage(tripId)) return
  uni.navigateTo({ url: orderDetailsRoute(tripId) })
}
</script>

<template>
  <view class="notification-entry" :class="placement">
    <button class="notification-trigger" :class="{ active: isOpen }" :aria-label="`${$t('Alerts')} (${customerNotificationBadgeCount})`" :aria-expanded="isOpen" @tap="isOpen = true">
      <image :src="triggerIcon" mode="aspectFit" />
      <text v-if="placement === 'tab'">{{ $t('Alerts') }}</text>
      <text v-if="customerNotificationBadgeCount" class="notification-badge">{{ customerNotificationBadgeCount > 99 ? '99+' : customerNotificationBadgeCount }}</text>
    </button>

    <button v-if="arrival" class="arrival-toast" @tap="openTrip(arrival.tripId, arrival.eventId)">
      <image src="/static/icons/notification.svg" mode="aspectFit" />
      <view><text class="arrival-title">{{ $t('New trip update') }}</text><text class="arrival-summary">{{ arrival.summary }}</text></view>
      <text class="arrival-arrow">›</text>
    </button>

    <view v-if="isOpen" class="notification-backdrop">
      <button class="notification-dismiss" :aria-label="$t('Close')" @tap="isOpen = false" />
      <view class="notification-sheet" role="dialog" aria-modal="true" :aria-label="$t('Alerts')">
        <view class="sheet-heading">
          <text>{{ $t('Alerts') }}</text>
          <button class="sheet-close" :aria-label="$t('Close')" @tap="isOpen = false">×</button>
        </view>
        <scroll-view scroll-y class="notification-list">
          <button v-if="currentCustomerTripId" class="notice-row current-trip" @tap="openTrip(currentCustomerTripId)">
            <text class="notice-label">{{ $t('Current trip') }}</text>
            <text class="notice-summary">{{ currentCustomerTripLabel || $t('Current trip') }}</text>
            <text class="notice-arrow">›</text>
          </button>
          <view class="list-heading">
            <text>{{ $t('Recent updates') }}</text>
            <button v-if="unreadCustomerNoticeCount" @tap="markAllCustomerNoticesRead">{{ $t('Mark all read') }}</button>
          </view>
          <button v-for="notice in customerNotices" :key="notice.eventId" class="notice-row" :class="{ unread: !notice.isRead }" @tap="openTrip(notice.tripId, notice.eventId)">
            <text class="notice-summary">{{ notice.summary }}</text>
            <text class="notice-time">{{ new Date(notice.occurredAt).toLocaleString(localeTag()) }}</text>
            <text class="notice-arrow">›</text>
          </button>
          <text v-if="!customerNotices.length" class="empty-state">{{ $t('No recent updates in this session.') }}</text>
        </scroll-view>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.notification-entry.tab { display:flex; min-width:44px; flex:1; align-items:center; justify-content:center; }
.notification-entry.floating { position:fixed; top:17px; right:16px; z-index:40; }
.notification-trigger { position:relative; display:flex; min-width:44px; min-height:44px; padding:0; align-items:center; justify-content:center; flex-direction:column; gap:2px; overflow:visible; color:#9299ab; font-size:11px; font-weight:500; }
.notification-trigger image { width:25px; height:25px; }
.tab .notification-trigger { height:auto; margin:0; border:0; border-radius:0; background:transparent; line-height:13px; }
.tab .notification-trigger::after { display:none; }
.tab .notification-trigger.active { color:#efa522; }
.floating .notification-trigger { width:44px; height:44px; border:1px solid #e6e8ec; border-radius:50%; background:#fff; box-shadow:0 3px 12px #0002; }
.notification-badge { position:absolute; z-index:1; top:0; right:0; display:flex; min-width:18px; height:18px; padding:0 4px; align-items:center; justify-content:center; border-radius:10px; background:#e48420; box-sizing:border-box; color:#fff; font-size:10px; font-weight:700; line-height:1; white-space:nowrap; }
.notification-backdrop { position:fixed; z-index:1000; inset:0; display:flex; align-items:flex-end; }
.notification-dismiss { position:absolute; inset:0; width:100%; height:100%; background:#0006; }
.notification-sheet { position:relative; z-index:1; width:100%; max-height:75vh; max-height:75dvh; padding:12px 16px calc(24px + env(safe-area-inset-bottom)); border-radius:20px 20px 0 0; background:#f8f8fa; color:#171717; }
.sheet-heading { position:relative; display:flex; height:44px; padding-right:52px; align-items:center; font-size:18px; font-weight:600; }
.sheet-close { position:absolute; top:0; right:0; display:flex; width:44px; height:44px; margin:0; padding:0; align-items:center; justify-content:center; background:transparent; color:#767c89; font-size:27px; line-height:1; }
.sheet-close::after { display:none; }
.notification-list { height:55vh; height:55dvh; max-height:60vh; max-height:60dvh; }
.list-heading { display:flex; margin:16px 0 8px; align-items:center; justify-content:space-between; font-size:14px; font-weight:600; }
.list-heading button { min-height:44px; color:#a56d10; font-size:12px; }
.notice-row { position:relative; display:flex; width:100%; min-height:64px; margin:0 0 8px; padding:12px 36px 12px 14px; align-items:flex-start; flex-direction:column; gap:5px; border:1px solid #e8e8ed; border-radius:11px; background:#fff; text-align:left; }
.notice-row.unread { border-color:#efd39a; background:#fff9ed; }
.notice-row.current-trip { border-color:#edca82; background:#fff5df; }
.notice-label { color:#a56d10; font-size:11px; font-weight:600; }
.notice-summary { font-size:14px; line-height:1.35; }
.notice-time { color:#8a93a4; font-size:11px; }
.notice-arrow { position:absolute; right:14px; top:50%; color:#a6adba; font-size:22px; transform:translateY(-50%); }
.empty-state { display:block; padding:20px 4px; color:#8a93a4; font-size:13px; }
.arrival-toast { position:fixed; z-index:900; top:86px; right:16px; left:16px; display:flex; min-height:64px; padding:10px 14px; align-items:center; gap:10px; border:1px solid #eac47a; border-radius:12px; background:#fff9ed; box-shadow:0 8px 25px #0002; text-align:left; }
.arrival-toast image { width:22px; height:22px; flex-shrink:0; }
.arrival-toast view { display:flex; min-width:0; flex:1; flex-direction:column; gap:2px; }
.arrival-title { color:#a56d10; font-size:12px; font-weight:600; }
.arrival-summary { overflow:hidden; color:#171717; font-size:13px; text-overflow:ellipsis; white-space:nowrap; }
.arrival-arrow { color:#a6adba; font-size:22px; }
@media (prefers-reduced-motion:reduce) { .arrival-toast { animation:none; } }
</style>
