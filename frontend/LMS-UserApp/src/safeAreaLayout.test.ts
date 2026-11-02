import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import activity from '../android/app/src/main/java/com/melco/lms/user/MainActivity.java?raw'
import shell from './components/AppShell.vue?raw'

describe('Android safe-area layout', () => {
  it('keeps the WebView inside system bars, cutouts and the keyboard', () => {
    expect(activity).toContain('setDecorFitsSystemWindows(false)')
    expect(activity).toContain('WindowInsets.Type.systemBars()')
    expect(activity).toContain('WindowInsets.Type.displayCutout()')
    expect(activity).toContain('WindowInsets.Type.ime()')
    expect(activity).toContain('view.setPadding(safeInsets.left, safeInsets.top,')
    expect(activity).toContain('.setInsets(handledTypes, Insets.NONE)')
  })

  it('keeps navigation inside its declared height without doubled safe-area padding', () => {
    const styles = parse(shell).descriptor.styles[0]?.content ?? ''
    expect(styles).toContain('padding-bottom: calc(66px + env(safe-area-inset-bottom))')
    expect(styles).toContain('box-sizing: border-box; height: calc(66px + env(safe-area-inset-bottom))')
    expect(styles).not.toContain('padding-bottom: calc(84px')
  })
})
