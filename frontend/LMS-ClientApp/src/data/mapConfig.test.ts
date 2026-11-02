import { describe, expect, it, vi } from 'vitest'

describe('shared map configuration', () => {
  it('provides the AMap settings injected from the shared environment', async () => {
    vi.stubGlobal('__LMS_MAP_WEB_KEY__', '  test-key  ')
    vi.stubGlobal('__LMS_MAP_SECURITY_JS_CODE__', '  test-code  ')
    const { mapConfig } = await import('../../../map.config')
    expect(mapConfig.provider).toBe('amap')
    expect(mapConfig.webKey).toBe('test-key')
    expect(mapConfig.securityJsCode).toBe('test-code')
  })
})
