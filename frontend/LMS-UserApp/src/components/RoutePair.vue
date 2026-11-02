<script setup lang="ts">
defineProps<{ pickupName: string; dropoffName: string }>()
</script>

<template>
  <view class="route-pair">
    <view class="route-stop">
      <view class="route-marker is-pickup" />
      <text class="route-name">{{ pickupName }}</text>
    </view>
    <view class="route-stop">
      <view class="route-marker is-dropoff" />
      <text class="route-name">{{ dropoffName }}</text>
    </view>
  </view>
</template>

<style scoped>
.route-pair { position: relative; display: flex; flex-direction: column; gap: 6px; margin: 14px 0; }
.route-pair::before { position: absolute; top: 14px; bottom: 14px; left: 8px; border-left: 1px solid #d8a347; content: ''; }
.route-stop { display: grid; grid-template-columns: 16px minmax(0, 1fr); align-items: center; gap: 10px; min-height: 22px; }
.route-marker { position: relative; z-index: 1; width: 8px; height: 8px; margin-left: 4px; border-radius: 50%; }
.is-pickup { background: #d8a347; }
.is-dropoff { border: 1px solid #d8a347; background: #fff; }
.route-name { display: block; min-width: 0; color: #32394b; font-size: 13px; line-height: 22px; overflow-wrap: anywhere; }
</style>
