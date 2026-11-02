import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import activity from '../android/app/src/main/java/com/melco/lms/driver/MainActivity.java?raw'
import home from './pages/home/index.vue?raw'

describe('Android safe-area layout', () => {
  it('keeps the WebView inside system bars, cutouts and the keyboard', () => {
    expect(activity).toContain('setDecorFitsSystemWindows(false)')
    expect(activity).toContain('WindowInsets.Type.systemBars()')
    expect(activity).toContain('WindowInsets.Type.displayCutout()')
    expect(activity).toContain('WindowInsets.Type.ime()')
    expect(activity).toContain('view.setPadding(safeInsets.left, safeInsets.top,')
    expect(activity).toContain('.setInsets(handledTypes, Insets.NONE)')
  })

  it('reserves only the actual navigation and safe-area height', () => {
    const styles = parse(home).descriptor.styles[0]?.content ?? ''
    expect(styles).toContain('.bottom-nav{bottom:0;height:calc(56px + env(safe-area-inset-bottom));padding:0 4px env(safe-area-inset-bottom);box-sizing:border-box}')
    expect(styles).toContain('.content{height:calc(100vh - 134px - env(safe-area-inset-bottom))}')
  })
})
