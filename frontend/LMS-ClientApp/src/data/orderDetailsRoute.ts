const ORDER_DETAILS_PAGE = 'pages/order-details/index'

export function orderDetailsRoute(tripId: string): string {
  return `/pages/order-details/index?id=${encodeURIComponent(tripId)}`
}

function pageTripId(page: ReturnType<typeof getCurrentPages>[number] | undefined): string | undefined {
  const candidate = page as { route?: string; options?: { id?: string } } | undefined
  return candidate?.route === ORDER_DETAILS_PAGE ? candidate.options?.id : undefined
}

export function reuseOrderDetailsPage(tripId: string): boolean {
  const pages = getCurrentPages()
  const targetIndex = pages.map(pageTripId).lastIndexOf(tripId)
  if (targetIndex < 0) return false
  const delta = pages.length - 1 - targetIndex
  if (delta > 0) {
    uni.navigateBack({ delta, fail: () => { uni.redirectTo({ url: orderDetailsRoute(tripId) }) } })
  }
  return true
}

function returnToHome(): void {
  uni.reLaunch({ url: '/pages/home/index' })
}

export function returnFromOrderDetails(tripId: string): void {
  const pages = getCurrentPages()
  const previousIndex = pages.slice(0, -1).map((page) => pageTripId(page) === tripId).lastIndexOf(false)
  if (previousIndex < 0) { returnToHome(); return }
  uni.navigateBack({ delta: pages.length - 1 - previousIndex, fail: returnToHome })
}

export function returnFromDriverMap(tripId: string): void {
  if (!tripId) { returnToHome(); return }
  if (reuseOrderDetailsPage(tripId)) return
  uni.redirectTo({ url: orderDetailsRoute(tripId), fail: returnToHome })
}
