import { createSSRApp } from 'vue'
import App from './App.vue'
import './styles.scss'
import './uni.scss'

export function createApp() {
  return { app: createSSRApp(App) }
}
