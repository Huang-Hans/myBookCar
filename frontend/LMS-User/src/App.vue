<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue'
import { startStaffNotificationHub } from './api/notifications'

// 应用级启动 SignalR 连接：登录后即可接收实时通知；回到前台时兜底重连，不用等 ShellLayout 挂载。
// 未登录时 start 内部直接返回；断线由集线器自动重连。
function handleVisibility(): void {
  if (document.visibilityState === 'visible') void startStaffNotificationHub()
}
onMounted(() => {
  void startStaffNotificationHub()
  document.addEventListener('visibilitychange', handleVisibility)
})
onBeforeUnmount(() => { document.removeEventListener('visibilitychange', handleVisibility) })
</script>

<template>
  <RouterView />
</template>
