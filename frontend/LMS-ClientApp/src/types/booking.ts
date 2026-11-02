export type ServiceId = 'business-sedan' | 'limousine' | 'party' | 'cross-border'

export interface TravelService {
  id: ServiceId
  title: string
  description: string
  image: string
  seats: number
  eta: string
}

export interface BookingDraft {
  serviceId: ServiceId
  pickup: string
  destination: string
  date: string
  time: string
  passengers: number
  vehicleClass?: string
  note: string
}

export interface Booking extends BookingDraft {
  id: string
  status: 'confirmed' | 'completed' | 'cancelled'
  createdAt: string
  // tripReference 订单号，来自服务端行程摘要；本地草稿订单没有订单号。
  reference?: string
}
