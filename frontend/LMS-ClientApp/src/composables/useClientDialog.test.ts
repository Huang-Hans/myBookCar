import { describe, expect, it, vi } from 'vitest'
import { useClientDialog } from './useClientDialog'

describe('client bottom sheet dialog', () => {
  it('does not invoke a destructive action until confirmed', async () => {
    const action = vi.fn()
    const sheet = useClientDialog()
    sheet.confirm('Cancel this booking?', 'Subject to policy.', 'Cancel Booking', 'Keep Booking', action)
    expect(action).not.toHaveBeenCalled()
    sheet.close()
    expect(action).not.toHaveBeenCalled()

    sheet.confirm('Cancel this booking?', 'Subject to policy.', 'Cancel Booking', 'Keep Booking', action)
    await sheet.submit()
    expect(action).toHaveBeenCalledOnce()
    expect(sheet.dialog.value).toBeNull()
  })

  it('shows an alert without a secondary action', async () => {
    const sheet = useClientDialog()
    sheet.alert('Request Failed', 'Trace ID: tr_test', 'OK')
    expect(sheet.dialog.value?.secondaryLabel).toBeUndefined()
    await sheet.submit()
    expect(sheet.dialog.value).toBeNull()
  })
})
