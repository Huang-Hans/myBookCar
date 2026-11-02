// Routes and field names follow the Portal menu matrix and the combined LMS OpenAPI document.
export interface PortalSource {
  path: string
  fields: readonly string[]
  idField?: string
  hasDetails?: boolean
  isSingle?: boolean
  isPaged?: boolean
  isSearchable?: boolean
  query?: Readonly<Record<string, string>>
}

export interface PortalSourceGroup {
  defaultTab: string
  tabs: Readonly<Record<string, PortalSource>>
}

const source = (path: string, fields: readonly string[], options: Omit<PortalSource, 'path' | 'fields'> = {}): PortalSource => ({ path, fields, isPaged: true, isSearchable: true, ...options })

export const portalSources: Readonly<Record<string, PortalSourceGroup>> = {
  locations: { defaultTab: 'locations', tabs: {
    locations: source('/api/platform/staff/v1/locations', ['name', 'region', 'categoryCode', 'address', 'coordinates', 'operatingHours', 'isActive'], { idField: 'locationId', hasDetails: true }),
  } },
  properties: { defaultTab: 'properties', tabs: {
    properties: source('/api/platform/staff/v1/properties', ['propertyId', 'name', 'address', 'contactPerson', 'assignedFleetCount', 'operatingHours', 'serviceLevel'], { idField: 'propertyId', hasDetails: true }),
  } },
  reasons: { defaultTab: 'reasons', tabs: {
    reasons: source('/api/platform/staff/v1/cancellation-reasons', ['reasonCode', 'name', 'pointsReturnPolicyCode', 'pointsReturnReference', 'source'], { idField: 'reasonCode', hasDetails: true }),
  } },
  travel: { defaultTab: 'registry', tabs: {
    registry: source('/api/platform/staff/v1/travel-time-indices', ['routeCategory', 'bufferMinutes', 'navigationProvider', 'isTrafficAwareEtaEnabled', 'applicationRule'], { idField: 'routeCategoryCode', hasDetails: true }),
    navigation: source('/api/platform/staff/v1/integration-references', ['name', 'configuredStatus', 'purpose', 'dataSource'], { idField: 'integrationCode', hasDetails: true, isPaged: false, isSearchable: false, query: { group: 'NAVIGATION' } }),
  } },
  notifications: { defaultTab: 'templates', tabs: {
    templates: source('/api/platform/staff/v1/notification-templates', ['templateId', 'eventType', 'eventTrigger', 'channels', 'isActive'], { idField: 'templateId', hasDetails: true }),
    channels: source('/api/platform/staff/v1/notification-channel-configurations', ['eventType', 'channels', 'eventTrigger', 'isActive']),
    history: source('/api/notification/staff/v1/notification-history', ['notificationId', 'recipient', 'channel', 'eventType', 'status', 'occurredAt'], { idField: 'notificationId', hasDetails: true }),
  } },
  categories: { defaultTab: 'categories', tabs: {
    categories: source('/api/platform/staff/v1/location-categories', ['categoryCode', 'name', 'description', 'isActive'], { idField: 'categoryCode', hasDetails: true }),
  } },
  blacklist: { defaultTab: 'customers', tabs: {
    customers: source('/api/platform/staff/v1/customer-blacklists', ['customerName', 'memberId', 'reason', 'effectiveDate', 'expiryDate', 'isActive'], { idField: 'blacklistId', hasDetails: true }),
    drivers: source('/api/dispatch/staff/v1/drivers', ['name', 'driverId', 'restriction.reason', 'restriction.effectiveDate', 'availability', 'status'], { idField: 'driverId', hasDetails: true, query: { view: 'BLACKLIST' } }),
    hitAlerts: source('/api/booking/staff/v1/blacklist-hits', ['tripReference', 'targetType', 'targetId', 'reason', 'propertyId'], { idField: 'approvalId', isPaged: false, isSearchable: false }),
  } },
  tiers: { defaultTab: 'membershipTiers', tabs: {
    membershipTiers: source('/api/platform/staff/v1/membership-tiers', ['name', 'monthlyEntitlementQuota', 'bookingPriority', 'vehicleUpgradeReference', 'advanceBookingWindowHours', 'cancellationFlexibilityReference'], { idField: 'tierCode', isPaged: false, isSearchable: false }),
    loyaltySync: source('/api/platform/staff/v1/loyalty-qualification-rules', ['authority', 'pointsEligibility', 'playQualificationReference', 'visitRequirementReference', 'specialDesignationReference'], { isSingle: true, isPaged: false, isSearchable: false }),
  } },
  selfBooking: { defaultTab: 'quotas', tabs: {
    bookingRules: source('/api/platform/staff/v1/self-booking-rules', ['minimumLeadTimeHours', 'maximumAdvanceDays', 'maximumActiveRequestsPerGuestPerDay', 'propertyAccessRule', 'eligibilitySource'], { isSingle: true, isPaged: false, isSearchable: false }),
    eligibilityRules: source('/api/platform/staff/v1/self-booking-eligibility-rules', ['eligibleTierCodes', 'entitlementRule', 'pointsValidationRule', 'riskCheckRule', 'exhaustionRule'], { isSingle: true, isPaged: false, isSearchable: false }),
    quotas: source('/api/booking/staff/v1/property-quotas', ['propertyId', 'dailyUsed', 'dailyQuota', 'weeklyUsed', 'weeklyQuota', 'monthlyUsed', 'monthlyQuota', 'exhaustionPolicy'], { idField: 'propertyId', hasDetails: true }),
    overrideAndEscalation: source('/api/booking/staff/v1/quota-overrides', ['overrideId', 'propertyId', 'guestId', 'additionalDailyEntitlements', 'status', 'requestedAt'], { idField: 'overrideId' }),
  } },
  csQualify: { defaultTab: 'qualificationReferences', tabs: {
    csBookingRules: source('/api/platform/staff/v1/customer-service-qualification-rules', ['eligibleGuestRule', 'justificationRule', 'approvalThresholdRule', 'verificationSource'], { isSingle: true, isPaged: false, isSearchable: false }),
    qualificationReferences: source('/api/booking/staff/v1/qualification-references', ['guest.name', 'qualificationId', 'propertyId', 'loyaltyOutcome', 'status', 'operatedAt'], { idField: 'qualificationId', hasDetails: true }),
    qualificationAudit: source('/api/booking/staff/v1/qualification-audits', ['actor', 'operation', 'resourceId', 'outcome', 'occurredAt', 'traceId'], { idField: 'auditId' }),
  } },
  system: { defaultTab: 'users', tabs: {
    users: source('/api/platform/staff/v1/users', ['name', 'corporateEmail', 'roleCode', 'propertyId', 'identityProvider', 'isActive'], { idField: 'userId', hasDetails: true }),
    permissions: source('/api/platform/staff/v1/role-permission-references', ['roleCode', 'roleName', 'canManageDrivers', 'canViewReports', 'canReviewApprovals', 'canManageConfiguration'], { idField: 'roleCode', isPaged: false, isSearchable: false }),
    parameters: source('/api/platform/staff/v1/system-parameters', ['language', 'timeZone', 'dateFormat', 'sessionTimeoutMinutes', 'passwordPolicyReference', 'pointsRedemptionMode'], { isSingle: true, isPaged: false, isSearchable: false }),
    integrations: source('/api/platform/staff/v1/integration-references', ['name', 'configuredStatus', 'purpose', 'configurationOwnership', 'environmentLabel'], { idField: 'integrationCode', hasDetails: true, isPaged: false, isSearchable: false, query: { group: 'SYSTEM' } }),
    auditLog: source('/api/platform/staff/v1/audit-records', ['actor', 'operation', 'resourceType', 'resourceId', 'occurredAt', 'outcome'], { idField: 'auditId' }),
  } },
  register: { defaultTab: 'register', tabs: {
    register: source('/api/platform/staff/v1/configuration-register', ['areaName', 'mechanism', 'isEditable', 'owner', 'versionLabel', 'sourceLabel'], { idField: 'areaCode' }),
  } },
}

const tabLabels: Readonly<Record<string, string>> = {
  hitAlerts: 'Hit alerts',
  membershipTiers: 'Membership Tiers',
  loyaltySync: 'Loyalty Sync',
  overrideAndEscalation: 'Override & Escalation',
  csBookingRules: 'CS Booking Rules',
  auditLog: 'Audit Log',
}

export function portalTabLabel(key: string): string {
  return tabLabels[key] ?? key.replace(/([A-Z])/g, ' $1').replace(/^./, (letter) => letter.toUpperCase())
}

export function portalSourcePath(value: PortalSource, page: number, pageSize: number, search: string): string {
  const query = new URLSearchParams(value.query)
  if (value.isPaged) { query.set('page', String(page)); query.set('pageSize', String(pageSize)) }
  if (value.isSearchable && search.trim()) query.set('search', search.trim())
  const suffix = query.toString()
  return suffix ? `${value.path}?${suffix}` : value.path
}

export function portalField(record: Record<string, unknown>, path: string): unknown {
  return path.split('.').reduce<unknown>((value, key) => value && typeof value === 'object' && key in value ? (value as Record<string, unknown>)[key] : undefined, record)
}
