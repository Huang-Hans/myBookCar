export interface DriverOrder {
  id: string
  displayId: string
  service: string
  status: 'Completed' | 'Cancelled'
  date: string
  pickup: string
  pickupAddress: string
  dropoff: string
  dropoffAddress: string
  accepted: string
  started: string
  ended: string
  customerInitials: string
  customerName: string
  customerPhone: string
  passengers: number
  specialInstructions: string
  vehicleRequirements: string
  customerServiceNote: string
}
