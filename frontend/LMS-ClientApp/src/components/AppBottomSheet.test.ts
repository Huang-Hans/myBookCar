import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import sheetSource from './AppBottomSheet.vue?raw'
import accountSource from '../pages/account/index.vue?raw'
import planRideSource from '../pages/plan-ride/index.vue?raw'
import registerSource from '../pages/register/index.vue?raw'

describe('client Figma-style sheets', () => {
  it('uses a bottom sheet with a separate backdrop, option selection and explicit confirmation', () => {
    const template = parse(sheetSource).descriptor.template?.content ?? ''
    expect(template).toContain('class="sheet-dismiss"')
    expect(template).toContain('role="dialog"')
    expect(template).toContain("emit('select', option)")
    expect(template).toContain("emit('confirm')")
  })

  it('replaces native selection popups on account, registration and trip planning screens', () => {
    for (const source of [accountSource, registerSource, planRideSource]) {
      const template = parse(source).descriptor.template?.content ?? ''
      expect(template).not.toMatch(/<picker\s/)
      expect(template).toContain('<AppBottomSheet')
    }
  })
})
