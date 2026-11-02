<script setup lang="ts">
import type { Trip } from '@/api/staff'
import { formatDateTime, statusLabel } from '@/api/presentation'
import RoutePair from './RoutePair.vue'

defineProps<{ trip: Trip }>()

function openTrip(tripId: string): void {
  uni.navigateTo({ url: `/pages/trip-detail/index?tripId=${encodeURIComponent(tripId)}` })
}
</script>

<template>
  <view class="card trip-card" @tap="openTrip(trip.tripId)">
    <view class="trip-card-header">
      <text class="trip-reference">{{ trip.tripReference }}</text>
      <text class="chip">{{ statusLabel(trip.status) }}</text>
    </view>
    <RoutePair :pickup-name="trip.route.pickupName" :dropoff-name="trip.route.dropoffName" />
    <text class="trip-meta">{{ trip.guest.name }} · {{ formatDateTime(trip.pickupAt) }}</text>
  </view>
</template>

<style scoped>
.trip-card-header { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.trip-reference { color: #252d43; font-size: 16px; font-weight: 700; }
.trip-meta { display: block; color: #8790a1; font-size: 12px; }
</style>
