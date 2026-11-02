<script setup lang="ts">
import { useLocale, type Locale } from '../i18n'
import { computed, nextTick, onMounted, onUnmounted, ref, watch, watchEffect } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { Bell, CalendarClock, CalendarDays, Car, ChartColumnIncreasing, ChevronDown, CircleHelp, ClipboardList, Clock3, FileText, MapPin, Route, Search, Settings2, ShieldCheck, Ticket, UserRound, UsersRound, X, Menu, BookOpen, Ban, Crown, Building2, ListChecks, Gauge, Layers3 } from 'lucide-vue-next'
import type { Component } from 'vue'
import { currentUser, isDemoMode, logout } from '../api/session'
import { useDemoStore } from '../stores/demo'
import { lms } from '../api/lms'
import { bookingNotifications, bookingNotificationUnreadCount, markBookingNotificationRead, startStaffNotificationHub, stopStaffNotificationHub } from '../api/notifications'
import type { NotificationSnapshotItem } from '../api/notificationContract'
const { locale, dateLocale, confirmationLanguage, setLocale, t } = useLocale()

const props = defineProps<{ mode: 'portal' | 'console' }>()
const emit = defineEmits<{
  (event: 'search', query: string): void
  (event: 'focus-search'): void
}>()
const store = useDemoStore()
const route = useRoute()
const router = useRouter()
const isCollapsed = ref(false)
const isSettingsOpen = ref(false)
const station = ref('Front Desk')
const defaultNotificationChannel = ref('SMS')
const settingsError = ref('')
const isLanguageOpen = ref(false)
const isAccountOpen = ref(false)
const accountPanel = ref<'profile' | 'help' | null>(null)
const accountControl = ref<HTMLElement | null>(null)
const accountTrigger = ref<HTMLButtonElement | null>(null)
const accountClose = ref<HTMLButtonElement | null>(null)
const languageControl = ref<HTMLElement | null>(null)
const languageTrigger = ref<HTMLButtonElement | null>(null)
const notificationControl = ref<HTMLElement | null>(null)
const isNotificationOpen = ref(false)
const search = ref('')
const currentSection = computed(() => String(route.params.section ?? ''))

interface NavItem { label: string; path?: string; icon: Component; badge?: string; children?: NavItem[] }
interface NavGroup { label: string; items: NavItem[] }
const portalGroups: NavGroup[] = [
  { label: 'OPERATIONS', items: [
    { label: 'Driver Management', path: 'drivers', icon: UsersRound },
    { label: 'Report Center', path: 'reports', icon: ChartColumnIncreasing },
  ] },
  { label: 'ADMINISTRATION', items: [
    { label: 'Common Used Locations', path: 'locations', icon: MapPin },
    { label: 'Properties', path: 'properties', icon: Building2 },
    { label: 'Cancellation Reasons', path: 'reasons', icon: X },
    { label: 'Cross-border Approvals', path: 'approvals', icon: ShieldCheck, badge: '8' },
    { label: 'Travel Time Index', path: 'travel', icon: Gauge },
    { label: 'System Notifications', path: 'notifications', icon: Bell },
    { label: 'Location Categories', path: 'categories', icon: Layers3 },
    { label: 'Blacklist', path: 'blacklist', icon: Ban },
    { label: 'Membership Tiers', path: 'tiers', icon: Crown },
    { label: 'Self-Booking Control', path: 'self-booking', icon: CalendarDays },
    { label: 'CS Qualify Control', path: 'cs-qualify', icon: ListChecks },
  ] },
  { label: 'SYSTEM', items: [
    { label: 'System Configuration', path: 'system', icon: Settings2 },
    { label: 'Configuration Register', path: 'register', icon: BookOpen },
  ] },
]
const consoleGroups: NavGroup[] = [
  { label: '', items: [
    { label: 'Booking List', path: 'manage', icon: ClipboardList },
    { label: 'Dispatch', path: 'dispatch', icon: Route },
    { label: 'Trip Tracking', path: 'tracking', icon: Clock3 },
    { label: 'History', path: 'history', icon: FileText },
    { label: 'Member Information', path: 'members', icon: Crown },
    { label: 'Coupon Management', path: 'coupons', icon: Ticket },
    { label: 'Driver Management', path: 'drivers', icon: UserRound },
    { label: 'Driver Roster', path: 'roster', icon: CalendarClock },
    { label: 'Vehicle Management', path: 'vehicles', icon: Car },
    { label: 'Property Management', path: 'properties', icon: Building2 },
    { label: 'Notifications', path: 'notifications', icon: Bell },
    { label: 'Report Center', path: 'reports', icon: ChartColumnIncreasing },
  ] },
]
const groups = computed(() => props.mode === 'portal' ? portalGroups : consoleGroups)
const bookingMenuOpen = ref(true)
function branchItems(items: NavItem[]): NavItem[] {
  return items.flatMap((item) => item.children ? [item, ...item.children] : [item])
}
function isBranchActive(item: NavItem): boolean {
  return item.children?.some((child) => child.path === currentSection.value) ?? false
}
function isBranchOpen(item: NavItem): boolean {
  return item.label === 'Booking Management' ? bookingMenuOpen.value : isBranchActive(item)
}
function toggleBranch(item: NavItem): void {
  if (isCollapsed.value) {
    const path = item.children?.[0]?.path
    if (path) void router.push(`/${props.mode}/${path}`)
    return
  }
  if (item.label === 'Booking Management') bookingMenuOpen.value = !bookingMenuOpen.value
}
const notificationUnreadCount = computed(() => isDemoMode ? store.notices.filter((notice) => !notice.isRead).length : bookingNotificationUnreadCount.value)
const notificationBadgeText = computed(() => notificationUnreadCount.value > 99 ? '99+' : String(notificationUnreadCount.value))
const showNotificationDot = computed(() => props.mode === 'portal' || notificationUnreadCount.value > 0)
const notificationAriaLabel = computed(() => notificationUnreadCount.value > 0 ? `${t('Notifications')}: ${notificationUnreadCount.value} ${t('unread')}` : t('Notifications'))
const pageTitle = computed(() => groups.value.flatMap((group) => branchItems(group.items)).find((item) => item.path === currentSection.value)?.label ?? 'LMS · Staff Workspace')
watchEffect(() => { if (typeof document !== 'undefined') document.title = `${t(pageTitle.value)} · LMS` })
function closeLanguageOnOutside(event: PointerEvent): void {
  if (!languageControl.value?.contains(event.target as Node)) isLanguageOpen.value = false
  if (!accountControl.value?.contains(event.target as Node)) isAccountOpen.value = false
  if (!notificationControl.value?.contains(event.target as Node)) isNotificationOpen.value = false
}
function onAccountKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') { isAccountOpen.value = false; accountTrigger.value?.focus(); return }
  if (!isAccountOpen.value || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return
  const options = Array.from(accountControl.value?.querySelectorAll<HTMLButtonElement>('[role="menuitem"]') ?? [])
  const current = options.indexOf(document.activeElement as HTMLButtonElement)
  const next = event.key === 'Home' ? 0 : event.key === 'End' ? options.length - 1 : event.key === 'ArrowDown' ? (current + 1) % options.length : (current - 1 + options.length) % options.length
  options[next]?.focus()
  event.preventDefault()
}
async function toggleAccount(): Promise<void> {
  isLanguageOpen.value = false
  isAccountOpen.value = !isAccountOpen.value
  if (isAccountOpen.value) { await nextTick(); accountControl.value?.querySelector<HTMLButtonElement>('[role="menuitem"]')?.focus() }
}
async function openAccountPanel(panel: 'profile' | 'help'): Promise<void> {
  isAccountOpen.value = false
  accountPanel.value = panel
  await nextTick()
  accountClose.value?.focus()
}
function closeAccountPanel(): void { accountPanel.value = null; accountTrigger.value?.focus() }
function onDocumentKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && accountPanel.value) closeAccountPanel()
}
function openAccountSettings(): void {
  isAccountOpen.value = false; isSettingsOpen.value = true; settingsError.value = ''
  if (!isDemoMode) void lms.staffPreferences().then((value) => { station.value = value.station || 'Front Desk'; defaultNotificationChannel.value = value.defaultNotificationChannel }).catch((error) => { settingsError.value = error instanceof Error ? error.message : 'Unable to load settings.' })
}
async function saveAccountSettings(): Promise<void> {
  if (!isDemoMode) {
    try { await lms.updateConsoleSettings(station.value, defaultNotificationChannel.value, confirmationLanguage.value) }
    catch (error) { settingsError.value = error instanceof Error ? error.message : 'Unable to save settings.'; return }
  }
  isSettingsOpen.value = false
}
function openPortalAccount(): void { isAccountOpen.value = false; void router.push('/portal/system') }
function signOut(): void { void stopStaffNotificationHub(); logout(); isAccountOpen.value = false; void router.push('/login') }
async function openBookingNotification(item: NotificationSnapshotItem): Promise<void> {
  try { await markBookingNotificationRead(item) }
  catch (error) { console.error(JSON.stringify({ event: 'notificationReadFailed', message: error instanceof Error ? error.message : 'Unknown error' })) }
  isNotificationOpen.value = false
  const tripId = item.notification.targetType === 'TRIP' ? item.notification.targetId : null
  if (tripId) await router.push({ path: '/console/dispatch', query: { tripId } })
}
function onLanguageKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') { isLanguageOpen.value = false; languageTrigger.value?.focus(); return }
  if (!isLanguageOpen.value || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return
  const options = Array.from(languageControl.value?.querySelectorAll<HTMLButtonElement>('[role="menuitemradio"]') ?? [])
  const current = options.indexOf(document.activeElement as HTMLButtonElement)
  const next = event.key === 'Home' ? 0 : event.key === 'End' ? options.length - 1 : event.key === 'ArrowDown' ? (current + 1) % options.length : (current - 1 + options.length) % options.length
  options[next]?.focus()
  event.preventDefault()
}
async function toggleLanguage(): Promise<void> {
  isAccountOpen.value = false
  isLanguageOpen.value = !isLanguageOpen.value
  if (isLanguageOpen.value) { await nextTick(); languageControl.value?.querySelector<HTMLButtonElement>('[aria-checked="true"]')?.focus() }
}
function chooseLanguage(value: Locale): void { setLocale(value); isLanguageOpen.value = false; languageTrigger.value?.focus() }
watch(currentSection, (section) => {
  for (const group of groups.value) {
    for (const item of group.items) {
      if (item.label === 'Booking Management' && item.children?.some((child) => child.path === section)) bookingMenuOpen.value = true
    }
  }
})
onMounted(() => { document.addEventListener('pointerdown', closeLanguageOnOutside); document.addEventListener('keydown', onDocumentKeydown); if (props.mode === 'console') void startStaffNotificationHub() })
onUnmounted(() => { document.removeEventListener('pointerdown', closeLanguageOnOutside); document.removeEventListener('keydown', onDocumentKeydown) })
</script>

<template>
  <div class="app-shell" :class="{ 'is-collapsed': isCollapsed, 'is-console': mode === 'console' }">
    <a class="skip-link" href="#main-content">{{ t('Skip to main content') }}</a>
    <header class="global-header">
      <button class="mobile-menu icon-button" :aria-label="t('Toggle navigation')" @click="isCollapsed = !isCollapsed"><Menu :size="19" /></button>
      <RouterLink class="brand" :to="mode === 'portal' ? '/portal/drivers' : '/console/manage'" :aria-label="t('LMS home')"><img class="brand-full" src="/assets/melco-logo-white.png" alt="Melco" /><img class="brand-symbol" src="/assets/melco-symbol.png" alt="Melco" /></RouterLink>
      <div class="header-spacer" />
      <label v-if="mode === 'portal'" class="header-search"><Search :size="17" /><input v-model="search" :aria-label="t('Search current module')" :placeholder="t('Search current module...')" @keydown.enter="emit('search', search)" /><kbd>⌘ K</kbd></label>
      <button v-else class="icon-button" :aria-label="t('Search')" @click="emit('focus-search')"><Search :size="18" /></button>
      <RouterLink v-if="mode === 'portal' || isDemoMode" class="icon-button notification-link" :to="mode === 'portal' ? '/portal/notifications' : '/console/notifications'" :aria-label="t('Notifications')"><Bell :size="18" /><span v-if="showNotificationDot" class="notification-dot" /></RouterLink>
      <div v-else ref="notificationControl" class="staff-notification-control">
        <button class="icon-button notification-link" type="button" :aria-label="notificationAriaLabel" aria-haspopup="menu" :aria-expanded="isNotificationOpen" @click="isNotificationOpen = !isNotificationOpen"><Bell :size="18" /><span v-if="notificationUnreadCount > 0" class="notification-count" aria-hidden="true">{{ notificationBadgeText }}</span></button>
        <div v-if="isNotificationOpen" class="staff-notification-menu" role="menu" :aria-label="t('Notifications')">
          <header><strong>{{ t('Notifications') }}</strong><span>{{ bookingNotificationUnreadCount }} {{ t('unread') }}</span></header>
          <button v-for="item in bookingNotifications" :key="item.eventId" type="button" role="menuitem" :class="{ unread: !item.isRead }" @click="openBookingNotification(item)"><span>{{ item.notification.summary }}</span><small>{{ new Date(item.notification.occurredAt).toLocaleString(dateLocale) }}</small></button>
          <p v-if="bookingNotifications.length === 0">{{ t('No notifications') }}</p>
        </div>
      </div>
      <span class="header-divider" />
      <div ref="languageControl" class="language-control" @keydown="onLanguageKeydown">
        <button ref="languageTrigger" type="button" class="language-trigger" :aria-label="t('Language')" aria-haspopup="menu" :aria-expanded="isLanguageOpen" @click="toggleLanguage">{{ locale === 'en' ? 'EN' : locale === 'zh-Hans' ? '简' : '繁' }} <ChevronDown :size="12" /></button>
        <div v-if="isLanguageOpen" class="language-menu" role="menu" :aria-label="t('Language')">
          <button type="button" role="menuitemradio" :aria-checked="locale === 'en'" @click="chooseLanguage('en')">English</button>
          <button type="button" role="menuitemradio" :aria-checked="locale === 'zh-Hant'" @click="chooseLanguage('zh-Hant')">繁體中文</button>
          <button type="button" role="menuitemradio" :aria-checked="locale === 'zh-Hans'" @click="chooseLanguage('zh-Hans')">简体中文</button>
        </div>
      </div>
      <span class="header-divider console-only" />
      <div ref="accountControl" class="account-control" @keydown="onAccountKeydown">
        <button ref="accountTrigger" type="button" class="user-chip" :aria-label="t('Staff account menu')" aria-haspopup="menu" :aria-expanded="isAccountOpen" @click="toggleAccount"><span class="user-avatar">{{ isDemoMode ? mode === 'portal' ? 'AL' : 'SL' : currentUser?.name?.split(' ').map((part) => part[0]).join('').slice(0, 2) ?? 'LM' }}</span><span class="user-copy"><strong>{{ isDemoMode ? mode === 'portal' ? 'Andrew Lau' : 'Sophie Leong' : currentUser?.name ?? 'LMS Staff' }}</strong><small v-if="mode === 'console'">{{ isDemoMode ? t('Front Desk Agent') : currentUser?.roleCode }}</small></span><ChevronDown :size="13" /></button>
        <div v-if="isAccountOpen" class="account-menu" role="menu" :aria-label="t('Staff account menu')">
          <div class="account-menu-heading"><strong>{{ isDemoMode ? mode === 'portal' ? 'Andrew Lau' : 'Sophie Leong' : currentUser?.name ?? 'LMS Staff' }}</strong><small>{{ isDemoMode ? mode === 'portal' ? t('Operations Manager · Local demo') : `FD-SL-003 · ${t('Front Desk')}` : currentUser?.roleCode }}</small></div>
          <template v-if="mode === 'console'">
            <button type="button" role="menuitem" @click="openAccountPanel('profile')"><UserRound :size="16" />{{ t('Staff profile') }}</button>
            <button type="button" role="menuitem" @click="openAccountSettings"><Settings2 :size="16" />{{ t('Console settings') }}</button>
            <button type="button" role="menuitem" @click="openAccountPanel('help')"><CircleHelp :size="16" />{{ t('Help & support') }}</button>
          </template>
          <button v-else type="button" role="menuitem" @click="openPortalAccount"><UserRound :size="16" />{{ t('User account & access') }} <span aria-hidden="true">→</span></button>
          <button v-if="!isDemoMode" type="button" role="menuitem" @click="signOut">{{ t('Sign out') }}</button>
        </div>
      </div>
    </header>
    <aside class="sidebar">
      <nav :aria-label="t('Main navigation')">
        <div v-for="group in groups" :key="group.label" class="nav-group">
          <div v-if="group.label" class="nav-heading">{{ t(group.label) }}</div>
          <template v-for="item in group.items" :key="item.label">
            <div v-if="item.children" class="nav-branch" :class="{ open: isBranchOpen(item) }">
              <button type="button" class="nav-item nav-parent" :class="{ 'has-active': isBranchActive(item) }" :aria-expanded="isBranchOpen(item)" :title="t(item.label)" @click="toggleBranch(item)">
                <component :is="item.icon" :size="17" :stroke-width="1.7" aria-hidden="true" /><span class="nav-label">{{ t(item.label) }}</span><ChevronDown class="nav-chevron" :size="14" aria-hidden="true" />
              </button>
              <div v-show="isBranchOpen(item)" class="nav-children">
                <RouterLink v-for="child in item.children" :key="child.path" class="nav-item nav-child" :class="{ active: currentSection === child.path }" :to="`/${mode}/${child.path}`" :title="t(child.label)" :aria-current="currentSection === child.path ? 'page' : undefined">
                  <component :is="child.icon" :size="15" :stroke-width="1.7" aria-hidden="true" /><span class="nav-label">{{ t(child.label) }}</span>
                </RouterLink>
              </div>
            </div>
            <RouterLink v-else class="nav-item" :class="{ active: currentSection === item.path }" :to="`/${mode}/${item.path}`" :title="t(item.label)" :aria-current="currentSection === item.path ? 'page' : undefined">
              <component :is="item.icon" :size="17" :stroke-width="1.7" aria-hidden="true" /><span class="nav-label">{{ t(item.label) }}</span><span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
            </RouterLink>
          </template>
        </div>
      </nav>
      <div class="sidebar-bottom">
        <button v-if="mode === 'portal'" class="sidebar-bottom-button" @click="isCollapsed = !isCollapsed"><Menu :size="17" /><span class="nav-label">{{ t(isCollapsed ? 'Expand navigation' : 'Collapse navigation') }}</span></button>
        <button v-else class="sidebar-bottom-button" @click="isSettingsOpen = true"><Settings2 :size="17" /><span class="nav-label">{{ t('Console Settings') }}</span></button>
      </div>
    </aside>
    <main id="main-content" class="main-content" :aria-label="t(pageTitle)" tabindex="-1"><slot /></main>
    <div v-if="isSettingsOpen" class="modal-backdrop"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="t('Console settings')"><header class="dialog-header"><h2>{{ t('Console Settings') }}</h2><button class="icon-button" :aria-label="t('Close')" @click="isSettingsOpen = false"><X :size="18" /></button></header><div class="dialog-body"><label class="field-label">{{ t('Station') }}<select v-model="station"><option value="Front Desk">{{ t('Front Desk') }}</option><option value="Concierge">{{ t('Concierge') }}</option></select></label><label class="field-label">{{ t('Default notification channel') }}<select v-model="defaultNotificationChannel"><option value="SMS">SMS</option><option value="IN_ROOM_MESSAGE">{{ t('In-room message') }}</option></select></label><p class="muted">{{ t(isDemoMode ? 'Preferences apply to this local demo only.' : 'Preferences are saved to your LMS account.') }}</p></div><footer class="dialog-footer"><p v-if="settingsError" class="form-error" role="alert">{{ settingsError }}</p><button class="primary-button" @click="saveAccountSettings">{{ t('SAVE PREFERENCES') }}</button></footer></section></div>
    <div v-if="accountPanel" class="modal-backdrop account-panel-backdrop"><section class="dialog account-panel" role="dialog" aria-modal="true" :aria-label="t(accountPanel === 'profile' ? 'Staff Profile' : 'Help & Support')"><header class="dialog-header"><h2>{{ t(accountPanel === 'profile' ? 'Staff Profile' : 'Help & Support') }}</h2><button ref="accountClose" type="button" class="icon-button" :aria-label="t('Close')" @click="closeAccountPanel"><X :size="18" /></button></header><div v-if="accountPanel === 'profile'" class="dialog-body"><div class="account-profile-heading"><span class="user-avatar">SL</span><h3>{{ isDemoMode ? 'Sophie Leong' : currentUser?.name }}</h3></div><dl class="account-profile-facts"><div><dt>{{ t('Role') }}</dt><dd>{{ t('Front Desk Agent') }}</dd></div><div><dt>{{ t('Staff ID') }}</dt><dd>{{ isDemoMode ? 'FD-SL-003' : currentUser?.userId }}</dd></div><div><dt>{{ t('Property / Station') }}</dt><dd>City of Dreams / FD-03</dd></div></dl><p>{{ t('All bookings and changes are tagged with your staff identity. Manager decisions require separate authorization.') }}</p><button type="button" class="secondary-button account-panel-action" @click="accountPanel = null; isSettingsOpen = true">{{ t('CONSOLE SETTINGS') }}</button></div><div v-else class="dialog-body account-help"><p>{{ t('For transfer assistance, contact Concierge Operations at') }} <strong>{{ t('ext. 8888') }}</strong>.</p><p>{{ t('For approval requests, contact the on-duty Front Desk Manager.') }}</p><p>{{ t('Support available 24 hours a day.') }}</p></div><footer v-if="accountPanel === 'profile'" class="account-panel-footer">{{ t('Booked by: Sophie Leong · Front Desk') }}</footer></section></div>
  </div>
</template>
