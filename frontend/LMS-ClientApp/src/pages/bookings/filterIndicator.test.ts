import { parse } from '@vue/compiler-sfc'
import { describe, expect, it } from 'vitest'
import bookingsSource from './index.vue?raw'

describe('booking filter indicator', () => {
  it('moves one selection indicator between the three accessible filter buttons', () => {
    const descriptor = parse(bookingsSource).descriptor
    const script = descriptor.scriptSetup?.content ?? ''
    const template = descriptor.template?.content ?? ''
    const styles = descriptor.styles.map((style) => style.content).join('\n')

    expect(script).toContain("['In Progress', 'Completed', 'Cancelled']")
    expect(script).toContain('filterOptions.indexOf(filter.value)')
    expect(template).toContain('class="filter-indicator"')
    expect(template).toContain('translateX(${activeFilterIndex * 100}%)')
    expect(template).toContain(':aria-pressed="filter === item"')
    expect(styles).toMatch(/\.filter-indicator\s*\{[^}]*transition:transform/)
    expect(styles).toContain('@media(prefers-reduced-motion:reduce)')
    const reducedMotionStyles = styles.slice(styles.indexOf('@media(prefers-reduced-motion:reduce)'))
    expect(reducedMotionStyles).toMatch(/\.filter-indicator\s*\{\s*transition:none/)
  })
})
