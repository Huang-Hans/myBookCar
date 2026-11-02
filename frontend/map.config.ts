/** Shared AMap settings for the client and driver applications. */
declare const __LMS_MAP_WEB_KEY__: string
declare const __LMS_MAP_SECURITY_JS_CODE__: string

export const mapConfig = Object.freeze({
  provider: 'amap' as const,
  webKey: __LMS_MAP_WEB_KEY__.trim(),
  securityJsCode: __LMS_MAP_SECURITY_JS_CODE__.trim(),
})
