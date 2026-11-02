import { resolve } from 'node:path'
import { loadEnv } from 'vite'

/** Inject one shared, untracked map environment into both independently built apps. */
export function sharedMapDefines(mode: string, appDirectory: string): Record<string, string> {
  const environment = loadEnv(mode, resolve(appDirectory, '..'), 'VITE_AMAP_')
  return {
    __LMS_MAP_WEB_KEY__: JSON.stringify(environment.VITE_AMAP_WEB_KEY ?? ''),
    __LMS_MAP_SECURITY_JS_CODE__: JSON.stringify(environment.VITE_AMAP_SECURITY_JS_CODE ?? ''),
  }
}
