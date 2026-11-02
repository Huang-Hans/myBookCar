import { describe, expect, it } from 'vitest'
import { tabs } from './tabNavigation'

describe('bottom navigation icons', () => {
  it('maps each tab to the supplied icon and its active variant', () => {
    expect(tabs.map(({ key, icon, activeIcon }) => [key, icon.split('/').pop(), activeIcon.split('/').pop()])).toEqual([
      ['home', 'gongzuotai.svg', 'gongzuotai-active.svg'],
      ['bookings', 'yuyuedan.svg', 'yuyuedan-active.svg'],
      ['tracking', 'hangcheng.svg', 'hangcheng-active.svg'],
      ['dispatch', 'zhipaipaiche.svg', 'zhipaipaiche-active.svg'],
      ['user', 'yonghu.svg', 'yonghu-active.svg'],
    ])
  })
})
