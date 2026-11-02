import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import type { Booking, BookingDraft } from '@/types/booking'

const STORAGE_KEY = 'lmsClientBookings'

const initialBookings: Booking[] = [
  {
    id: 'trip_demo_001',
    serviceId: 'limousine',
    pickup: 'City of Dreams',
    destination: 'Macau International Airport',
    date: '2026-10-13',
    time: '11:00',
    passengers: 1,
    note: '',
    status: 'confirmed',
    createdAt: '2026-10-12T07:55:00Z'
  },
  {
    id: 'trip_demo_002', serviceId: 'party', pickup: 'Studio City', destination: 'Macau Ferry Terminal', date: '2026-10-14', time: '12:00', passengers: 4, note: '', status: 'confirmed', createdAt: '2026-10-10T07:55:00Z'
  },
  {
    id: 'trip_demo_003', serviceId: 'cross-border', pickup: 'City of Dreams', destination: 'Hong Kong International Airport', date: '2026-10-15', time: '14:30', passengers: 2, note: '', status: 'confirmed', createdAt: '2026-10-09T07:55:00Z'
  }
]

export const useBookingStore = defineStore('booking', () => {
  const bookings = ref<Booking[]>([])
  const restored = ref(false)

  const upcoming = computed(() => bookings.value.filter((item) => item.status === 'confirmed'))
  const history = computed(() => bookings.value.filter((item) => item.status !== 'confirmed'))

  function persist() {
    uni.setStorageSync(STORAGE_KEY, bookings.value)
  }

  function restore() {
    if (restored.value) return
    const saved = uni.getStorageSync(STORAGE_KEY)
    bookings.value = Array.isArray(saved) && saved.length ? saved : [...initialBookings]
    restored.value = true
  }

  function createBooking(draft: BookingDraft, bookingId?: string, reference?: string) {
    const booking: Booking = {
      ...draft,
      id: bookingId ?? `BC${Date.now().toString().slice(-11)}`,
      reference,
      status: 'confirmed',
      createdAt: new Date().toISOString()
    }
    bookings.value.unshift(booking)
    persist()
    return booking
  }

  function cancelBooking(id: string) {
    const booking = bookings.value.find((item) => item.id === id)
    if (!booking) return
    booking.status = 'cancelled'
    persist()
  }

  return { bookings, upcoming, history, restore, createBooking, cancelBooking }
})
