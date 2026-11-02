import { defineConfig, loadEnv } from 'vite'
import uniPlugin from '@dcloudio/vite-plugin-uni'
import { sharedMapDefines } from '../map.env'

const uni = typeof uniPlugin === 'function'
  ? uniPlugin
  : (uniPlugin as unknown as { default: typeof uniPlugin }).default

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // 开发代理的后端地址：默认 melco1；本地可在 .env 中用 VITE_LMS_API_PROXY_TARGET 改指向（例如 65 网关 http://192.168.30.65:5088）。
  const apiProxyTarget = env.VITE_LMS_API_PROXY_TARGET || 'https://melco1.weprogram.site'
  return {
    plugins: [uni()],
    define: sharedMapDefines(mode, process.cwd()),
    server: {
      host: env.VITE_DEV_SERVER_HOST || '0.0.0.0',
      port: 5193,
      strictPort: true,
      cors: false,
      proxy: { '/api': { target: apiProxyTarget, changeOrigin: true, ws: true } },
    },
    preview: { host: '0.0.0.0', port: 5193, strictPort: true },
    resolve: { alias: { '@': '/src' } },
  }
})
