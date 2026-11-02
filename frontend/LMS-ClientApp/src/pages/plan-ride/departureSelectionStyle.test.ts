import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import planRideSource from './index.vue?raw'

describe('departure picker selected option', () => {
  it('highlights the selected date, hour, and minute while scrolling', () => {
    const page = parse(planRideSource).descriptor
    const template = page.template?.content ?? ''
    const style = page.styles.map((entry) => entry.content).join('\n')

    for (const index of [0, 1, 2]) {
      expect(template).toContain(`'is-selected': draftDepartureSelection[${index}] === index`)
    }
    expect(style).toMatch(/\.departure-option\.is-selected\{[^}]*color:#[0-9a-f]{6};[^}]*font-weight:700/)
    expect(style).toMatch(/\.departure-selection-lines view\{[^}]*border-top:2px solid/)
  })
})
