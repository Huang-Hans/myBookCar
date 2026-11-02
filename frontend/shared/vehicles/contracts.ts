export interface VehicleOption {
  vehicleId: string
  plate: string
  model: string
  propertyId: string
  driverId?: string | null
}

export interface VehicleOptionsPage {
  data: VehicleOption[]
  meta?: { pagination?: { totalPages: number } | null } | null
}
