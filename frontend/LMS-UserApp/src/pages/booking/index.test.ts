import { describe, expect, it } from 'vitest'
import bookingPage from './index.vue?raw'
import segmentedChoice from '../../components/SegmentedChoice.vue?raw'
import departurePicker from '../../components/BookingDeparturePicker.vue?raw'
import optionSheet from '../../components/BookingOptionSheet.vue?raw'

describe('booking option controls', () => {
  it('previews first and opens a confirmation dialog before creating the booking', () => {
    expect(bookingPage).toContain('if (preview.value.canSubmit) isConfirmationOpen.value = true')
    expect(bookingPage).toContain('if (busy.value || !isConfirmationOpen.value || !preview.value?.canSubmit) return')
    expect(bookingPage).toContain('role="dialog" aria-modal="true"')
    expect(bookingPage).toContain('@tap="submit"')
    expect(bookingPage).toContain('@tap="isConfirmationOpen = false"')
  })

  it('does not display a luggage field and submits the API default of zero luggage', () => {
    expect(bookingPage).not.toContain("t('行李')")
    expect(bookingPage).not.toContain('v-model="luggage"')
    expect(bookingPage).toContain('passengers: passengerCount, luggage: 0')
  })

  it('loads the vehicle service products from the backend and selects the first bookable option', () => {
    expect(bookingPage).not.toContain("t('服務類型')")
    expect(bookingPage).toContain('staffApi.bookingOptions()')
    expect(bookingPage).toContain('bookingOptions.value.products.find((item) => item.isBookable)')
    expect(bookingPage).toContain('<SegmentedChoice v-if="vehicleOptions.length" v-model="selectedProductCode"')
    expect(bookingPage).toContain('productCode: selectedProduct.value.productCode')
    expect(bookingPage).toContain('serviceType: selectedProduct.value.serviceType')
    expect(bookingPage).not.toContain("code: 'LUXURY_SEDAN'")
    expect(bookingPage).not.toContain("code: 'LUXURY_MPV'")
    expect(bookingPage).not.toContain("code: 'MAYBACH'")
    expect(bookingPage).not.toContain('class="chips"')
  })

  it('slides one selected indicator with an accessible reduced-motion fallback', () => {
    expect(segmentedChoice).toContain('gap:0')
    expect(segmentedChoice).toContain('transform: `translateX(${selectedIndex.value * 100}%)`')
    expect(segmentedChoice).toContain('transition:transform 240ms')
    expect(segmentedChoice).toContain(':aria-pressed="modelValue === option.value"')
    expect(segmentedChoice).toContain(':disabled="option.disabled"')
    expect(segmentedChoice).toContain('@media (prefers-reduced-motion:reduce)')
  })

  it('loads catalogs independently and shows field-level retry instead of empty pickers', () => {
    expect(bookingPage).toContain('Promise.allSettled([')
    expect(bookingPage).toContain('loadCatalogOptions(staffApi.properties)')
    expect(bookingPage).toContain('loadCatalogOptions(staffApi.locations)')
    expect(bookingPage).toContain('v-if="propertyError" class="option-error"')
    expect(bookingPage).toContain('v-if="locationError" class="option-error"')
    expect(bookingPage).toContain(':disabled="optionsLoading || !locations.length"')
  })

  it('combines date and time into a future-only mobile wheel', () => {
    expect(bookingPage).toContain('<BookingDeparturePicker :open="isDeparturePickerOpen"')
    expect(bookingPage).toContain('isFutureMacauDeparture(date.value, time.value, new Date())')
    expect(bookingPage).not.toContain('<picker mode="date"')
    expect(bookingPage).not.toContain('<picker mode="time"')
    expect(departurePicker).toContain('<picker-view class="departure-wheel"')
    expect(departurePicker).toContain('<picker-view-column class="date-column">')
    expect(departurePicker).toContain('class="selected-band"')
    expect(departurePicker).toContain("emit('confirm', { date, time })")
  })

  it('opens a searchable mobile sheet for property, pickup and destination', () => {
    expect(bookingPage).toContain("@tap=\"openOptionSheet('property')\"")
    expect(bookingPage).toContain("@tap=\"openOptionSheet('pickup')\"")
    expect(bookingPage).toContain("@tap=\"openOptionSheet('dropoff')\"")
    expect(bookingPage).toContain('<BookingOptionSheet :open="optionSheetKind !== null"')
    expect(bookingPage).not.toContain('<picker :range=')
    expect(optionSheet).toContain('role="dialog" aria-modal="true"')
    expect(optionSheet).toContain('v-if="options.length > 6"')
    expect(optionSheet).toContain('<scroll-view class="option-list" scroll-y>')
    expect(optionSheet).toContain(':disabled="!canConfirm"')
  })
})
