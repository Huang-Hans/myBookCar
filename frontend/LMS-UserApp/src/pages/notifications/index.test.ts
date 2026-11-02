import { describe, expect, it } from 'vitest'
import notificationsPage from './index.vue?raw'

describe('notification filters', () => {
  it('uses the shared sliding selector and resets pagination when switching', () => {
    expect(notificationsPage).toContain('<SegmentedChoice :model-value="unreadOnly ? \'UNREAD\' : \'ALL\'"')
    expect(notificationsPage).toContain('value: \'ALL\', label: \'全部\'')
    expect(notificationsPage).toContain('value: \'UNREAD\', label: \'未讀\'')
    expect(notificationsPage).toContain("unreadOnly.value = value === 'UNREAD'; page.value = 1; void load()")
  })

  it('places an accessible circular refresh button to the right', () => {
    expect(notificationsPage).toContain('class="filter-toolbar"')
    expect(notificationsPage).toContain('class="filter-select"')
    expect(notificationsPage).toContain('class="refresh-button" :aria-label="t(\'重新整理\')"')
    expect(notificationsPage).toContain('.refresh-button { display:grid; flex:none; width:42px; height:42px;')
    expect(notificationsPage).toContain('border-radius:50%')
  })
})
