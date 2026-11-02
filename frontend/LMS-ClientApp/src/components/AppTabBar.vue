<script setup lang="ts">
import { markTabRefresh, type TabKey } from '@/data/tabNavigation'
import CustomerNotificationCenter from '@/components/CustomerNotificationCenter.vue'
import CurrentTripBanner from '@/components/CurrentTripBanner.vue'
const props = defineProps<{ active: TabKey }>()
const tabs: Array<{ key: TabKey; label: string; url: string }> = [
  { key: 'home', label: 'Home', url: '/pages/home/index' },
  { key: 'bookings', label: 'Bookings', url: '/pages/bookings/index' },
]
function icon(key: TabKey): string { return `/static/icons/tab-${key}-${props.active === key ? 'active' : 'inactive'}.svg` }
function navigate(url: string): void {
  markTabRefresh(url.replace('/pages/', '').replace('/index', '') as TabKey)
  uni.reLaunch({ url })
}
</script>

<template>
  <CurrentTripBanner />
  <view class="tab-shell"><view class="tab-bar"><button v-for="tab in tabs" :key="tab.key" :class="{ active: active === tab.key }" :aria-current="active === tab.key ? 'page' : undefined" @tap="navigate(tab.url)"><image :src="icon(tab.key)" mode="aspectFit" /><text>{{ $t(tab.label) }}</text></button><CustomerNotificationCenter /><button :class="{ active: active === 'account' }" :aria-current="active === 'account' ? 'page' : undefined" @tap="navigate('/pages/account/index')"><image :src="icon('account')" mode="aspectFit" /><text>{{ $t('Account') }}</text></button></view></view>
</template>

<style scoped lang="scss">
.tab-shell { position: fixed; right: 0; bottom: 0; left: 0; z-index: 20; box-sizing: border-box; height: calc(56px + env(safe-area-inset-bottom)); padding-bottom: env(safe-area-inset-bottom); background: #fafafc; }
.tab-bar { display: flex; box-sizing: border-box; height: 56px; padding: 3px 0; border-top: 1px solid #eeeef2; }
.tab-bar button { display: flex; flex: 1; min-width: 44px; min-height: 44px; align-items: center; justify-content: center; flex-direction: column; gap: 2px; color: #9299ab; font-size: 11px; font-weight: 500; line-height: 13px; }
.tab-bar button.active { color: #efa522; }
.tab-bar image { width: 25px; height: 25px; }
</style>
