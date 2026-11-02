/** Display a local OS notification for a message already received by the app. */
export function showSystemNotification(title: string, content: string, payload: unknown): void {
  const message = content.trim()
  if (!message) return

  // #ifdef APP
  try {
    if (typeof uni === 'undefined' || typeof uni.createPushMessage !== 'function') return
    uni.createPushMessage({
      title,
      content: message,
      payload,
    })
  } catch (error) {
    console.error(JSON.stringify({ event: 'systemNotificationFailed', message: error instanceof Error ? error.message : 'Unknown notification error' }))
  }
  // #endif
}

/** Surface foreground remote-push deliveries as an OS notification too. */
export function registerSystemPushListener(title: string): () => void {
  // #ifdef APP
  if (typeof uni === 'undefined' || typeof uni.onPushMessage !== 'function') return () => {}
  const listener = (result: { type: 'click' | 'receive'; data: { content: string; title: string; payload: unknown } }): void => {
    if (result.type !== 'receive') return
    showSystemNotification(result.data.title || title, result.data.content, result.data.payload)
  }
  uni.onPushMessage(listener)
  return () => uni.offPushMessage(listener)
  // #endif
  return () => {}
}
