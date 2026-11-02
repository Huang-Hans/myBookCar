<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { staffApi } from '@/api/staff'
import { t } from '@/i18n'
import { tabs, type Tab } from './tabNavigation'

const props = defineProps<{ title: string; active?: Tab; subtitle?: string }>()
const unreadCount = ref(0)
onMounted(async () => { try { unreadCount.value = (await staffApi.unreadCount()).data.unreadCount } catch { unreadCount.value = 0 } })
function navigate(key: Tab): void { if (key !== props.active) uni.reLaunch({ url: `/pages/${key}/index` }) }
function openNotifications(): void { if (props.title !== '消息通知') uni.navigateTo({ url: '/pages/notifications/index' }) }
</script>

<template>
  <view class="shell">
    <view class="masthead">
      <view class="brand-row"><image class="logo" src="/static/brand/melco-logo-white.png" mode="aspectFit" /><text class="app-name">LMS · USER</text></view>
      <view class="heading"><view class="heading-copy"><text class="eyebrow">TRANSPORT OPERATIONS</text><text class="title">{{ t(title) }}</text><text v-if="subtitle" class="subtitle">{{ t(subtitle) }}</text></view><view class="notification-control"><button class="bell" :aria-label="t('消息通知')" @tap="openNotifications"><image class="bell-icon" src="/static/icons/tabs/xiaoxi-active.svg" mode="aspectFit" /></button><view v-if="unreadCount" class="badge"><text>{{ unreadCount > 99 ? '99+' : unreadCount }}</text></view></view></view>
    </view>
    <view class="body"><slot /></view>
    <view class="tab-bar"><button v-for="tab in tabs" :key="tab.key" class="tab" :class="{ selected: active === tab.key }" @tap="navigate(tab.key)"><image class="tab-icon" :src="active === tab.key ? tab.activeIcon : tab.icon" mode="aspectFit" /><text>{{ t(tab.label) }}</text></button></view>
  </view>
</template>

<style scoped lang="scss">
.shell { box-sizing: border-box; min-height: 100vh; min-height: 100dvh; padding-bottom: calc(66px + env(safe-area-inset-bottom)); background: #f5f6f9; }
.masthead { padding: calc(25px + env(safe-area-inset-top)) 22px 27px; background: linear-gradient(145deg, #515b9f, #3e487f); color: #fff; border-radius: 0 0 26px 26px; }
.brand-row { display: flex; align-items: center; justify-content: space-between; height: 34px; }.logo { width: 127px; height: 33px; }.app-name { font-size: 10px; letter-spacing: 2px; color: #d8dbef; }
.heading { display: grid; grid-template-columns: minmax(0, 1fr) 42px; align-items: center; width: 100%; padding-top: 25px; }.heading-copy { display: flex; flex-direction: column; min-width: 0; }.eyebrow { color: #d5d8ec; font-size: 10px; letter-spacing: 2px; }.title { margin-top: 5px; font-size: 27px; font-weight: 700; }.subtitle { margin-top: 7px; color: #e1e3f1; font-size: 12px; }.notification-control { position: relative; justify-self: end; width: 42px; height: 42px; overflow: visible; }.bell { width: 42px; min-width: 42px; max-width: 42px; height: 42px; margin: 0; border: 1px solid #d6dbf0; border-radius: 13px; background: #fff; color: #4f5a9f; }.bell-icon { display:block; flex:none; width:23px; height:23px; }.badge { position: absolute; z-index: 1; top: -9px; right: -7px; display: flex; align-items: center; justify-content: center; min-width: 21px; height: 21px; padding: 0 4px; border: 2px solid #46508b; border-radius: 12px; background: #e9aa47; color: #fff; font-size: 10px; font-weight: 700; line-height: 1; white-space: nowrap; pointer-events: none; }
.body { padding: 20px 18px; }.tab-bar { position: fixed; z-index: 10; left: 0; right: 0; bottom: 0; display: flex; box-sizing: border-box; height: calc(66px + env(safe-area-inset-bottom)); padding-bottom: env(safe-area-inset-bottom); border-top: 1px solid #e7e9ef; background: #fff; box-shadow: 0 -4px 18px #1e2e5510; }.tab { display: flex; min-width: 0; flex: 1; flex-direction: column; align-items: center; justify-content: center; gap: 2px; color: #8c93a4; font-size: 11px; text-align: center; }.tab.selected { color: #4f5a9f; font-weight: 700; }.tab-icon { display: block; flex: none; width: 24px; height: 24px; }.tab > text:last-child, .tab > uni-text:last-child { display: block; width: 100%; line-height: 15px; text-align: center; }
</style>

