<template><view /></template>

<script setup lang="ts">
import { onHide, onLaunch, onShow } from '@dcloudio/uni-app'
import { startSystemNoticeMonitor, stopSystemNoticeMonitor } from '@/api/systemNoticeMonitor'
import { registerSystemPushListener } from '../../shared/systemNotifications'

onLaunch(() => { registerSystemPushListener('LMS Operations') })
onShow(startSystemNoticeMonitor)
onHide(stopSystemNoticeMonitor)
</script>

<style lang="scss">
page { min-height: 100%; background: #f5f6f9; color: #1f2434; font-family: Inter, "PingFang SC", "Microsoft YaHei", Arial, sans-serif; }
view, uni-view, text, uni-text, button, uni-button, input, textarea, picker { box-sizing: border-box; }
button, uni-button { margin: 0; padding: 0; display: flex !important; align-items: center; justify-content: center; line-height: normal !important; text-align: center; }
button::after, uni-button::after { border: 0 !important; }
</style>
