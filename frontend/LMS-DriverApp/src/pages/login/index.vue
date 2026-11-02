<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { ApiRequestError, hasDriverSession, loginDriver } from '@/api/http'

const phone = ref('')
const password = ref('')
const error = ref('')
const isSubmitting = ref(false)

async function openDriverWorkspace(): Promise<void> {
  await uni.reLaunch({ url: '/pages/home/index' })
}

onShow(() => {
  if (!hasDriverSession()) return
  void openDriverWorkspace().catch(() => { error.value = 'Unable to open the driver workspace.' })
})

async function signIn(): Promise<void> {
  if (!/^\+?\d{8,15}$/.test(phone.value.replace(/[\s()-]/g, '')) || !password.value) { error.value = 'Enter a phone number and password.'; return }
  isSubmitting.value = true; error.value = ''
  try { await loginDriver(phone.value.trim(), password.value); password.value = ''; await openDriverWorkspace() }
  catch (reason) { const apiError = reason instanceof ApiRequestError ? reason : null; error.value = apiError ? `${apiError.message} (Trace ID: ${apiError.traceId})` : 'Unable to sign in.' }
  finally { isSubmitting.value = false }
}
</script>

<template><view class="login-page"><image src="/static/figma/melco-logo.png" mode="aspectFit" /><view class="card"><text class="title">{{ $t("Driver Sign In") }}</text><text class="label">{{ $t("Phone") }}</text><input v-model="phone" placeholder="+853…" /><text class="label">{{ $t("Password") }}</text><input v-model="password" password :placeholder='$t("Enter Your Password")' /><text v-if="error" class="error">{{ error }}</text><button :disabled="isSubmitting" @tap="signIn">{{ isSubmitting ? 'Signing in…' : 'Sign in' }}</button></view></view></template>

<style scoped lang="scss">.login-page{min-height:100vh;padding:70px 24px;background:#f8f8f8}.login-page>image{display:block;width:190px;height:75px;margin:0 auto 30px}.card{max-width:430px;margin:auto;padding:24px;border-radius:14px;background:#fff}.title{display:block;margin-bottom:25px;text-align:center;font-size:22px;font-weight:700}.label{display:block;margin:14px 0 7px;color:#777}.card input{height:50px;padding:0 14px;border:1px solid #e5e5e5;border-radius:9px}.card button{width:100%;height:50px;margin-top:23px;border-radius:9px;background:#eeaa2d;color:#fff;font-size:16px}.error{display:block;margin-top:12px;color:#b84242;font-size:13px;line-height:1.4}</style>
