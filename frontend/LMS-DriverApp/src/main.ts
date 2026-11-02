import { createSSRApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { t } from './i18n/locale'

export function createApp() {
  const app = createSSRApp(App)
  app.use(createPinia())
  app.config.globalProperties.$t = t
  return { app }
}
