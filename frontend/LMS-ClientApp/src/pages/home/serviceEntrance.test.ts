import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import homeSource from './index.vue?raw'

describe('home service entrance', () => {
  it('staggers every service card from below while fading in', () => {
    const descriptor = parse(homeSource).descriptor
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')

    expect(template).toContain('v-for="(service, index) in services"')
    expect(template).toContain('animationDelay: `${index * 180}ms`')
    expect(styles).toMatch(/\.service-card\s*\{\s*animation:service-card-enter/)
    expect(styles).toMatch(/@keyframes service-card-enter\s*\{\s*from\s*\{\s*opacity:0;\s*transform:translateY\(28px\)/)
    expect(styles).toContain('@media (prefers-reduced-motion:reduce)')
    const reducedMotionStyles = styles.slice(styles.indexOf('@media (prefers-reduced-motion:reduce)'))
    expect(reducedMotionStyles).toMatch(/\.service-card\s*\{\s*animation:none;\s*opacity:1;\s*transform:none/)
  })
})
