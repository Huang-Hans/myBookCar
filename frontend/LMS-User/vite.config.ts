import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  // 开发代理的后端地址：默认 melco1；本地可在 .env 中用 VITE_API_PROXY_TARGET 改指向（例如 65 网关 http://192.168.30.65:5088）。
  const apiProxyTarget = env.VITE_API_PROXY_TARGET || 'https://melco1.weprogram.site'

  return {
    plugins: [vue()],
    server: { port: 5175, strictPort: true, cors: false, proxy: { '/api': { target: apiProxyTarget, changeOrigin: true, ws: true } } },
    test: { environment: 'jsdom', globals: true },
  }
})
