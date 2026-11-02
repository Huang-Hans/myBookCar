import { describe, expect, it } from 'vitest'
import homePage from './index.vue?raw'

describe('vehicle inspection controls', () => {
  it('keeps Chinese status labels on one line with wider action buttons', () => {
    expect(homePage).toContain('flex:0 0 64px;width:64px;min-width:64px')
    expect(homePage).toContain('white-space:nowrap')
    expect(homePage).toContain('max-width:calc(100% - 152px)')
  })

  it('closes the issue sheet from the gray backdrop without a cancel button', () => {
    expect(homePage).toContain('class="sheet-backdrop issue-sheet-backdrop" @tap="closeVehicleIssue"')
    expect(homePage).toContain('<view class="issue-sheet" @tap.stop role="dialog"')
    expect(homePage).toContain('.issue-sheet-backdrop{inset:0}')
    expect(homePage).toContain('function closeVehicleIssue(): void { isVehicleIssueOpen.value = false;')
    expect(homePage).toContain('<view class="issue-sheet-heading"><text>{{ $t("Report an Issue") }}</text></view>')
    expect(homePage).not.toContain('<button @tap="closeVehicleIssue">')
  })
})
