import { describe, expect, it } from 'vitest'
import source from './AmapRouteMap.vue?raw'

describe('AMap JSAPI loading', () => {
  it('attempts the JSAPI before considering a tile fallback when securityJsCode is absent', () => {
    const start = source.indexOf('async function renderInteractiveMap(')
    const sdkCall = source.indexOf('const AMap = await loadAmap(props.apiKey, props.securityJsCode)', start)
    expect(start).toBeGreaterThan(-1)
    expect(sdkCall).toBeGreaterThan(start)
    const beforeSdkCall = source.slice(start, sdkCall)
    expect(beforeSdkCall).not.toContain('renderAmapTileMap(')
    expect(beforeSdkCall).not.toContain('!props.securityJsCode.trim()')
    expect(source).toContain('if (securityJsCode.trim()) amapWindow._AMapSecurityConfig')
  })
})
