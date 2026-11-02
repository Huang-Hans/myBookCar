<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiErrorMessage } from '../api/http'
import { login } from '../api/session'
import { useLocale } from '../i18n'

const { t } = useLocale()
const router = useRouter()
const route = useRoute()
const username = ref('')
const password = ref('')
const error = ref('')
const busy = ref(false)
async function submit(): Promise<void> {
  if (!username.value.trim() || !password.value) { error.value = 'Enter your username and password.'; return }
  busy.value = true; error.value = ''
  try {
    await login(username.value.trim(), password.value)
    const next = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/') && !route.query.redirect.startsWith('//') ? route.query.redirect : '/console/tracking'
    await router.replace(next)
  } catch (cause) {
    error.value = apiErrorMessage(cause)
  } finally { busy.value = false; password.value = '' }
}
</script>

<template>
  <main class="login-page"><form class="panel login-card" @submit.prevent="submit">
    <img src="/assets/melco-logo-white.png" alt="Melco" class="login-logo" />
    <h1>{{ t('Sign in to LMS') }}</h1><p>{{ t('Use your corporate account to access the staff workspace.') }}</p>
    <label class="field-label">{{ t('Username') }}<input v-model="username" autocomplete="username" required /></label>
    <label class="field-label">{{ t('Password') }}<input v-model="password" type="password" autocomplete="current-password" required /></label>
    <p v-if="error" role="alert" class="form-error">{{ t(error) }}</p>
    <button class="primary-button" type="submit" :disabled="busy">{{ t(busy ? 'Signing in...' : 'Sign in') }}</button>
  </form></main>
</template>
