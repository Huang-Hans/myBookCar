<script setup lang="ts">
import { onHide, onLaunch, onShow } from '@dcloudio/uni-app'
import { startDriverNotificationHub, stopDriverNotificationHub } from '@/api/notifications'
import { registerSystemPushListener } from '../../shared/systemNotifications'

onLaunch(() => { registerSystemPushListener('LMS Driver') })
onShow(() => { void startDriverNotificationHub() })
onHide(() => { void stopDriverNotificationHub() })
</script>

<style lang="scss">
page { min-height: 100%; background: #f8f8f8; color: #111; font-family: Arial, "PingFang SC", sans-serif; }
view, text, image, button, uni-button, input, textarea { box-sizing: border-box; }
button, uni-button { margin: 0; padding: 0; border: 0; border-radius: 0; display: flex !important; align-items: center; justify-content: center; line-height: normal !important; background: transparent; }
button::after, uni-button::after { border: 0 !important; }
</style>
