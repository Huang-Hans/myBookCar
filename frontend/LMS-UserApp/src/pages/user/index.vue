<script setup lang="ts">
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import AppShell from '@/components/AppShell.vue'
import StatePanel from '@/components/StatePanel.vue'
import { clearSession, displayError, requireSession, type CurrentUser } from '@/api/http'
import { staffApi } from '@/api/staff'
import { locale, setLocale, t, type Locale } from '@/i18n'

type Panel = 'profile' | 'settings' | 'help' | null

const user = ref<CurrentUser | null>(null)
const panel = ref<Panel>(null)
const station = ref('Front Desk')
const defaultNotificationChannel = ref('SMS')
const propertyName = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const settingsError = ref('')
const initials = computed(() => user.value?.name.split(/\s+/).map((part) => part[0]).join('').slice(0, 2).toUpperCase() || 'LM')
const stationOptions = computed(() => [{ value: 'Front Desk', label: t('前台') }, { value: 'Concierge', label: t('禮賓部') }])
const channelOptions = computed(() => [{ value: 'SMS', label: 'SMS' }, { value: 'IN_ROOM_MESSAGE', label: t('客房訊息') }])
const stationLabel = computed(() => stationOptions.value.find((item) => item.value === station.value)?.label ?? station.value)
const channelLabel = computed(() => channelOptions.value.find((item) => item.value === defaultNotificationChannel.value)?.label ?? defaultNotificationChannel.value)
const propertyStation = computed(() => [propertyName.value || user.value?.propertyId, stationLabel.value].filter(Boolean).join(' / '))

onShow(() => { if (requireSession()) void load() })

async function load(): Promise<void> {
  loading.value = true; error.value = ''; settingsError.value = ''
  try {
    user.value = (await staffApi.currentUser()).data
    const [preferencesResult, propertiesResult] = await Promise.allSettled([staffApi.staffPreferences(), staffApi.properties()])
    if (preferencesResult.status === 'fulfilled') {
      station.value = preferencesResult.value.data.station || 'Front Desk'
      defaultNotificationChannel.value = preferencesResult.value.data.defaultNotificationChannel || 'SMS'
    } else settingsError.value = displayError(preferencesResult.reason)
    if (propertiesResult.status === 'fulfilled') propertyName.value = propertiesResult.value.data.find((item) => item.propertyId === user.value?.propertyId)?.name ?? ''
  } catch (cause) { error.value = displayError(cause) }
  finally { loading.value = false }
}

function chooseStation(event: { detail: { value: string } }): void { station.value = stationOptions.value[Number(event.detail.value)]?.value ?? station.value }
function chooseChannel(event: { detail: { value: string } }): void { defaultNotificationChannel.value = channelOptions.value[Number(event.detail.value)]?.value ?? defaultNotificationChannel.value }
function chooseLanguage(value: Locale): void { setLocale(value) }
async function saveSettings(): Promise<void> {
  if (saving.value) return
  saving.value = true; settingsError.value = ''
  try {
    await staffApi.updateConsoleSettings(station.value, defaultNotificationChannel.value, locale.value === 'en' ? 'EN' : 'ZH_HANT')
    panel.value = null
    uni.showToast({ title: t('設定已儲存'), icon: 'success' })
  } catch (cause) { settingsError.value = displayError(cause) }
  finally { saving.value = false }
}
function signOut(): void { clearSession(); uni.reLaunch({ url: '/pages/login/index' }) }
</script>

<template>
  <AppShell title="用戶中心" active="user" subtitle="管理個人資料及偏好設定">
    <text v-if="loading" class="muted">{{ t('載入中…') }}</text>
    <StatePanel v-else-if="error" :message="error" kind="error"><button class="secondary-button" @tap="load">{{ t('重試') }}</button></StatePanel>
    <template v-else-if="user">
      <view class="card identity">
        <view class="avatar">{{ initials }}</view>
        <view class="identity-copy"><text class="name">{{ user.name }}</text><text class="role">{{ user.roleCode.replaceAll('_', ' ') }}</text></view>
      </view>
      <view class="card menu">
        <button class="menu-item" @tap="panel = 'profile'"><text>{{ t('員工資料') }}</text><text class="chevron">›</text></button>
        <button class="menu-item" @tap="panel = 'settings'"><text>{{ t('控制台設定') }}</text><text class="chevron">›</text></button>
        <button class="menu-item" @tap="panel = 'help'"><text>{{ t('說明與支援') }}</text><text class="chevron">›</text></button>
      </view>
      <view class="card language-card">
        <text class="card-title">{{ t('語言') }}</text>
        <view class="language-options">
          <button :class="{ selected: locale === 'zh-Hant' }" @tap="chooseLanguage('zh-Hant')">繁體中文</button>
          <button :class="{ selected: locale === 'en' }" @tap="chooseLanguage('en')">English</button>
        </view>
      </view>
      <button class="signout" @tap="signOut">{{ t('登出') }}</button>
    </template>
    <view v-if="panel" class="overlay" @tap="panel = null">
      <view class="sheet" @tap.stop>
        <view class="sheet-header"><text class="sheet-title">{{ t(panel === 'profile' ? '員工資料' : panel === 'settings' ? '控制台設定' : '說明與支援') }}</text><button class="close" :aria-label="t('關閉')" @tap="panel = null">×</button></view>
        <template v-if="panel === 'profile' && user">
          <view class="profile-heading"><view class="avatar small-avatar">{{ initials }}</view><text class="name">{{ user.name }}</text></view>
          <view class="fact"><text>{{ t('角色') }}</text><text>{{ user.roleCode.replaceAll('_', ' ') }}</text></view>
          <view class="fact"><text>{{ t('員工編號') }}</text><text>{{ user.userId }}</text></view>
          <view class="fact"><text>{{ t('物業 / 崗位') }}</text><text>{{ propertyStation || '—' }}</text></view>
          <text class="note">{{ t('所有預約及更改均會記錄您的員工身份；主管審批須另行授權。') }}</text>
          <button class="secondary-button settings-link" @tap="panel = 'settings'">{{ t('控制台設定') }}</button>
        </template>
        <template v-else-if="panel === 'settings'">
          <text class="field-label">{{ t('崗位') }}</text>
          <picker :range="stationOptions.map((item) => item.label)" @change="chooseStation"><view class="field-picker">{{ stationLabel }} <text>⌄</text></view></picker>
          <text class="field-label">{{ t('預設通知渠道') }}</text>
          <picker :range="channelOptions.map((item) => item.label)" @change="chooseChannel"><view class="field-picker">{{ channelLabel }} <text>⌄</text></view></picker>
          <text class="note">{{ t('偏好設定會儲存到您的 LMS 帳戶。') }}</text>
          <text v-if="settingsError" class="error-text" role="alert">{{ settingsError }}</text>
          <button class="primary-button save-button" :disabled="saving" @tap="saveSettings">{{ t(saving ? '儲存中…' : '儲存設定') }}</button>
        </template>
        <template v-else>
          <text class="help-line">{{ t('如需接送協助，請聯絡禮賓營運部，分機 8888。') }}</text>
          <text class="help-line">{{ t('審批事宜請聯絡當值前台經理。') }}</text>
          <text class="help-line">{{ t('支援服務全天候提供。') }}</text>
        </template>
      </view>
    </view>
  </AppShell>
</template>

<style scoped>
.identity { display:flex; align-items:center; gap:14px; padding:20px; }
.avatar { display:flex; flex:none; align-items:center; justify-content:center; width:48px; height:48px; border-radius:50%; background:#edf0fb; color:#4f5a9f; font-size:16px; font-weight:700; }
.identity-copy { display:flex; flex-direction:column; gap:5px; min-width:0; }.name { color:#252d43; font-size:17px; font-weight:700; }.role { color:#8790a1; font-size:11px; letter-spacing:.3px; }
.menu { padding:0 17px; }.menu-item { display:flex; align-items:center; justify-content:space-between; width:100%; min-height:55px; border-bottom:1px solid #eef0f4; color:#30384e; font-size:14px; text-align:left; }.menu-item:last-child { border-bottom:0; }.chevron { color:#9ca5b5; font-size:24px; }
.language-card { padding:18px; }.language-options { display:flex; gap:9px; margin-top:14px; }.language-options button { flex:1; min-height:40px; border:1px solid #dfe2ed; border-radius:10px; color:#687285; font-size:13px; }.language-options button.selected { border-color:#4f5a9f; background:#eef0fb; color:#4f5a9f; font-weight:700; }
.signout { width:100%; min-height:48px; margin-top:16px; border:1px solid #e4e6ee; border-radius:12px; background:#fff; color:#a33f49; font-size:14px; font-weight:600; }
.overlay { position:fixed; z-index:20; inset:0; display:flex; align-items:flex-end; background:#1c234766; }.sheet { width:100%; max-height:80vh; padding:20px 20px calc(30px + env(safe-area-inset-bottom)); overflow:auto; border-radius:24px 24px 0 0; background:#fff; }.sheet-header { display:grid; grid-template-columns:minmax(0, 1fr) 35px; align-items:center; gap:12px; margin-bottom:18px; }.sheet-title { font-size:18px; font-weight:700; }.close { width:35px; height:35px; margin:0; color:#8992a4; font-size:27px; }
.profile-heading { display:flex; align-items:center; gap:11px; margin-bottom:17px; }.small-avatar { width:38px; height:38px; font-size:13px; }.fact { display:flex; justify-content:space-between; gap:15px; padding:13px 0; border-bottom:1px solid #eef0f4; color:#7b8497; font-size:12px; }.fact text:last-child { color:#30384e; font-weight:600; text-align:right; overflow-wrap:anywhere; }.note { display:block; margin:17px 0; color:#8891a3; font-size:12px; line-height:1.6; }.settings-link { margin-top:13px; }.field-picker { justify-content:space-between; }.save-button { margin-top:18px; }.help-line { display:block; margin:0 0 15px; color:#465065; font-size:13px; line-height:1.6; }
</style>
