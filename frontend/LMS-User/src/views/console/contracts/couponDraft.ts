import type { CouponScope, CouponType } from '../../../api/dispatch'

export interface CouponDraft {
  code: string
  name: string
  memberId: string
  type: CouponType
  faceAmount: string
  scope: CouponScope
  propertyId: string
  vehicleRestriction: string
  validFrom: string
  validTo: string
  issuedQuantity: string
}
