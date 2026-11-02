type ShortcutNavigator = Pick<typeof uni, 'navigateTo' | 'reLaunch'>

export type HomeOverview = 'tracking' | 'dispatch' | 'completed'

function openPage(url: string, navigation: ShortcutNavigator): void {
  navigation.navigateTo({
    url,
    fail: () => { navigation.reLaunch({ url }) },
  })
}

export function openHomeOverview(target: HomeOverview, navigation: ShortcutNavigator): void {
  const url = target === 'tracking'
    ? '/pages/tracking/index'
    : target === 'dispatch'
      ? '/pages/dispatch/index'
      : '/pages/bookings/index?filter=COMPLETED'
  openPage(url, navigation)
}

export function openHomeShortcut(target: 'booking' | 'dispatch', navigation: ShortcutNavigator): void {
  openPage(target === 'dispatch' ? '/pages/dispatch/index' : '/pages/booking/index', navigation)
}
