export interface PropertyDraft {
  propertyId: string
  name: string
  address: string
  contactPerson: string
  contactPhone: string
  assignedFleetCount: number
  operatingHours: string
  serviceLevel: string
  minimumLeadTimeHours: number
  cancellationPolicyReference: string
  membershipEligibility: string
  vipOverrideRule: string
  isActive: boolean
}
export interface PropertyRecord extends PropertyDraft {
  dailyQuota?: number | null
  version: number
  isDeleted: boolean
}
export type PropertyPatch = Partial<Omit<PropertyDraft, 'propertyId'>>
export type PropertyStatus = 'active' | 'inactive' | 'deleted'
export interface PropertyQuery { page: number; pageSize: number; search?: string; status?: PropertyStatus }
export interface PropertyPage {
  data: PropertyRecord[]
  meta?: { pagination?: { totalItems: number; totalPages?: number } | null } | null
}
export interface PropertyService {
  list(query: PropertyQuery): Promise<PropertyPage>
  detail(id: string): Promise<PropertyRecord>
  create(draft: PropertyDraft, key: string): Promise<PropertyRecord>
  update(id: string, changes: PropertyPatch, version: number): Promise<PropertyRecord>
  remove(id: string, version: number): Promise<void>
}
