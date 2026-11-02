<script setup lang="ts">
import { t } from '@/i18n'
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { displayError, getSession } from '@/api/http'
import { staffApi } from '@/api/staff'

const username = ref('')
const password = ref('')
const error = ref('')
const busy = ref(false)
onShow(() => { if (getSession()) uni.reLaunch({ url: '/pages/home/index' }) })
async function signIn(): Promise<void> {
  if (!username.value.trim() || !password.value) { error.value = t('請輸入用戶名稱和密碼'); return }
  busy.value = true; error.value = ''
  try { await staffApi.login(username.value.trim(), password.value); password.value = ''; uni.reLaunch({ url: '/pages/home/index' }) }
  catch (cause) { error.value = displayError(cause) }
  finally { busy.value = false }
}
</script>
<template><view class="login"><view class="hero"><image src="/static/brand/melco-logo-white.png" mode="aspectFit" /><text class="eyebrow">LMS · USER APP</text><text class="hero-title">{{ t('員工出行服務') }}</text><text class="hero-subtitle">{{ t('預約、行程與派車，一站處理。') }}</text></view><view class="form card"><text class="welcome">{{ t('歡迎回來') }}</text><text class="muted">{{ t('請使用企業帳戶登入') }}</text><text class="field-label">{{ t('用戶名稱') }}</text><input v-model="username" class="field" :placeholder="t('輸入用戶名稱')" /><text class="field-label">{{ t('密碼') }}</text><input v-model="password" class="field" password :placeholder="t('輸入密碼')" /><text v-if="error" class="error-text" role="alert">{{ error }}</text><button class="primary-button" :disabled="busy" @tap="signIn">{{ t(busy ? '登入中…' : '登入工作台') }}</button></view><text class="footer">MELCO TRANSPORTATION OPERATIONS</text></view></template>
<style scoped>.login { min-height:100vh; background:#f5f6f9; }.hero { display:flex; flex-direction:column; padding:calc(60px + env(safe-area-inset-top)) 27px 100px; border-radius:0 0 35px 35px; background:linear-gradient(150deg,#5c66a7,#394478); color:#fff; }.hero image { width:158px; height:50px; margin-bottom:37px; }.eyebrow { color:#d3d7ed; font-size:11px; letter-spacing:2px; }.hero-title { margin-top:8px; font-size:31px; font-weight:700; }.hero-subtitle { margin-top:10px; color:#e2e4f1; font-size:13px; }.form { position:relative; width:calc(100vw - 40px); box-sizing:border-box; margin:-62px 20px 0; padding:24px 21px 26px; }.welcome { display:block; margin-bottom:6px; font-size:21px; font-weight:700; }.form .primary-button { margin-top:25px; }.footer { display:block; padding:27px 0; color:#9ca2b1; font-size:10px; letter-spacing:2px; text-align:center; }</style>




