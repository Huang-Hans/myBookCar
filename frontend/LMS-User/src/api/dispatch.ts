import { api } from './session'
import type { ShiftWithdrawalView, WithdrawShiftsRequest } from './contracts/shiftWithdrawals'
import type { SuccessEnvelope } from './http'
import type { PatchCouponRequest } from './contracts/couponChanges'
export type { PatchCouponRequest } from './contracts/couponChanges'

const root = '/api/dispatch/staff/v1'

function withQuery(path: string, values: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== '') search.set(key, String(value))
  })
  const suffix = search.toString()
  return suffix ? `${path}?${suffix}` : path
}

function shiftBoardPath(date: string, propertyId?: string): string {
  const search = new URLSearchParams()
  search.set('Date', date)
  if (propertyId) search.set('PropertyId', propertyId)
  return `${root}/shifts/board?${search}`
}

function id(value: string): string { return encodeURIComponent(value) }
function json(body: object, method: 'POST' | 'PUT' | 'PATCH', create = false): RequestInit {
  const headers = new Headers()
  if (create) headers.set('Idempotency-Key', crypto.randomUUID())
  return { method, headers, body: JSON.stringify(body) }
}

export type CouponType = 'FREE_TRIP' | 'DISCOUNT' | 'FARE_DEDUCTION'
export type CouponStatus = 'ACTIVE' | 'UPCOMING' | 'EXHAUSTED' | 'DISABLED' | 'EXPIRED'
export type CouponScope = 'ALL_PROPERTIES' | 'PROPERTY' | 'VEHICLE'
export interface CouponView {
  couponId: string
  code: string
  name: string
  memberId: string
  type: CouponType
  faceAmount: number
  scope: CouponScope
  propertyId: string | null
  vehicleRestriction: string | null
  validFrom: string
  validTo: string
  issuedQuantity: number
  redeemedCount: number
  status: CouponStatus
  version: number
}
export interface CouponSummaryView { active: number; issuedQuantity: number; redeemedCount: number; upcoming: number }
// WINSON-DEVIATION: Upstream uses `disabled` and overloads `faceAmount` as trips, percent, or MOP whole units; keep these shapes at the API boundary.
export interface CreateCouponRequest { code: string; name: string; memberId: string; type: CouponType; faceAmount: number; scope: CouponScope; propertyId?: string; vehicleRestriction?: string; validFrom: string; validTo: string; issuedQuantity: number; disabled?: boolean }

export type VehicleGrade = 'BUSINESS_SEDAN' | 'LUXURY_MPV' | 'LUXURY_SEDAN'
export type VehicleStatus = 'AVAILABLE' | 'ON_TRIP' | 'MAINTENANCE' | 'OUT_OF_SERVICE'
export interface VehicleView { vehicleId: string; plate: string; model: string; grade: VehicleGrade; propertyId: string; seats: number; mileageKm: number; status: VehicleStatus; driverId: string | null; driverName: string | null; version: number }
export interface VehicleSummaryView { available: number; onTrip: number; maintenance: number; outOfService: number }
export interface CreateVehicleRequest { plate: string; model: string; grade: VehicleGrade; propertyId: string; seats: number; mileageKm: number; status: VehicleStatus; driverId?: string }

export type ShiftPeriod = 'MORNING' | 'AFTERNOON' | 'NIGHT'
export type ShiftStatus = 'ON_DUTY' | 'REST' | 'LEAVE'
export type ShiftRole = 'PREFERRED' | 'STANDBY'
// WINSON-DEVIATION: The upstream Shift DTO uses `endsNextDay`; retain it only at the API boundary.
export interface ShiftView { shiftId: string; businessDate: string; period: ShiftPeriod; startsAt: string; endsAt: string; endsNextDay: boolean; driverId: string; driverName: string | null; propertyId: string; vehiclePlate: string | null; vehicleGrade?: VehicleGrade | null; status: ShiftStatus; role: ShiftRole; attendance: string | null; badges: string[]; version: number }
export interface ShiftColumnView { period: ShiftPeriod; startsAt: string; endsAt: string; endsNextDay: boolean; count: number; shifts: ShiftView[] }
export interface ShiftBoardView { timeZone: string; businessDate: string; propertyId: string | null; periods: ShiftColumnView[] }
export interface ShiftUpcomingDayView { businessDate: string; count: number; shifts: ShiftView[] }
export interface ShiftUpcomingView { timeZone: string; from: string; to: string; propertyId: string | null; days: ShiftUpcomingDayView[] }
export interface CreateShiftRequest { businessDate: string; driverId: string; period: ShiftPeriod; status: ShiftStatus; role: ShiftRole }
export interface PatchShiftRequest { status?: ShiftStatus; role?: ShiftRole; attendance?: string }

export const dispatchApi = {
  coupons: (query: { page?: number; pageSize?: number; search?: string; memberId?: string; type?: CouponType; status?: CouponStatus } = {}): Promise<SuccessEnvelope<CouponView[]>> =>
    api.requestEnvelope<CouponView[]>(withQuery(`${root}/coupons`, { page: 1, pageSize: 20, ...query })),
  couponSummary: () => api.request<CouponSummaryView>(`${root}/coupons/summary`),
  coupon: (couponId: string) => api.request<CouponView>(`${root}/coupons/${id(couponId)}`),
  createCoupon: (body: CreateCouponRequest) => api.request<CouponView>(`${root}/coupons`, json(body, 'POST', true)),
  updateCoupon: (couponId: string, body: PatchCouponRequest, version: number) => api.request<CouponView>(`${root}/coupons/${id(couponId)}`, { ...json(body, 'PATCH'), headers: new Headers([['X-Expected-Version', String(version)]]) }),
  removeCoupon: (couponId: string) => api.request<null>(`${root}/coupons/${id(couponId)}`, { method: 'DELETE' }),
  vehicles: (query: { page?: number; pageSize?: number; search?: string; propertyId?: string; grade?: VehicleGrade; status?: VehicleStatus } = {}): Promise<SuccessEnvelope<VehicleView[]>> =>
    api.requestEnvelope<VehicleView[]>(withQuery(`${root}/vehicles`, { page: 1, pageSize: 20, ...query })),
  vehicleSummary: (propertyId?: string) => api.request<VehicleSummaryView>(withQuery(`${root}/vehicles/summary`, { propertyId })),
  vehicle: (vehicleId: string) => api.request<VehicleView>(`${root}/vehicles/${id(vehicleId)}`),
  createVehicle: (body: CreateVehicleRequest) => api.request<VehicleView>(`${root}/vehicles`, json(body, 'POST', true)),
  // WINSON-DEVIATION: The upstream shift-board contract names these query parameters Date and PropertyId.
  shiftBoard: (date: string, propertyId?: string) => api.request<ShiftBoardView>(shiftBoardPath(date, propertyId)),
  shiftBoardAt: (pickupAt: string, propertyId?: string) =>
    api.request<ShiftBoardView>(withQuery(`${root}/shifts/board`, { pickupAt: new Date(pickupAt).toISOString().replace(/\.\d{3}Z$/, 'Z'), propertyId })),
  shiftCalendar: (from: string, to: string, propertyId?: string, signal?: AbortSignal) =>
    api.request<ShiftBoardView[]>(withQuery(`${root}/shifts/calendar`, { from, to, propertyId }), { signal }),
  upcomingShifts: (propertyId?: string) => api.request<ShiftUpcomingView>(withQuery(`${root}/shifts/upcoming`, { propertyId })),
  shift: (shiftId: string) => api.request<ShiftView>(`${root}/shifts/${id(shiftId)}`),
  createShift: <T extends CreateShiftRequest>(body: T) => api.request<ShiftView>(`${root}/shifts`, json({ businessDate: body.businessDate, driverId: body.driverId, period: body.period, status: body.status, role: body.role }, 'POST', true)),
  withdrawShifts: (body: WithdrawShiftsRequest) => api.request<ShiftWithdrawalView>(`${root}/shifts/withdrawals`, json(body, 'POST')),
  updateShift: (shiftId: string, body: PatchShiftRequest) => api.request<ShiftView>(`${root}/shifts/${id(shiftId)}`, json(body, 'PATCH')),
  assignShiftVehicle: (shiftId: string, vehiclePlate: string) => api.request<ShiftView>(`${root}/shifts/${id(shiftId)}/vehicle`, json({ vehiclePlate }, 'PUT')),
  clearShiftVehicle: (shiftId: string) => api.request<ShiftView>(`${root}/shifts/${id(shiftId)}/vehicle`, { method: 'DELETE' }),
}
