import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ShiftName = 'Morning' | 'Afternoon' | 'Night'
export type DutyStatus = 'On duty' | 'Resting' | 'On leave'
export type FleetDriverStatus = 'On duty' | 'Available' | 'On leave' | 'Suspended'
export type FleetVehicleStatus = 'In service' | 'On a trip' | 'In workshop' | 'Out of service'

export interface FleetDriver {
  staffId: string
  name: string
  phoneLastFour: string
  property: string
  license: string
  licenseExpiry: string
  classes: string[]
  languages: string
  status: FleetDriverStatus
}

export interface FleetVehicle {
  plate: string
  model: string
  vehicleClass: string
  property: string
  seats: number
  luggage: number
  mileage: string
  permit: string
  lastService: string
  status: FleetVehicleStatus
}

export interface FleetShift {
  id: string
  date: string
  staffId: string
  property: string
  shift: ShiftName
  hours: string
  plate: string
  status: DutyStatus
}

export function shiftHours(shift: ShiftName): string {
  if (shift === 'Morning') return '07:00–15:00'
  if (shift === 'Afternoon') return '15:00–23:00'
  return '23:00–07:00'
}

export function macauDayKey(offset = 0): string {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Macau', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const read = (type: string) => Number(parts.find((part) => part.type === type)?.value ?? 0)
  const date = new Date(Date.UTC(read('year'), read('month') - 1, read('day') + offset))
  return date.toISOString().slice(0, 10)
}

const today = macauDayKey(0)
const tomorrow = macauDayKey(1)

export const useFleetStore = defineStore('fleet', () => {
  const drivers = ref<FleetDriver[]>([
    { staffId: 'DRV-014', name: 'Daniel Ho', phoneLastFour: '6621', property: 'City of Dreams', license: 'M-882041', licenseExpiry: '2028-04-12', classes: ['Luxury MPV'], languages: 'Cantonese, English', status: 'On duty' },
    { staffId: 'DRV-021', name: 'Eric Chan', phoneLastFour: '1904', property: 'Studio City', license: 'M-771230', licenseExpiry: '2027-09-01', classes: ['Luxury MPV', 'Luxury Sedan'], languages: 'Cantonese, Mandarin', status: 'Available' },
    { staffId: 'DRV-008', name: 'Mei Cheong', phoneLastFour: '4480', property: 'Altira', license: 'M-650118', licenseExpiry: '2028-01-20', classes: ['Maybach', 'Luxury Sedan'], languages: 'Cantonese, English', status: 'Available' },
    { staffId: 'DRV-033', name: 'Ken Lei', phoneLastFour: '7752', property: 'Morpheus', license: 'M-910442', licenseExpiry: '2026-11-30', classes: ['Luxury Sedan'], languages: 'Cantonese', status: 'On leave' },
    { staffId: 'DRV-019', name: 'Alex Wong', phoneLastFour: '2208', property: 'Nuwa', license: 'M-540773', licenseExpiry: '2027-06-18', classes: ['Luxury Sedan'], languages: 'Cantonese, English', status: 'Suspended' },
  ])
  const vehicles = ref<FleetVehicle[]>([
    { plate: 'MR-28-66', model: 'Toyota Alphard', vehicleClass: 'Luxury MPV', property: 'City of Dreams', seats: 6, luggage: 4, mileage: '42,180 km', permit: '2027-03-12', lastService: '2026-09-18', status: 'In service' },
    { plate: 'MR-16-82', model: 'Mercedes V-Class', vehicleClass: 'Luxury MPV', property: 'Studio City', seats: 6, luggage: 5, mileage: '28,440 km', permit: '2026-12-01', lastService: '2026-08-02', status: 'In service' },
    { plate: 'MU-09-21', model: 'Mercedes S-Class', vehicleClass: 'Luxury Sedan', property: 'Morpheus', seats: 3, luggage: 2, mileage: '19,760 km', permit: '2027-01-20', lastService: '2026-10-08', status: 'In workshop' },
    { plate: 'MA-02-08', model: 'Mercedes-Maybach', vehicleClass: 'Maybach', property: 'Altira', seats: 3, luggage: 2, mileage: '11,205 km', permit: '2027-06-30', lastService: '2026-07-14', status: 'In service' },
    { plate: 'MW-04-15', model: 'BMW 7 Series', vehicleClass: 'Luxury Sedan', property: 'Nuwa', seats: 3, luggage: 2, mileage: '63,900 km', permit: '2026-04-30', lastService: '2025-12-11', status: 'Out of service' },
  ])
  const shifts = ref<FleetShift[]>([
    { id: '1', date: today, staffId: 'DRV-014', property: 'City of Dreams', shift: 'Morning', hours: shiftHours('Morning'), plate: 'MR-28-66', status: 'On duty' },
    { id: '2', date: today, staffId: 'DRV-021', property: 'Studio City', shift: 'Morning', hours: shiftHours('Morning'), plate: '', status: 'On duty' },
    { id: '3', date: today, staffId: 'DRV-008', property: 'Altira', shift: 'Afternoon', hours: shiftHours('Afternoon'), plate: '', status: 'On duty' },
    { id: '4', date: today, staffId: 'DRV-033', property: 'Morpheus', shift: 'Afternoon', hours: shiftHours('Afternoon'), plate: '', status: 'On leave' },
    { id: '5', date: tomorrow, staffId: 'DRV-014', property: 'City of Dreams', shift: 'Afternoon', hours: shiftHours('Afternoon'), plate: '', status: 'On duty' },
  ])

  function addDriver(driver: FleetDriver): void {
    drivers.value.unshift(driver)
  }

  function addVehicle(vehicle: FleetVehicle): void {
    vehicles.value.unshift(vehicle)
  }

  function schedule(input: { date: string; staffId: string; property: string; shift: ShiftName; status: DutyStatus }): string | null {
    const driver = drivers.value.find((item) => item.staffId === input.staffId)
    if (!driver) return 'Select a driver.'
    if (driver.status === 'Suspended') return 'Suspended drivers cannot be scheduled.'
    if (input.status === 'On duty' && driver.status === 'On leave') return 'A driver on leave cannot be placed on duty.'
    if (shifts.value.some((item) => item.date === input.date && item.staffId === input.staffId)) return 'This driver already has a shift on that day.'
    shifts.value.push({
      id: `${Date.now()}`,
      date: input.date,
      staffId: input.staffId,
      property: input.property,
      shift: input.shift,
      hours: shiftHours(input.shift),
      plate: '',
      status: input.status,
    })
    return null
  }

  function vehiclesFor(shift: FleetShift): FleetVehicle[] {
    const driver = drivers.value.find((item) => item.staffId === shift.staffId)
    if (!driver || shift.status !== 'On duty') return []
    const taken = new Set(shifts.value.filter((item) => item.date === shift.date && item.id !== shift.id && item.plate).map((item) => item.plate))
    return vehicles.value.filter((vehicle) => vehicle.status === 'In service' && vehicle.property === shift.property && driver.classes.includes(vehicle.vehicleClass) && !taken.has(vehicle.plate))
  }

  function assignVehicle(shiftId: string, plate: string): string | null {
    const shift = shifts.value.find((item) => item.id === shiftId)
    if (!shift) return 'Select a shift.'
    if (shift.status !== 'On duty') return 'Only an on-duty shift can take a vehicle.'
    const vehicle = vehicles.value.find((item) => item.plate === plate)
    if (!vehicle || !vehiclesFor(shift).some((item) => item.plate === plate)) return 'Choose a vehicle that is in service at the same property and matches the driver.'
    shift.plate = plate
    return null
  }

  function clearVehicle(shiftId: string): void {
    const shift = shifts.value.find((item) => item.id === shiftId)
    if (shift) shift.plate = ''
  }

  function shiftOn(staffId: string, date: string): FleetShift | undefined {
    return shifts.value.find((item) => item.staffId === staffId && item.date === date)
  }

  function driverOn(plate: string, date: string): FleetDriver | undefined {
    const shift = shifts.value.find((item) => item.plate === plate && item.date === date)
    return shift ? drivers.value.find((item) => item.staffId === shift.staffId) : undefined
  }

  return { drivers, vehicles, shifts, addDriver, addVehicle, schedule, vehiclesFor, assignVehicle, clearVehicle, shiftOn, driverOn }
})
