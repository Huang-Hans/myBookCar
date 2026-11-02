import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import serviceInfoSource from './index.vue?raw'

describe('service category vehicle entrance', () => {
  it('slides the shared vehicle image in from the right and fades it in', () => {
    const descriptor = parse(serviceInfoSource).descriptor
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')

    expect(template).toContain('<image class="vehicle" :src="service.image"')
    expect(styles).toMatch(/\.vehicle\s*\{[^}]*animation:\s*vehicle-enter/)
    expect(styles).toMatch(/@keyframes vehicle-enter\s*\{\s*from\s*\{\s*opacity:\s*0;\s*transform:\s*translateX\(120px\)/)
    expect(styles).toMatch(/to\s*\{\s*opacity:\s*1;\s*transform:\s*translateX\(0\)/)
    expect(styles).toMatch(/@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{\s*\.vehicle\s*\{\s*animation:\s*none;\s*opacity:\s*1;\s*transform:\s*none/)
  })
})
