<script setup lang="ts">

import { ref } from 'vue'
import { validatePreviewRegistration } from '@/data/clientAuth'
import { setLocale, t } from '@/i18n/locale'
import AppBottomSheet from '@/components/AppBottomSheet.vue'
import { useClientDialog } from '@/composables/useClientDialog'

const fullName = ref('')
const phone = ref('')
const email = ref('')
const language = ref('English')
const error = ref('')
const isLanguageSheetOpen = ref(false)
const pendingLanguage = ref('English')
const { dialog, alert, close, submit: submitDialog } = useClientDialog()

function submit(): void {
  const validation = validatePreviewRegistration(fullName.value, phone.value, email.value)
  if (validation) { error.value = validation; return }
  alert(t('Registration Unavailable'), t('The current LMS API contract does not include customer registration or account activation. Please contact operations for account access.'), t('OK'))
}
function openLanguageSheet(): void { pendingLanguage.value = language.value === '繁體中文' ? 'Traditional Chinese' : language.value === '简体中文' ? 'Simplified Chinese' : 'English'; isLanguageSheetOpen.value = true }
function confirmLanguage(): void {
  language.value = pendingLanguage.value === 'Traditional Chinese' ? '繁體中文' : pendingLanguage.value === 'Simplified Chinese' ? '简体中文' : 'English'
  setLocale(pendingLanguage.value === 'Traditional Chinese' ? 'ZH_HANT' : pendingLanguage.value === 'Simplified Chinese' ? 'ZH_HANS' : 'EN')
  isLanguageSheetOpen.value = false
}
function goBack(): void { uni.navigateBack() }
function unavailable(provider: string): void { uni.showToast({ title: provider + ' registration is not connected', icon: 'none' }) }
</script>

<template>
  <view class="auth-page"><image class="logo" src="/static/figma/melco-logo-black.png" mode="aspectFit" /><view class="form"><text class="heading">{{ $t("Your Next Journey Starts Here") }}</text><text class="label">{{ $t("Full Name") }}</text><input v-model="fullName" class="field" :placeholder='$t("Your Full Name")' /><text class="label">{{ $t("Phone") }}</text><input v-model="phone" class="field" type="text" placeholder="+852 6123 4567" /><text class="label">{{ $t("Email") }}</text><input v-model="email" class="field" placeholder="You@Example.Com" /><button class="field picker" @tap="openLanguageSheet"><text>{{ $t("Preferred Language") }}</text><text>{{ language }}　›</text></button><text v-if="error" class="error" role="alert">{{ error }}</text><button class="primary" @tap="submit">{{ $t("Create Account") }}</button><button class="sign-in" @tap="goBack">{{ $t("Already Have an Account? Sign in") }}</button><text class="separator">{{ $t("Or Continue with") }}</text><button v-for="provider in ['Apple', 'Google', 'Wechat']" :key="provider" class="provider" @tap="unavailable(provider)">{{ $t("Continue with") }} {{ provider }}</button></view><AppBottomSheet :open="isLanguageSheetOpen" :title="$t('Language')" :options="['English', 'Traditional Chinese', 'Simplified Chinese']" :selected="pendingLanguage" :confirm-label="$t('Confirm')" @select="pendingLanguage = $event" @close="isLanguageSheetOpen = false" @confirm="confirmLanguage" /><AppBottomSheet :open="dialog !== null" :title="dialog?.title ?? ''" :confirm-label="dialog?.confirmLabel ?? $t('OK')" @close="close" @confirm="submitDialog"><text>{{ dialog?.message }}</text></AppBottomSheet></view>>
</template>

<style scoped lang="scss">
.auth-page { min-height:100vh; background:#fff; color:#171717; }.logo { display:block; width:190px; height:75px; margin:20px auto 12px; }.form { padding:0 24px 50px; }.heading { display:block; margin-bottom:18px; text-align:center; font-size:21px; font-weight:600; }.label { display:block; margin:13px 0 7px; color:#9299ab; font-size:13px; }.field { height:49px; padding:0 14px; border:1px solid #e6e6ea; border-radius:9px; font-size:15px; }.picker { display:flex; align-items:center; justify-content:space-between; }.primary { width:100%; height:50px; margin-top:21px; border-radius:28px; background:#f1ac2b; color:#fff; font-size:16px; }.sign-in { display:block; width:100%; margin:22px 0; color:#ed9220; font-size:14px; }.separator { display:block; margin-bottom:10px; color:#9299ab; text-align:center; font-size:12px; }.provider { width:100%; height:44px; margin-bottom:10px; border:1px solid #e7e7eb; border-radius:9px; font-size:14px; }.error { display:block; margin-top:10px; color:#ba3737; }.note { display:block; color:#999; text-align:center; font-size:11px; }
.logo{height:45px;margin:39px auto 0}.form{padding-top:0}.heading{margin:0 0 46px;color:#959dad;font-size:13px;font-weight:400}.label{margin:13px 0 7px}.field{height:50px;border-radius:11px;background:#fff}.picker{height:58px;margin-top:12px;font-size:14px}.picker text:last-child{color:#919bad}.primary{margin-top:17px}.sign-in{height:44px;margin:19px 0 14px;background:transparent}.separator{margin-bottom:10px}.provider{background:#fff}.note{display:none}
</style>
