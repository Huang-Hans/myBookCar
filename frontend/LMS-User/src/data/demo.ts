export type DriverAvailability = 'Available' | 'On Trip' | 'Break' | 'Off Duty'
export type DriverStatus = 'Active' | 'Inactive' | 'Suspended'
export interface Driver {
  driverId: string
  version?: number
  licenseExpiryDate?: string
  fullName: string
  licenseNumber: string
  phoneMasked: string
  propertyName: string
  vehicleId?: string | null
  vehicleName: string
  availability: DriverAvailability
  status: DriverStatus
  emergencyContact: string
  emergencyContactPhone?: string
  languagesSpoken: string
  assignedProperties: string[]
  blacklistReason: string
  blacklistEffectiveDate: string
  blacklistExpiryDate: string
}

export type BookingStatus = 'Pending' | 'Confirmed' | 'Driver Assigned' | 'Driver En Route' | 'Arrived at Pickup' | 'In Progress' | 'Completed' | 'Cancelled'
export interface Booking {
  productCode?: 'BUSINESS_SEDAN' | 'LIMOUSINE' | 'PARTY' | 'CROSS_BORDER_TRANSFER'
  channel?: 'CONSOLE' | 'SELF_SERVICE'
  bookingId: string
  tripReference?: string
  version?: number
  pickupLocationId?: string
  dropoffLocationId?: string
  guestId: string
  guestName: string
  roomNumber: string
  pickupLocation: string
  dropoffLocation: string
  pickupAt: string
  propertyName: string
  vehicleClass: string
  status: BookingStatus
  // 要显示的状态文案（行程中细分为乘客上车 / 行驶中 / 到达目的地）；没有时显示 status。
  stageLabel?: string
  points: number
  serviceType?: 'Complimentary' | 'Property transfer' | 'Cross-border'
  createdByStaffId?: string
  createdByStaffName?: string
  cancellationReason?: string
  // 已指派司机的姓名与车辆文本（指派时的快照）；未指派时为空。
  driverName?: string
  vehicleLabel?: string
}
export interface Guest {
  guestId: string
  memberId?: string
  propertyId?: string
  phoneMasked?: string
  fullName: string
  roomNumber: string
  tier: 'Silver' | 'Gold' | 'Platinum' | 'Diamond' | 'VIP'
  isInHouse: boolean
  phoneLastFour: string
  preferences: string[]
  isBlacklisted: boolean
}
export interface Location {
  locationId: string
  name: string
  category: 'Airport' | 'Ferry' | 'Border' | 'Properties'
  address: string
  isFavorite: boolean
}
export interface Approval {
  approvalId: string
  guestId?: string
  version?: number
  guestName: string
  roomNumber: string
  tier: string
  reason: string
  status: 'Pending Approval' | 'Approved' | 'Rejected'
  requestedAt: string
  requestedByStaffId?: string
}
export interface Notice {
  noticeId: string
  category: string
  title: string
  message: string
  createdAt: string
  isRead: boolean
}

export const propertyNames = ['City of Dreams', 'Studio City', 'Altira', 'Grand Hyatt'] as const
const driverNames = ['Driver A', 'Driver B', 'Driver C', 'Driver D', 'Driver E', 'Driver F', 'Driver G', 'Driver H', 'Driver I', 'Driver J', 'Driver K', 'Driver L']
const vehicles = ['Mercedes S-Class', 'Toyota Alphard', 'BMW 7 Series']
const availability: DriverAvailability[] = ['Available', 'On Trip', 'Available', 'Break', 'Off Duty', 'Available', 'On Trip', 'Available', 'Break', 'Off Duty', 'Available', 'Available']
export const demoDrivers: Driver[] = driverNames.map((fullName, index) => ({
  driverId: `DR-${1021 + index}`,
  fullName,
  licenseNumber: `MAC-${28415 + index}`,
  phoneMasked: '+853 •••• ••••',
  propertyName: propertyNames[index % propertyNames.length] ?? propertyNames[0],
  vehicleName: `${vehicles[index % vehicles.length] ?? vehicles[0]} · MP-${28 + index}-61`,
  availability: availability[index] ?? 'Available',
  status: index === 10 ? 'Inactive' : 'Active',
  emergencyContact: '',
  languagesSpoken: 'English, Chinese',
  assignedProperties: [propertyNames[index % propertyNames.length] ?? propertyNames[0]],
  blacklistReason: '',
  blacklistEffectiveDate: '',
  blacklistExpiryDate: '',
}))

export const demoGuests: Guest[] = [
  { guestId: 'g_001', fullName: 'Guest A', roomNumber: '1808', tier: 'Platinum', isInHouse: true, phoneLastFour: '1001', preferences: ['Quiet ride'], isBlacklisted: false },
  { guestId: 'g_002', fullName: 'Guest B', roomNumber: '1206', tier: 'Gold', isInHouse: true, phoneLastFour: '1002', preferences: ['Airport pickup'], isBlacklisted: false },
  { guestId: 'g_003', fullName: 'Guest C', roomNumber: '2201', tier: 'Diamond', isInHouse: true, phoneLastFour: '1003', preferences: [], isBlacklisted: false },
  { guestId: 'g_004', fullName: 'Guest D', roomNumber: '1612', tier: 'VIP', isInHouse: true, phoneLastFour: '1004', preferences: ['Extra luggage'], isBlacklisted: false },
  { guestId: 'g_005', fullName: 'Guest E', roomNumber: '1902', tier: 'Diamond', isInHouse: true, phoneLastFour: '1005', preferences: [], isBlacklisted: true },
]

export const demoLocations: Location[] = [
  { locationId: 'loc_001', name: 'Macau International Airport', category: 'Airport', address: 'Taipa · Departures terminal', isFavorite: true },
  { locationId: 'loc_002', name: 'Taipa Ferry Terminal', category: 'Ferry', address: 'Taipa · Main entrance', isFavorite: false },
  { locationId: 'loc_003', name: 'Border Gate', category: 'Border', address: 'Macau Peninsula · Passenger crossing', isFavorite: false },
  { locationId: 'loc_004', name: 'City of Dreams', category: 'Properties', address: 'Cotai · Nuwa lobby', isFavorite: false },
  { locationId: 'loc_005', name: 'Studio City', category: 'Properties', address: 'Cotai · Main lobby', isFavorite: false },
  { locationId: 'loc_006', name: 'Outer Harbour Ferry Terminal', category: 'Ferry', address: 'Macau Peninsula · Arrivals entrance', isFavorite: false },
]

export const demoBookings: Booking[] = [
  { bookingId: 'TR-20261008-042', guestId: 'g_001', guestName: 'Guest A', roomNumber: '1808', pickupLocation: 'Macau International Airport', dropoffLocation: 'City of Dreams — Main Lobby', pickupAt: '2026-10-08T06:30:00Z', propertyName: 'Nuwa', vehicleClass: 'Luxury Sedan', serviceType: 'Property transfer', status: 'Driver En Route', points: 3000 },
  { bookingId: 'TR-20261008-041', guestId: 'g_002', guestName: 'Guest B', roomNumber: '1206', pickupLocation: 'Taipa Ferry Terminal', dropoffLocation: 'City of Dreams — Main Lobby', pickupAt: '2026-10-08T06:15:00Z', propertyName: 'Nuwa', vehicleClass: 'Luxury MPV', serviceType: 'Complimentary', status: 'Confirmed', points: 4500 },
  { bookingId: 'TR-20261008-040', guestId: 'g_003', guestName: 'Guest C', roomNumber: '2201', pickupLocation: 'City of Dreams', dropoffLocation: 'City of Dreams — Main Lobby', pickupAt: '2026-10-08T05:45:00Z', propertyName: 'Morpheus', vehicleClass: 'Luxury MPV', serviceType: 'Property transfer', status: 'Completed', points: 4500 },
  { bookingId: 'TR-20261008-039', guestId: 'g_004', guestName: 'Guest D', roomNumber: '1612', pickupLocation: 'Border Gate', dropoffLocation: 'City of Dreams — Main Lobby', pickupAt: '2026-10-08T05:20:00Z', propertyName: 'Nuwa', vehicleClass: 'Maybach', serviceType: 'Cross-border', status: 'Completed', points: 8000 },
  { bookingId: 'TR-20261008-038', guestId: 'g_003', guestName: 'Guest C', roomNumber: '2201', pickupLocation: 'Studio City', dropoffLocation: 'City of Dreams — Main Lobby', pickupAt: '2026-10-08T07:10:00Z', propertyName: 'Nuwa', vehicleClass: 'Luxury Sedan', serviceType: 'Property transfer', status: 'Driver Assigned', points: 3000 },
]

export const demoApprovals: Approval[] = [
  { approvalId: 'AP-20261008-001', guestName: 'Guest A', roomNumber: '1808', tier: 'Platinum', reason: 'Below Diamond tier · Guest requests a review of transfer entitlement.', status: 'Pending Approval', requestedAt: '2026-10-08T06:20:00Z' },
]

export const demoNotices: Notice[] = [
  { noticeId: 'n_001', category: 'System alerts', title: 'Console ready', message: 'Live backend and push delivery are not connected. Session events are shown here.', createdAt: '2026-10-08T06:00:00Z', isRead: false },
  { noticeId: 'n_002', category: 'Approval required', title: 'Approval request pending', message: 'A guest entitlement review needs attention.', createdAt: '2026-10-08T06:20:00Z', isRead: false },
  { noticeId: 'n_003', category: 'Driver assigned', title: 'Driver assigned', message: 'A driver has been assigned to a transfer.', createdAt: '2026-10-08T06:18:00Z', isRead: false },
  { noticeId: 'n_004', category: 'Booking confirmed', title: 'Booking confirmed', message: 'A transfer to Taipa Ferry Terminal was confirmed.', createdAt: '2026-10-08T06:10:00Z', isRead: true },
]
