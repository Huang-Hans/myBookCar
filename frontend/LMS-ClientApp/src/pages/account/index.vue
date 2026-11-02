<script setup lang="ts">
import { t } from '@/i18n/locale'

import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import AppTabBar from '@/components/AppTabBar.vue'
import { isTripBannerVisible } from '@/data/customerNoticeStore'
import AppBottomSheet from '@/components/AppBottomSheet.vue'
import { consumeTabRefresh } from '@/data/tabNavigation'
import { maskPhoneForDisplay, presentMembership } from '@/data/accountPresentation'
import { defaultFavoriteLocations, saveFavoriteLocations } from '@/data/favoriteLocations'
import { ApiRequestError, logoutCustomer } from '@/api/http'
import { customerApi, type CustomerAccount, type CustomerLocation, type CustomerPreferences, type CustomerSummary, type LocationFavorites, type ReviewableTrip } from '@/api/customer'
import { stopCustomerNotificationHub } from '@/api/notifications'
import { clearCustomerNotices, setCurrentCustomerTrip } from '@/data/customerNoticeStore'
import { setLocale } from '@/i18n/locale'

interface PreviewProfile { fullName: string; email: string; phone: string; language: string }
const saved = uni.getStorageSync('lmsClientPreviewProfile') as PreviewProfile | ''
const profile = ref<PreviewProfile>(saved && typeof saved === 'object' ? saved : { fullName: 'Alex Morgan', email: 'alex.morgan@email.com', phone: '+852 6123 4567', language: 'English' })
const section = ref<'main' | 'profile' | 'favorites' | 'feedback'>('main')
const notifications = ref(uni.getStorageSync('lmsClientNotifications') !== false)
const feedback = ref('')
const favoriteLocations = ref({ ...defaultFavoriteLocations })
const account = ref<CustomerAccount | null>(null)
const customerSummary = ref<CustomerSummary | null>(null)
const preferences = ref<CustomerPreferences | null>(null)
const locations = ref<CustomerLocation[]>([])
const pickupFavorites = ref<LocationFavorites | null>(null)
const dropoffFavorites = ref<LocationFavorites | null>(null)
const reviewableTrips = ref<ReviewableTrip[]>([])
const rating = ref(5)
const activeSheet = ref<'language' | 'favoritePickup' | 'favoriteDropoff' | 'signout' | 'error' | null>(null)
const pendingOption = ref('')
const sheetError = ref('')
const editableName = ref(profile.value.fullName)
const editableEmail = ref(profile.value.email)
const initials = computed(() => profile.value.fullName.split(' ').map((part) => part[0]).join('').slice(0, 2))
const locationNames = computed(() => locations.value.map((item) => item.name))
const membership = computed(() => presentMembership(account.value, customerSummary.value))
const displayPhone = computed(() => maskPhoneForDisplay(profile.value.phone))
let hasLoadedMain = false
let hasLoadedFavorites = false
let hasLoadedReviews = false
onShow(() => {
  // Data loads when the Account bottom-navigation tab is tapped (or on the first open), not on every page show.
  if (consumeTabRefresh('account') || !hasLoadedMain) { hasLoadedMain = true; void loadAccountMain() }
})
// Each section fetches only its own data, and only the first time it is opened.
function openSection(value: typeof section.value): void {
  section.value = value
  if (value === 'favorites' && !hasLoadedFavorites) { hasLoadedFavorites = true; void loadFavorites() }
  if (value === 'feedback' && !hasLoadedReviews) { hasLoadedReviews = true; void loadReviews() }
}
async function loadAccountMain(): Promise<void> {
  const [profileResult, summaryResult, preferenceResult] = await Promise.allSettled([
    customerApi.getProfile(), customerApi.getSelf(), customerApi.getPreferences(),
  ])

  if (summaryResult.status === 'fulfilled') {
    customerSummary.value = summaryResult.value.data
    setCurrentCustomerTrip(summaryResult.value.data.currentTrip)
  }
  if (profileResult.status === 'fulfilled') {
    const data = profileResult.value.data
    account.value = data
    profile.value = { ...profile.value, fullName: data.name, email: data.email ?? '', phone: data.phoneMasked ?? '' }
    editableName.value = data.name
    editableEmail.value = data.email ?? ''
  } else if (summaryResult.status === 'fulfilled') {
    profile.value.fullName = summaryResult.value.data.name
    editableName.value = summaryResult.value.data.name
  }
  if (preferenceResult.status === 'fulfilled') {
    const data = preferenceResult.value.data
    preferences.value = data
    profile.value.language = data.language === 'ZH_HANT' ? '繁體中文' : data.language === 'ZH_HANS' ? '简体中文' : 'English'
    setLocale(data.language)
    notifications.value = data.isPushNotificationsEnabled
  }

  if (profileResult.status === 'rejected' && summaryResult.status === 'rejected') {
    const error = profileResult.reason instanceof ApiRequestError ? profileResult.reason : summaryResult.reason instanceof ApiRequestError ? summaryResult.reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load account.'), icon: 'none' })
  }
}
async function loadFavorites(): Promise<void> {
  const [locationResult, pickupResult, dropoffResult] = await Promise.allSettled([
    customerApi.listLocations({ page: 1, pageSize: 50 }),
    customerApi.getFavorites('PICKUP'), customerApi.getFavorites('DROPOFF'),
  ])
  if (locationResult.status === 'fulfilled') locations.value = locationResult.value.data
  if (pickupResult.status === 'fulfilled') {
    pickupFavorites.value = pickupResult.value.data
    favoriteLocations.value.pickupLocation = pickupResult.value.data.items[0]?.name ?? ''
  }
  if (dropoffResult.status === 'fulfilled') {
    dropoffFavorites.value = dropoffResult.value.data
    favoriteLocations.value.dropoffLocation = dropoffResult.value.data.items[0]?.name ?? ''
  }
  if (locationResult.status === 'rejected' && pickupResult.status === 'rejected') {
    const error = locationResult.reason instanceof ApiRequestError ? locationResult.reason : pickupResult.reason instanceof ApiRequestError ? pickupResult.reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load favorite locations.'), icon: 'none' })
  }
}
async function loadReviews(): Promise<void> {
  try {
    reviewableTrips.value = (await customerApi.listReviewableTrips()).data
  } catch (reason) {
    const error = reason instanceof ApiRequestError ? reason : null
    uni.showToast({ title: error ? `${error.message} · ${error.traceId}` : t('Unable to load reviews.'), icon: 'none' })
  }
}
function openLanguageSheet(): void {
  pendingOption.value = profile.value.language === '繁體中文' ? 'Traditional Chinese' : profile.value.language === '简体中文' ? 'Simplified Chinese' : 'English'
  activeSheet.value = 'language'
}
async function changeLanguage(): Promise<void> {
  const language = pendingOption.value === 'Traditional Chinese' ? 'ZH_HANT' : pendingOption.value === 'Simplified Chinese' ? 'ZH_HANS' : 'EN'
  try {
    const response = await customerApi.updatePreferences({ language }, preferences.value?.version)
    preferences.value = response.data
    profile.value.language = language === 'ZH_HANT' ? '繁體中文' : language === 'ZH_HANS' ? '简体中文' : 'English'
    setLocale(language)
    activeSheet.value = null
  } catch (reason) { showApiError(reason, t('Unable to save language.')) }
}
async function changeNotifications(): Promise<void> {
  try {
    const response = await customerApi.updatePreferences({ isPushNotificationsEnabled: !notifications.value }, preferences.value?.version)
    preferences.value = response.data
    notifications.value = response.data.isPushNotificationsEnabled
  } catch (reason) { showApiError(reason, 'Unable to save notifications.') }
}
async function signOut(): Promise<void> {
  try { await stopCustomerNotificationHub(); await logoutCustomer() }
  finally { clearCustomerNotices(); uni.reLaunch({ url: '/pages/login/index' }) }
}
function openFavoriteSheet(usage: 'PICKUP' | 'DROPOFF'): void {
  pendingOption.value = usage === 'PICKUP' ? favoriteLocations.value.pickupLocation : favoriteLocations.value.dropoffLocation
  activeSheet.value = usage === 'PICKUP' ? 'favoritePickup' : 'favoriteDropoff'
}
function confirmFavoriteSheet(): void {
  if (!locations.value.some((item) => item.name === pendingOption.value)) return
  if (activeSheet.value === 'favoritePickup') favoriteLocations.value.pickupLocation = pendingOption.value
  else if (activeSheet.value === 'favoriteDropoff') favoriteLocations.value.dropoffLocation = pendingOption.value
  activeSheet.value = null
}
async function saveProfile(): Promise<void> {
  if (!editableName.value.trim()) { uni.showToast({ title: t('Name is required.'), icon: 'none' }); return }
  try {
    const response = await customerApi.updateProfile({ name: editableName.value.trim(), email: editableEmail.value.trim() || null }, account.value?.version)
    account.value = response.data
    profile.value.fullName = response.data.name
    profile.value.email = response.data.email ?? ''
    section.value = 'main'
    uni.showToast({ title: t('Profile updated'), icon: 'none' })
  } catch (reason) { showApiError(reason, 'Unable to update profile.') }
}
async function saveFeedback(): Promise<void> {
  if (!feedback.value.trim()) { uni.showToast({ title: t('Enter feedback before saving'), icon: 'none' }); return }
  const trip = reviewableTrips.value.find((item) => item.canReview)
  if (!trip) { uni.showToast({ title: t('There is no journey available to review.'), icon: 'none' }); return }
  try {
    await customerApi.saveReview(trip.tripId, rating.value, feedback.value.trim(), trip.review?.version)
    feedback.value = ''; section.value = 'main'; uni.showToast({ title: t('Feedback submitted'), icon: 'none' }); await loadReviews()
  } catch (reason) { showApiError(reason, 'Unable to submit feedback.') }
}
async function saveFavoriteLocationSettings(): Promise<void> {
  try {
    const pickup = locations.value.find((item) => item.name === favoriteLocations.value.pickupLocation)
    const dropoff = locations.value.find((item) => item.name === favoriteLocations.value.dropoffLocation)
    if (!pickup || !dropoff) throw new Error('Choose locations from the catalog.')
    const [pickupResponse, dropoffResponse] = await Promise.all([
      customerApi.replaceFavorites('PICKUP', [pickup.locationId], pickupFavorites.value?.version),
      customerApi.replaceFavorites('DROPOFF', [dropoff.locationId], dropoffFavorites.value?.version),
    ])
    pickupFavorites.value = pickupResponse.data; dropoffFavorites.value = dropoffResponse.data
    favoriteLocations.value = saveFavoriteLocations(favoriteLocations.value)
    uni.showToast({ title: t('Favorite locations saved'), icon: 'none' })
  } catch (reason) { showApiError(reason, reason instanceof Error ? reason.message : 'Unable to save favorites.') }
}
function showApiError(reason: unknown, fallback: string): void {
  const error = reason instanceof ApiRequestError ? reason : null
  sheetError.value = error ? `${error.message}\nTrace ID: ${error.traceId}` : fallback
  activeSheet.value = 'error'
}
</script>

<template>
  <view class="account-page"><view class="header"><button v-if="section !== 'main'" @tap="section = 'main'">‹</button><text>{{ $t(section === 'main' ? 'Account' : section === 'profile' ? 'Edit Profile' : section === 'favorites' ? 'Favorite Locations' : 'Ratings and Feedback') }}</text></view><scroll-view scroll-y class="scroll" :class="{ 'with-trip-banner': isTripBannerVisible }"><view class="body">
    <template v-if="section === 'main'"><view class="profile"><view class="avatar">{{ initials }}</view><view><text class="name">{{ profile.fullName }}</text><text class="muted">{{ $t("Member No") }} {{ membership.memberId }}</text></view></view><view class="tier" :class="{ 'membership-unavailable': !membership.hasMembershipData }"><text class="dragon-badge">{{ membership.tierCode }}</text><view class="membership-points"><text class="points-number">{{ membership.pointsBalance }}</text><text class="points-caption">{{ $t(membership.hasMembershipData ? 'Loyalty Points' : 'Membership data unavailable') }}</text></view><image class="membership-card-image" src="/static/figma/dragon-card.png" mode="aspectFit" /></view><view class="info-card"><view><text>{{ $t("Full Name") }}</text><text>{{ profile.fullName }}</text></view><view><text>{{ $t("Email") }}</text><text>{{ profile.email || '—' }}</text></view><view><text>{{ $t("Phone") }}</text><text>{{ displayPhone }}</text></view></view><button class="menu-row" @tap="section = 'profile'"><text>{{ $t("Edit Profile") }}</text><text>›</text></button><button class="menu-row" @tap="openLanguageSheet"><text>{{ $t("Language") }}</text><text>{{ profile.language }}　›</text></button><button class="menu-row" @tap="openSection('favorites')"><text>{{ $t("Favorite Locations") }}</text><text>›</text></button><view class="notification-card"><view><text>{{ $t("Push Notifications") }}</text><text class="muted">{{ $t("Receive Booking Confirmation, Driver Assignment, Driver Arrival, Trip Reminder, Points Earned, and Promotional Notifications") }}</text></view><button class="toggle" :class="{ enabled: notifications }" role="switch" :aria-checked="notifications" :aria-label='$t("Push Notifications")' @tap="changeNotifications"><text /></button></view><button class="menu-row" @tap="openSection('feedback')"><text>{{ $t("Ratings and Feedback") }}</text><text>›</text></button><button class="sign-out" @tap="activeSheet = 'signout'">{{ $t("Sign Out") }}</button></template>
    <template v-else-if="section === 'profile'"><text class="section-title">{{ $t("Full Name") }}</text><view class="favorite-field"><input v-model="editableName" :aria-label='$t("Full Name")' /></view><text class="section-title">{{ $t("Email") }}</text><view class="favorite-field"><input v-model="editableEmail" :aria-label='$t("Email")' /></view><text class="muted intro">{{ $t("Phone changes require a verification flow and are not available in the current API.") }}</text><button class="primary favorite-save" @tap="saveProfile">{{ $t("Save Profile") }}</button></template>
    <template v-else-if="section === 'favorites'"><text class="muted intro">{{ $t("Your Usual Places, Always Close at Hand") }}</text><text class="section-title">{{ $t("Favorite Pickup Location") }}</text><button class="favorite-field favorite-choice" @tap="openFavoriteSheet('PICKUP')">{{ favoriteLocations.pickupLocation || $t('Choose pickup location') }} <text>›</text></button><text class="section-title">{{ $t("Favorite Drop-Off Location") }}</text><button class="favorite-field favorite-choice" @tap="openFavoriteSheet('DROPOFF')">{{ favoriteLocations.dropoffLocation || $t('Choose drop-off location') }} <text>›</text></button><button class="primary favorite-save" @tap="saveFavoriteLocationSettings">{{ $t("Save Favorite Locations") }}</button></template>
    <template v-else><text class="section-title">{{ $t("Tell Us About Your Journey") }}</text><view class="rating-stars" role="group" :aria-label="$t('Rating:')"><button v-for="star in [1, 2, 3, 4, 5]" :key="star" :aria-label="String(star)" :aria-pressed="star <= rating" :class="{ selected: star <= rating }" @tap="rating = star">★</button></view><textarea v-model="feedback" class="feedback" :placeholder='$t("Share your feedback")' /><button class="primary" @tap="saveFeedback">{{ $t("Save Feedback") }}</button></template>
  </view></scroll-view><AppTabBar active="account" />
    <AppBottomSheet :open="activeSheet === 'language'" :title="$t('Language')" :options="['English', 'Traditional Chinese', 'Simplified Chinese']" :selected="pendingOption" :confirm-label="$t('Confirm')" @select="pendingOption = $event" @close="activeSheet = null" @confirm="changeLanguage" />
    <AppBottomSheet :open="activeSheet === 'favoritePickup' || activeSheet === 'favoriteDropoff'" :title="$t(activeSheet === 'favoritePickup' ? 'Favorite Pickup Location' : 'Favorite Drop-Off Location')" :options="locationNames" :selected="pendingOption" :search-placeholder="$t('Search or Enter a Location')" :confirm-label="$t('Confirm')" :is-confirm-disabled="!locationNames.includes(pendingOption)" @select="pendingOption = $event" @close="activeSheet = null" @confirm="confirmFavoriteSheet" />
    <AppBottomSheet :open="activeSheet === 'signout'" :title="$t('Sign Out?')" :confirm-label="$t('Sign Out')" @close="activeSheet = null" @confirm="signOut"><text>{{ $t('You Can Sign In to Access Your Melco Club Account') }}</text></AppBottomSheet>
    <AppBottomSheet :open="activeSheet === 'error'" :title="$t('Request Failed')" :confirm-label="$t('OK')" @close="activeSheet = null" @confirm="activeSheet = null"><text>{{ sheetError }}</text></AppBottomSheet>
  </view>
</template>

<style scoped lang="scss">
.account-page { min-height:100vh; background:#f8f8fa; }.header { display:flex; height:78px; align-items:center; justify-content:center; background:#fff; font-size:18px; font-weight:600; }.header button { position:absolute; left:17px; color:#eba321; font-size:32px; }.scroll { height:calc(100vh - 134px); } .scroll.with-trip-banner { height:calc(100vh - 186px); }.body { padding:22px 16px 90px; }.profile { display:flex; padding:16px; align-items:center; gap:14px; border-radius:12px; background:#fff; }.avatar { display:flex; width:54px; height:54px; align-items:center; justify-content:center; border-radius:50%; background:#edac32; color:#fff; font-size:20px; }.profile>view:last-child { display:flex; flex-direction:column; gap:5px; }.name { font-size:19px; }.muted { color:#9299ab; font-size:12px; line-height:1.5; }.tier { display:flex; margin:17px 0; padding:18px; gap:5px; flex-direction:column; border-radius:12px; background:#e9aa36; color:#fff; }.tier text:first-child { font-size:16px; }.tier text:nth-child(2) { font-size:29px; font-weight:700; }.tier small { font-size:12px; }.info-card { padding:6px 16px; border-radius:12px; background:#fff; }.info-card view { display:flex; min-height:50px; align-items:center; justify-content:space-between; border-bottom:1px solid #eee; font-size:14px; }.info-card view:last-child { border:0; }.info-card view text:first-child { color:#9399a8; }.menu-row { display:flex; width:100%; min-height:54px; margin-top:12px; padding:0 16px; align-items:center; justify-content:space-between; border-radius:11px; background:#fff; text-align:left; font-size:15px; }.menu-row text:last-child { color:#949aaa; }.notification-card { display:flex; margin-top:12px; padding:16px; align-items:center; justify-content:space-between; gap:10px; border-radius:11px; background:#fff; }.notification-card>view { display:flex; gap:7px; flex-direction:column; }.toggle { display:flex; width:44px; height:26px; padding:3px; align-items:center; border-radius:15px; background:#ccc; }.toggle.enabled { background:#f1ac2b; }.toggle text { width:20px; height:20px; border-radius:50%; background:#fff; }.toggle.enabled text { margin-left:auto; }.sign-out { width:100%; height:48px; margin-top:20px; border:1px solid #e8e8eb; border-radius:10px; background:#fff; color:#ba5050; }.intro { display:block; margin-bottom:27px; }.section-title { display:block; margin:23px 0 12px; font-size:18px; }.feedback { width:100%; height:150px; padding:15px; border:1px solid #e6e6eb; border-radius:11px; background:#fff; }.primary { width:100%; height:50px; margin-top:18px; border-radius:25px; background:#f1ac2b; color:#fff; }.preview-note { display:block; margin:18px 0; color:#aaa; text-align:center; font-size:11px; }
.account-page .header{background:#f8f8fa}.account-page .body{padding-top:18px}.profile{padding:6px 0 10px;border-radius:0;background:transparent}.avatar{width:54px;height:54px;border:1px solid #e9d7b5;background:#f3e9d8;color:#a9782d;font-size:13px}.tier{height:184px;margin:14px 0 12px;padding:28px 24px;gap:0;background:#073d30}.tier text:first-child{align-self:flex-start;padding:2px 9px;border-radius:3px;background:#05b993;font-size:12px}.tier text:nth-child(2){margin-top:25px;font-size:30px;font-weight:400}.tier small{margin-top:3px}.info-card{padding:0 16px}.info-card view{display:flex;min-height:79px;align-items:flex-start;justify-content:center;flex-direction:column;gap:10px}.info-card view text:last-child{font-size:16px}.menu-row{min-height:51px}.preview-note{display:none}
.tier{position:relative;display:block;box-sizing:border-box;height:184px;padding:24px 132px 24px 24px;overflow:hidden;border-radius:12px;background:#073e31 url('/static/figma/dragon-bg.png') 50% 58% / cover no-repeat}
.tier .dragon-badge{display:inline-block;margin-top:4px;padding:2px 8px;border-radius:3px;background:#11c795;color:#fff;font-size:12px;font-weight:400;line-height:15px}
.tier.membership-unavailable .dragon-badge{background:#667d76;font-size:11px}.tier.membership-unavailable .membership-card-image{opacity:.45}
.membership-points{position:relative;display:flex;margin-top:24px;gap:6px;flex-direction:column;color:#fff}
.tier .membership-points .points-number{margin:0;padding:0;background:transparent;color:#fff;font-size:28px;font-weight:700;line-height:34px}
.tier .membership-points .points-caption{margin:0;padding:0;background:transparent;color:#fff;font-size:13px;font-weight:400;line-height:18px}
.membership-card-image{position:absolute;top:24px;right:24px;width:88px;height:137px;border-radius:4px}
.notification-card>view{min-width:0;flex:1}.toggle{box-sizing:border-box;flex:0 0 44px;margin:0 0 0 auto;justify-content:flex-start}.toggle::after{display:none}
.favorite-field{display:flex;min-height:54px;padding:0 16px;align-items:center;border-radius:11px;background:#fff}.favorite-field input{width:100%;height:40px;font-size:15px}.favorite-save{margin-top:28px}
.favorite-choice{display:flex;width:100%;align-items:center;justify-content:space-between;text-align:left;background:#fff}.favorite-choice text{color:#9299ab}.rating-stars{display:flex;gap:10px}.rating-stars button{min-width:46px;height:46px;background:transparent;color:#c7c7cc;font-size:32px}.rating-stars button.selected{color:#f1ac2b}
</style>
