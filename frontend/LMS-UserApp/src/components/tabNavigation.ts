export type Tab = 'home' | 'bookings' | 'tracking' | 'dispatch' | 'user'

export const tabs: Array<{ key: Tab; label: string; icon: string; activeIcon: string }> = [
  { key: 'home', label: '工作台', icon: '/static/icons/tabs/gongzuotai.svg', activeIcon: '/static/icons/tabs/gongzuotai-active.svg' },
  { key: 'bookings', label: '預約', icon: '/static/icons/tabs/yuyuedan.svg', activeIcon: '/static/icons/tabs/yuyuedan-active.svg' },
  { key: 'tracking', label: '行程', icon: '/static/icons/tabs/hangcheng.svg', activeIcon: '/static/icons/tabs/hangcheng-active.svg' },
  { key: 'dispatch', label: '派車', icon: '/static/icons/tabs/zhipaipaiche.svg', activeIcon: '/static/icons/tabs/zhipaipaiche-active.svg' },
  { key: 'user', label: '用戶', icon: '/static/icons/tabs/yonghu.svg', activeIcon: '/static/icons/tabs/yonghu-active.svg' },
]
