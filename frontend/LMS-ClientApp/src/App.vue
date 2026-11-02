<script setup lang="ts">
import { onHide, onLaunch, onShow } from '@dcloudio/uni-app'
import { useBookingStore } from '@/stores/booking'
import { startCustomerNotificationHub, stopCustomerNotificationHub } from '@/api/notifications'
import { initializeCustomerNoticeStore, refreshCurrentCustomerTrip } from '@/data/customerNoticeStore'
import { registerSystemPushListener } from '../../shared/systemNotifications'

onLaunch(() => {
  registerSystemPushListener('LMS Client')
  useBookingStore().restore()
  initializeCustomerNoticeStore()
})
onShow(() => { void startCustomerNotificationHub(); void refreshCurrentCustomerTrip(true) })
// 网页（H5）切到其他标签页、最小化或被其他窗口遮挡都会触发 onHide；此时断开会漏掉期间的行程推送，
// 因此网页端保持连接，只有原生 App 进入后台时才断开，回到前台由 onShow 重连并刷新。
onHide(() => {
  // #ifndef H5
  void stopCustomerNotificationHub()
  // #endif
})
</script>

<style lang="scss">
page {
  width: 100%;
  min-height: 100%;
  background: #f6f6f5;
  color: #171717;
  font-family: Inter, "SF Pro Display", "PingFang SC", "Microsoft YaHei", Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
}

view,
text,
button,
uni-button,
input,
textarea,
scroll-view,
image {
  box-sizing: border-box;
}

button,
uni-button {
  margin: 0;
  padding: 0;
  border: 0;
  border-radius: 0;
  display: flex !important;
  align-items: center;
  justify-content: center;
  line-height: normal !important;
  background: transparent;
}

button::after,
uni-button::after {
  border: 0 !important;
}

input,
textarea {
  color: #171717;
}
</style>
