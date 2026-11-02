<script setup lang="ts">
import { useLocale } from '../i18n'
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ShellLayout from '../components/ShellLayout.vue'
import DriverPage from './portal/DriverPage.vue'
import LocationPage from './portal/LocationPage.vue'
import ReportsPage from './portal/ReportsPage.vue'
import PortalApprovalsPage from './portal/PortalApprovalsPage.vue'
import PortalGenericPage from './portal/PortalGenericPage.vue'
import { isDemoMode } from '../api/session'
const { t } = useLocale()

const route = useRoute()
const router = useRouter()
const section = computed(() => String(route.params.section ?? 'drivers'))
function search(query: string): void { void router.replace({ query: query.trim() ? { q: query.trim() } : {} }) }
</script>

<template>
  <ShellLayout mode="portal" @search="search">
    <DriverPage v-if="section === 'drivers'" />
    <LocationPage v-else-if="section === 'locations'" />
    <ReportsPage v-else-if="section === 'reports'" />
    <PortalApprovalsPage v-else-if="section === 'approvals'" />
    <PortalGenericPage v-else :section="section" />
    <div class="portal-meta"><span>{{ isDemoMode ? t('◷ Local demo · External integrations are reference-only') : t('Connected to LMS API') }}</span><span>{{ t('Macau SAR · GMT+8') }}</span></div>
    <footer class="portal-footer"><span>{{ t('© 2026 Melco Resorts & Entertainment. All rights reserved.') }}</span><span>{{ t('Limousine Management Portal | Scope v3') }}</span></footer>
  </ShellLayout>
</template>
