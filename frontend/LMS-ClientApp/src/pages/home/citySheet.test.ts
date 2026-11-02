import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import homeSource from './index.vue?raw'

describe('home city sheet', () => {
  it('closes from the separate dimmed area without catching sheet interactions', () => {
    const template = parse(homeSource).descriptor.template?.content ?? ''

    expect(template).toMatch(/class="city-sheet-backdrop"\s*>\s*<button class="city-sheet-dismiss"[^>]*@tap="closeCitySheet"/)
    expect(template).toMatch(/<view class="city-sheet"[^>]*role="dialog"/)
    expect(template).not.toContain('@tap.self="closeCitySheet"')
  })
})
