<script setup lang="ts">

import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { validatePreviewSignIn } from '@/data/clientAuth'
import { resolvePostLoginRoute } from '@/data/authNavigation'
import { ApiRequestError, loginCustomer } from '@/api/http'
import { clearCustomerNotices } from '@/data/customerNoticeStore'

type Method = 'Email' | 'Phone' | 'Melco Member ID'
const methods: Method[] = ['Email', 'Phone', 'Melco Member ID']
const method = ref<Method>('Phone')
const identity = ref('')
const password = ref('')
const error = ref('')
const isSubmitting = ref(false)
const inputType = computed(() => 'text' as const)
const postLoginRoute = ref('/pages/home/index')

onLoad((query) => {
  postLoginRoute.value = resolvePostLoginRoute(query?.redirect)
})

async function signIn(): Promise<void> {
  if (method.value !== 'Phone') {
    error.value = 'The current API contract supports phone sign-in only.'
    return
  }
  const validation = validatePreviewSignIn(method.value, identity.value, password.value)
  if (validation) { error.value = validation; return }
  isSubmitting.value = true
  error.value = ''
  try {
    await loginCustomer(identity.value.trim(), password.value)
    clearCustomerNotices()
    password.value = ''
    await uni.reLaunch({ url: postLoginRoute.value })
  } catch (reason) {
    const apiError = reason instanceof ApiRequestError ? reason : null
    error.value = apiError ? `${apiError.message} (Trace ID: ${apiError.traceId})` : 'Unable to sign in.'
  } finally {
    isSubmitting.value = false
  }
}
function unavailable(provider: string): void {
  uni.showToast({ title: provider + ' sign-in needs an identity provider', icon: 'none' })
}
function goRegister(): void { uni.navigateTo({ url: '/pages/register/index' }) }
</script>

<template>
  <view class="auth-page">

    <image class="logo" src="/static/figma/melco-logo-black.png" mode="aspectFit" />
    <view class="form">
      <view class="methods"><button v-for="item in methods" :key="item" :class="{ active: method === item }" :aria-pressed="method === item" @tap="method = item; identity = ''; error = ''">{{ $t(item) }}</button></view>
      <text class="label">{{ $t(method) }}</text>
      <input v-model="identity" class="field" :type="inputType" :placeholder="'Enter Your ' + method" :aria-label="method" />
      <text class="label">{{ $t("Password") }}</text>
      <input v-model="password" class="field" password :placeholder='$t("Enter Your Password")' :aria-label='$t("Password")' />
      <text v-if="error" class="error" role="alert">{{ error }}</text>
      <button class="primary" :disabled="isSubmitting" @tap="signIn">{{ isSubmitting ? 'Signing in…' : 'Sign in' }}</button>
      <button class="register-link" @tap="goRegister">{{ $t("New to Melco? Create an Account") }}</button>
      <text class="separator">{{ $t("Or Continue with") }}</text>
      <button v-for="provider in ['Apple', 'Google', 'Wechat']" :key="provider" class="provider" @tap="unavailable(provider)">{{ $t("Continue with") }} {{ provider }}</button>
    </view>

  </view>
</template>

<style scoped lang="scss">
.auth-page { min-height:100vh; background:#fff; color:#131313; font-size:14px; }

.logo { display:block; width:190px; height:75px; margin:27px auto 15px; }
.form { padding:0 24px 55px; }
.methods { display:flex; margin:0 0 18px; gap:7px; }.methods button { flex:1; height:40px; border:1px solid #e6e6ea; border-radius:5px; color:#9299ab; font-size:12px; }.methods button.active { border-color:#e6a527; color:#222; }
.label { display:block; margin:12px 0 6px; color:#9299ab; font-size:13px; }.field { height:50px; padding:0 15px; border:1px solid #e6e6ea; border-radius:11px; background:#fff; font-size:15px; }
.primary { width:100%; height:50px; margin-top:17px; border-radius:28px; background:#efaa2d; color:#fff; font-size:17px; }.register-link { display:block; width:100%; min-height:44px; margin:18px 0 21px; background:#fff; color:#ed9220; font-size:14px; }.separator { display:block; margin-bottom:10px; color:#9299ab; text-align:center; font-size:12px; }.provider { width:100%; height:44px; margin-bottom:10px; border:1px solid #e7e7eb; border-radius:9px; background:#fff; font-size:14px; }.error { display:block; margin-top:10px; color:#ba3737; }
</style>
