<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import PropertyManagement from '../../../../shared/properties/PropertyManagement.vue'
import { propertiesApi } from '../../api/properties'
import { apiErrorMessage } from '../../api/http'
import { currentUser, isDemoMode } from '../../api/session'
import { useLocale } from '../../i18n'
import { useDemoStore } from '../../stores/demo'

const { t } = useLocale()
const route = useRoute()
const store = useDemoStore()
const canRead = computed(() => currentUser.value?.permissions.includes('configuration:read') ?? false)
const canWrite = computed(() => currentUser.value?.permissions.includes('properties:write') ?? false)
</script>

<template>
  <PropertyManagement hide-heading :service="propertiesApi" :translate="t" :error-message="apiErrorMessage"
    :connected="!isDemoMode" :can-read="canRead" :can-write="canWrite"
    :can-create="currentUser?.hasAllPropertyAccess ?? false" :initial-search="String(route.query.q ?? '')" @saved="store.resetLoads()" />
</template>
