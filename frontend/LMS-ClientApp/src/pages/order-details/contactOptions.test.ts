import { describe, expect, it, vi } from 'vitest'
import { isDialablePhoneNumber, mockDriverPhoneNumber, operationsPhoneNumber, resolveContactPhoneNumber, supportPhoneNumber } from './contactOptions'
import detailsPage from './index.vue?raw'
import customerApiSource from '../../api/customer.ts?raw'
import nativeActivity from '../../../android/app/src/main/java/com/melco/lms/client/MainActivity.java?raw'

describe('order contact options', () => {
  it('dials the configured support and operations numbers without loading driver data', async () => {
    const getDriverContact = vi.fn()
    expect(await resolveContactPhoneNumber('Contact Support', 'trip_1', getDriverContact)).toBe(supportPhoneNumber)
    expect(await resolveContactPhoneNumber('Chat with Operations', 'trip_1', getDriverContact)).toBe(operationsPhoneNumber)
    expect(supportPhoneNumber).toBe('+8613700000000')
    expect(operationsPhoneNumber).toBe('+8613900000000')
    expect(getDriverContact).not.toHaveBeenCalled()
  })

  it('loads the assigned driver number from the owned trip contact endpoint', async () => {
    const getDriverContact = vi.fn().mockResolvedValue({ data: { phoneNumber: '+85366182400' } })
    expect(await resolveContactPhoneNumber('Call Driver', 'trip_1', getDriverContact, false)).toBe('+85366182400')
    expect(getDriverContact).toHaveBeenCalledExactlyOnceWith('trip_1')
    expect(customerApiSource).toContain('/api/booking/customer/v1/me/trips/${encodeURIComponent(tripId)}/driver-contacts')
  })

  it('uses a demo driver number only when the preview flag is enabled', async () => {
    const getDriverContact = vi.fn()
    expect(await resolveContactPhoneNumber('Call Driver', 'trip_1', getDriverContact, true)).toBe(mockDriverPhoneNumber)
    expect(mockDriverPhoneNumber).toBe('+12025550100')
    expect(getDriverContact).not.toHaveBeenCalled()
  })

  it('rejects missing, unsafe, or unavailable driver contacts without dialing', async () => {
    expect(isDialablePhoneNumber('tel:+85366182400')).toBe(false)
    expect(isDialablePhoneNumber('+85366182400')).toBe(true)
    await expect(resolveContactPhoneNumber('Call Driver', 'trip_1', vi.fn().mockResolvedValue({ data: { phoneNumber: '' } }), false)).rejects.toThrow('Contact phone number is unavailable')
    await expect(resolveContactPhoneNumber('Call Driver', 'trip_1', vi.fn().mockRejectedValue(new Error('Not found')), false)).rejects.toThrow('Not found')
  })

  it('uses a click handler and the platform phone dialer for all three options', () => {
    expect(detailsPage).toContain('class="action" @click="callContact(option)"')
    expect(detailsPage).toContain('uni.makePhoneCall({')
    expect(detailsPage).toContain('!isMockDriverPhoneEnabled && (!bookingId.value || !trip.value?.driver?.canContact)')
    expect(detailsPage).not.toContain('@tap="unavailable(option)"')
    expect(nativeActivity).toContain('shouldOverrideUrlLoading(WebView view, WebResourceRequest request)')
    expect(nativeActivity).toContain('new Intent(Intent.ACTION_DIAL, uri)')
    expect(nativeActivity).not.toContain('Intent.ACTION_CALL')
  })
})
