import type { ShiftPeriod } from '../dispatch'

export interface ShiftWithdrawalTarget { shiftId: string; version: number }

export interface WithdrawShiftsRequest {
  businessDate: string
  period: ShiftPeriod
  propertyId: string | null
  isWholePeriod: boolean
  targets: ShiftWithdrawalTarget[]
}

export interface ShiftWithdrawalView { businessDate: string; period: ShiftPeriod; withdrawnCount: number }
