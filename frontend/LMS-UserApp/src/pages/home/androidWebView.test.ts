import { describe, expect, it } from 'vitest'
import mainActivity from '../../../android/app/src/main/java/com/melco/lms/user/MainActivity.java?raw'

describe('Android WebView bundled page loading', () => {
  it('does not reuse stale H5 assets after an APK update', () => {
    expect(mainActivity).toContain('settings.setCacheMode(WebSettings.LOAD_NO_CACHE);')
    expect(mainActivity).toContain('webView.clearCache(true);')
    expect(mainActivity.indexOf('webView.clearCache(true);')).toBeLessThan(mainActivity.indexOf('webView.loadUrl(ASSET_ORIGIN + "/index.html");'))
  })
})
