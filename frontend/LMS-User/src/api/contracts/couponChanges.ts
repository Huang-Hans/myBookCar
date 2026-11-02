import type { CouponScope, CouponType } from '../dispatch'

export interface PatchCouponRequest {
  code?: string
  name?: string
  memberId?: string
  type?: CouponType
  faceAmount?: number
  scope?: CouponScope
  propertyId?: string | null
  vehicleRestriction?: string | null
  validFrom?: string
  validTo?: string
  issuedQuantity?: number
  disabled?: boolean
}
