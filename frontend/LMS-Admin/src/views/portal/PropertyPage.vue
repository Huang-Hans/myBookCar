<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import PropertyManagement from '../../../../shared/properties/PropertyManagement.vue'
import { propertiesApi } from '../../api/properties'
import { apiErrorMessage } from '../../api/http'
import { currentUser, isDemoMode, loadCurrentUser } from '../../api/session'
import { useLocale } from '../../i18n'

const { t } = useLocale()
const route = useRoute()
const identityError = ref('')
const canRead = computed(() => currentUser.value?.permissions.includes('configuration:read') ?? false)
const canWrite = computed(() => currentUser.value?.permissions.includes('properties:write') ?? false)
async function readIdentity(): Promise<void> {
  identityError.value = ''
  try { await loadCurrentUser() } catch (cause) { identityError.value = apiErrorMessage(cause) }
}
onMounted(() => { if (!isDemoMode && !currentUser.value) void readIdentity() })
</script>

<template>
  <p v-if="!isDemoMode && !currentUser" :class="{ 'form-error': identityError }">
    {{ identityError || t('Loading') }} <button v-if="identityError" class="text-button" @click="readIdentity">{{ t('Retry') }}</button>
  </p>
  <PropertyManagement v-else :service="propertiesApi" :translate="t" :error-message="apiErrorMessage"
    :connected="!isDemoMode" :can-read="canRead" :can-write="canWrite"
    :can-create="currentUser?.hasAllPropertyAccess ?? false" :initial-search="String(route.query.q ?? '')" />
</template>
