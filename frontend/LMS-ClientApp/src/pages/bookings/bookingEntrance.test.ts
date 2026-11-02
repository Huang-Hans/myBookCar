import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import bookingsSource from './index.vue?raw'

describe('booking list entrance', () => {
  it('reveals every card in order from top to bottom', () => {
    const descriptor = parse(bookingsSource).descriptor
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')

    expect(template).toContain('v-for="(booking, index) in bookings"')
    expect(template).toContain('animationDelay: `${index * 100}ms`')
    expect(styles).toMatch(/\.booking-card\s*\{\s*animation:booking-card-enter/)
    expect(styles).toMatch(/@keyframes booking-card-enter\s*\{\s*from\s*\{\s*opacity:0;\s*transform:translateY\(-24px\)/)
    expect(styles).toMatch(/to\s*\{\s*opacity:1;\s*transform:translateY\(0\)/)
    expect(styles).toMatch(/@media\s*\(prefers-reduced-motion:reduce\)\s*\{\s*\.booking-card\s*\{\s*animation:none/)
  })
})
