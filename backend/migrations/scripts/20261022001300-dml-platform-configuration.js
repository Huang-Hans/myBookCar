const configuration = EJSON.deserialize({
  "release": {
    "_id": "rel_dev_0000000000000001",
    "environment": "Development",
    "sequence": 1,
    "digest": "sha256:bootstrap-development-v1",
    "approvedBy": "system:bootstrap",
    "effectiveAt": "2026-10-10T02:43:59.728Z",
    "createdAt": "2026-10-10T02:43:59.728Z"
  },
  "activeConfiguration": {
    "_id": "Development",
    "releaseId": "rel_dev_0000000000000001",
    "sequence": 1,
    "version": 1,
    "updatedAt": "2026-10-10T02:43:59.728Z"
  },
  "assets": [
    {
      "_id": "rel_dev_0000000000000001:PROPERTIES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "PROPERTIES",
      "payload": {
        "items": [
          {
            "propertyId": "COD",
            "name": "City of Dreams",
            "address": "Estrada do Istmo, Cotai",
            "contactPerson": "Property transport desk",
            "contactPhone": "+853 8868 8800",
            "assignedFleetCount": 18,
            "operatingHours": "24 hours",
            "serviceLevel": "Pickup within 5 minutes",
            "minimumLeadTimeHours": 24,
            "cancellationPolicyReference": "Standard cancellation reasons CR-001 to CR-005",
            "membershipEligibility": "All active membership tiers",
            "vipOverrideRule": "VIP override approved by Operations Manager",
            "isActive": true,
            "isDeleted": false,
            "version": 1
          },
          {
            "propertyId": "SC",
            "name": "Studio City",
            "address": "Estrada do Istmo, Cotai",
            "contactPerson": "Property transport desk",
            "contactPhone": "+853 8868 8801",
            "assignedFleetCount": 16,
            "operatingHours": "24 hours",
            "serviceLevel": "Pickup within 5 minutes",
            "minimumLeadTimeHours": 24,
            "cancellationPolicyReference": "Standard cancellation reasons CR-001 to CR-005",
            "membershipEligibility": "All active membership tiers",
            "vipOverrideRule": "VIP override approved by Operations Manager",
            "isActive": true,
            "isDeleted": false,
            "version": 1
          },
          {
            "propertyId": "ALT",
            "name": "Altira",
            "address": "Avenida de Kwong Tung, Taipa",
            "contactPerson": "Property transport desk",
            "contactPhone": "+853 8868 8802",
            "assignedFleetCount": 8,
            "operatingHours": "24 hours",
            "serviceLevel": "Pickup within 5 minutes",
            "minimumLeadTimeHours": 24,
            "cancellationPolicyReference": "Standard cancellation reasons CR-001 to CR-005",
            "membershipEligibility": "All active membership tiers",
            "vipOverrideRule": "VIP override approved by Operations Manager",
            "isActive": true,
            "isDeleted": false,
            "version": 1
          },
          {
            "propertyId": "GH",
            "name": "Grand Hyatt",
            "address": "City of Dreams, Cotai",
            "contactPerson": "Property transport desk",
            "contactPhone": "+853 8868 8803",
            "assignedFleetCount": 10,
            "operatingHours": "24 hours",
            "serviceLevel": "Pickup within 5 minutes",
            "minimumLeadTimeHours": 24,
            "cancellationPolicyReference": "Standard cancellation reasons CR-001 to CR-005",
            "membershipEligibility": "All active membership tiers",
            "vipOverrideRule": "VIP override approved by Operations Manager",
            "isActive": true,
            "isDeleted": false,
            "version": 1
          }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:LOCATION_CATEGORIES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "LOCATION_CATEGORIES",
      "payload": {
        "items": [
          { "categoryCode": "LC-001", "name": "Airport", "description": "Airport terminals and arrivals", "isActive": true },
          { "categoryCode": "LC-002", "name": "Ferry Terminal", "description": "Passenger ferry terminals", "isActive": true },
          { "categoryCode": "LC-003", "name": "Casino", "description": "Gaming and entertainment entrances", "isActive": true },
          { "categoryCode": "LC-004", "name": "Hotel", "description": "Hotel lobbies and concierge desks", "isActive": true },
          { "categoryCode": "LC-005", "name": "Shopping Mall", "description": "Retail complex pickup points", "isActive": true },
          { "categoryCode": "LC-006", "name": "Convention Center", "description": "Conference and exhibition entrances", "isActive": true }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:CANCELLATION_REASONS",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "CANCELLATION_REASONS",
      "payload": {
        "items": [
          { "reasonCode": "CR-001", "name": "Customer no-show", "pointsReturnPolicyCode": "PR-002", "pointsReturnReference": "Points forfeiture assessed by loyalty system", "source": "Reference only" },
          { "reasonCode": "CR-002", "name": "Flight delayed", "pointsReturnPolicyCode": "PR-001", "pointsReturnReference": "Points returned under eligibility rules", "source": "Reference only" },
          { "reasonCode": "CR-003", "name": "Changed plans", "pointsReturnPolicyCode": "PR-003", "pointsReturnReference": "Return eligibility depends on notice period", "source": "Reference only" },
          { "reasonCode": "CR-004", "name": "Driver unavailable", "pointsReturnPolicyCode": "PR-001", "pointsReturnReference": "Points returned", "source": "Reference only" },
          { "reasonCode": "CR-005", "name": "Vehicle breakdown", "pointsReturnPolicyCode": "PR-001", "pointsReturnReference": "Points returned", "source": "Reference only" }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:TRAVEL_BUFFERS",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "TRAVEL_BUFFERS",
      "payload": {
        "items": [
          { "routeCategoryCode": "AIRPORT_TRANSFER", "routeCategory": "Airport transfer", "bufferMinutes": 20, "navigationProvider": "GOOGLE_MAPS", "isTrafficAwareEtaEnabled": true, "applicationRule": "Applied before scheduled pickup" },
          { "routeCategoryCode": "FERRY_TERMINAL", "routeCategory": "Ferry terminal", "bufferMinutes": 15, "navigationProvider": "GOOGLE_MAPS", "isTrafficAwareEtaEnabled": true, "applicationRule": "Applied before scheduled pickup" },
          { "routeCategoryCode": "PROPERTY_TRANSFER", "routeCategory": "Property transfer", "bufferMinutes": 10, "navigationProvider": "GOOGLE_MAPS", "isTrafficAwareEtaEnabled": true, "applicationRule": "Applied before scheduled pickup" },
          { "routeCategoryCode": "CROSS_BORDER", "routeCategory": "Cross-border", "bufferMinutes": 60, "navigationProvider": "AMAP", "isTrafficAwareEtaEnabled": true, "applicationRule": "Applied before scheduled pickup" },
          { "routeCategoryCode": "PEAK_HOUR", "routeCategory": "Peak-hour adjustment", "bufferMinutes": 15, "navigationProvider": "GOOGLE_MAPS", "isTrafficAwareEtaEnabled": true, "applicationRule": "Weekdays 17:00-20:00" }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:NOTIFICATION_CHANNELS",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "NOTIFICATION_CHANNELS",
      "payload": {
        "items": [
          { "eventType": "BOOKING_CONFIRMATION", "channels": ["EMAIL", "IN_APP"], "eventTrigger": "Entitlement confirmed", "isActive": true },
          { "eventType": "TRIP_REMINDER", "channels": ["SMS", "PUSH"], "eventTrigger": "30 minutes before pickup", "isActive": true },
          { "eventType": "DRIVER_ASSIGNED", "channels": ["SMS", "IN_APP"], "eventTrigger": "Driver assignment received", "isActive": true },
          { "eventType": "TRIP_STARTED", "channels": ["PUSH", "IN_APP"], "eventTrigger": "Driver starts trip", "isActive": true },
          { "eventType": "BOOKING_CANCELLED", "channels": ["EMAIL", "SMS"], "eventTrigger": "Cancellation confirmed", "isActive": true }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:NOTIFICATION_TEMPLATES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "NOTIFICATION_TEMPLATES",
      "payload": {
        "items": [
          { "templateId": "NT-001", "eventType": "BOOKING_CONFIRMATION", "eventTrigger": "Entitlement confirmed", "channels": ["EMAIL", "IN_APP"], "contentTemplate": "Your booking {tripReference} is confirmed for {pickupAt}.", "isActive": true },
          { "templateId": "NT-002", "eventType": "TRIP_REMINDER", "eventTrigger": "30 minutes before pickup", "channels": ["SMS", "PUSH"], "contentTemplate": "Your limousine arrives in 30 minutes at {pickupName}.", "isActive": true },
          { "templateId": "NT-003", "eventType": "DRIVER_ASSIGNED", "eventTrigger": "Driver assignment received", "channels": ["SMS", "IN_APP"], "contentTemplate": "Driver {driverName} and vehicle {assignedVehicle} are assigned to {tripReference}.", "isActive": true },
          { "templateId": "NT-004", "eventType": "TRIP_STARTED", "eventTrigger": "Driver starts trip", "channels": ["PUSH", "IN_APP"], "contentTemplate": "Your trip {tripReference} has started.", "isActive": true },
          { "templateId": "NT-005", "eventType": "BOOKING_CANCELLED", "eventTrigger": "Cancellation confirmed", "channels": ["EMAIL", "SMS"], "contentTemplate": "Booking {tripReference} is cancelled. Reason: {cancellationReason}.", "isActive": true }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:ROLE_PERMISSION_REFERENCES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "ROLE_PERMISSION_REFERENCES",
      "payload": {
        "items": [
          { "roleCode": "SUPER_ADMIN", "roleName": "Super Admin", "canManageDrivers": true, "canViewReports": true, "canReviewApprovals": true, "canManageConfiguration": true },
          { "roleCode": "OPERATIONS_MANAGER", "roleName": "Operations Manager", "canManageDrivers": true, "canViewReports": true, "canReviewApprovals": true, "canManageConfiguration": false },
          { "roleCode": "DISPATCHER", "roleName": "Dispatcher", "canManageDrivers": true, "canViewReports": false, "canReviewApprovals": false, "canManageConfiguration": false },
          { "roleCode": "FINANCE", "roleName": "Finance", "canManageDrivers": false, "canViewReports": true, "canReviewApprovals": false, "canManageConfiguration": false },
          { "roleCode": "PROPERTY_CS_STAFF", "roleName": "Property CS Staff", "canManageDrivers": false, "canViewReports": false, "canReviewApprovals": true, "canManageConfiguration": false }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:INTEGRATION_REFERENCES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "INTEGRATION_REFERENCES",
      "payload": {
        "items": [
          { "integrationCode": "LOYALTY", "name": "Melco Loyalty System", "configuredStatus": "Connected", "purpose": "Membership tier, points balance and qualification outcome reference", "configurationOwnership": "Loyalty system", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "SYSTEM" },
          { "integrationCode": "PMS", "name": "Property Management System", "configuredStatus": "Connected", "purpose": "Guest profile and stay reference for cross-border approvals", "configurationOwnership": "Property systems", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "SYSTEM" },
          { "integrationCode": "SMS", "name": "SMS Gateway", "configuredStatus": "Configured", "purpose": "SMS delivery channel for trip reminders and driver assignment", "configurationOwnership": "Configuration administrator", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "SYSTEM" },
          { "integrationCode": "EMAIL", "name": "Email Gateway", "configuredStatus": "Configured", "purpose": "Email delivery channel for booking confirmation and cancellation", "configurationOwnership": "Configuration administrator", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "SYSTEM" },
          { "integrationCode": "MAPPING", "name": "Mapping Provider", "configuredStatus": "Configured", "purpose": "Maps provider used to maintain route category buffers", "configurationOwnership": "Configuration administrator", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "SYSTEM" },
          { "integrationCode": "GOOGLE_MAPS", "name": "Google Maps", "configuredStatus": "Connected", "purpose": "Traffic-aware ETA reference for route buffers", "configurationOwnership": "Navigation provider", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "NAVIGATION" },
          { "integrationCode": "AMAP", "name": "Amap", "configuredStatus": "Connected", "purpose": "Traffic-aware ETA reference for cross-border route buffers", "configurationOwnership": "Navigation provider", "environmentLabel": "Development", "dataSource": "Reference configuration only", "accessPolicy": "Read-only reference; the Portal does not call the provider", "group": "NAVIGATION" }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:CONFIGURATION_REGISTER",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "CONFIGURATION_REGISTER",
      "payload": {
        "items": [
          { "areaCode": "PROPERTIES", "areaName": "Property list & details", "mechanism": "PORTAL_SCREEN", "isEditable": true, "owner": "Configuration administrator", "versionLabel": "Live", "sourceLabel": "Property Management", "modulePath": "/properties" },
          { "areaCode": "CANCELLATION_REASONS", "areaName": "Cancellation reasons", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/reasons" },
          { "areaCode": "POINTS_RETURN_POLICY_REFERENCES", "areaName": "Points-return policy references", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/reasons" },
          { "areaCode": "NOTIFICATION_TEMPLATES", "areaName": "Notification templates", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/notifications" },
          { "areaCode": "NOTIFICATION_CHANNELS", "areaName": "Notification channels", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/notifications" },
          { "areaCode": "LOCATION_CATEGORIES", "areaName": "Location categories", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/categories" },
          { "areaCode": "MEMBERSHIP_TIERS", "areaName": "Membership tier definitions", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/tiers" },
          { "areaCode": "LOYALTY_QUALIFICATION_RULES", "areaName": "Tier qualification rules", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Loyalty system", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/tiers" },
          { "areaCode": "SYSTEM_PARAMETERS", "areaName": "System parameters", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/system" },
          { "areaCode": "TRAVEL_BUFFERS", "areaName": "Route-category buffer times", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/travel" },
          { "areaCode": "SELF_BOOKING_ELIGIBILITY_RULES", "areaName": "Service eligibility rules", "mechanism": "CONFIGURATION_UPLOAD", "isEditable": false, "owner": "Configuration administrator", "versionLabel": "2026.06.24", "sourceLabel": "Managed via configuration upload", "modulePath": "/self-booking" },
          { "areaCode": "COMMON_LOCATIONS", "areaName": "Common used locations", "mechanism": "PORTAL_SCREEN", "isEditable": true, "owner": "Operations / Risk control", "versionLabel": "Live", "sourceLabel": "Locations", "modulePath": "/locations" },
          { "areaCode": "PROPERTY_QUOTAS", "areaName": "Complimentary property quotas", "mechanism": "PORTAL_SCREEN", "isEditable": true, "owner": "Operations / Risk control", "versionLabel": "Live", "sourceLabel": "Self-Booking Control", "modulePath": "/self-booking" },
          { "areaCode": "DRIVER_MASTER_DATA", "areaName": "Driver master data", "mechanism": "PORTAL_SCREEN", "isEditable": true, "owner": "Operations / Risk control", "versionLabel": "Live", "sourceLabel": "Driver Management", "modulePath": "/drivers" },
          { "areaCode": "CUSTOMER_BLACKLIST", "areaName": "Customer blacklist", "mechanism": "PORTAL_SCREEN", "isEditable": true, "owner": "Operations / Risk control", "versionLabel": "Live", "sourceLabel": "Blacklist", "modulePath": "/blacklist" }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:MEMBERSHIP_TIERS",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "MEMBERSHIP_TIERS",
      "payload": {
        "items": [
          { "tierCode": "SILVER", "name": "Silver", "monthlyEntitlementQuota": 1, "bookingPriority": "STANDARD", "vehicleUpgradeReference": "Standard rules", "advanceBookingWindowHours": 24, "cancellationFlexibilityReference": "Standard rules" },
          { "tierCode": "GOLD", "name": "Gold", "monthlyEntitlementQuota": 2, "bookingPriority": "STANDARD", "vehicleUpgradeReference": "Standard rules", "advanceBookingWindowHours": 24, "cancellationFlexibilityReference": "Standard rules" },
          { "tierCode": "PLATINUM", "name": "Platinum", "monthlyEntitlementQuota": 4, "bookingPriority": "PRIORITY", "vehicleUpgradeReference": "Included", "advanceBookingWindowHours": 24, "cancellationFlexibilityReference": "Included" },
          { "tierCode": "DIAMOND", "name": "Diamond", "monthlyEntitlementQuota": 8, "bookingPriority": "PRIORITY", "vehicleUpgradeReference": "Included", "advanceBookingWindowHours": 24, "cancellationFlexibilityReference": "Included" },
          { "tierCode": "VIP", "name": "VIP", "monthlyEntitlementQuota": 12, "bookingPriority": "PRIORITY", "vehicleUpgradeReference": "Included", "advanceBookingWindowHours": 24, "cancellationFlexibilityReference": "Included" }
        ]
      }
    },
    {
      "_id": "rel_dev_0000000000000001:LOYALTY_QUALIFICATION_RULES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "LOYALTY_QUALIFICATION_RULES",
      "payload": {
        "authority": "Melco loyalty system · Real-time tier check at request time",
        "pointsEligibility": "Membership points balance and entitlement record",
        "playQualificationReference": "Qualification-outcome reference only",
        "visitRequirementReference": "Visit frequency evaluated by loyalty system",
        "specialDesignationReference": "VIP designation from loyalty profile",
        "pointsExpiryReference": "Points expiry determined by loyalty-system rules"
      }
    },
    {
      "_id": "rel_dev_0000000000000001:SELF_BOOKING_RULES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "SELF_BOOKING_RULES",
      "payload": {
        "minimumLeadTimeHours": 24,
        "maximumAdvanceDays": 30,
        "maximumActiveRequestsPerGuestPerDay": 1,
        "propertyAccessRule": "Assigned property entitlement only",
        "eligibilitySource": "Points balance and transfer entitlement checked by loyalty system"
      }
    },
    {
      "_id": "rel_dev_0000000000000001:SELF_BOOKING_ELIGIBILITY_RULES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "SELF_BOOKING_ELIGIBILITY_RULES",
      "payload": {
        "eligibleTierCodes": ["GOLD", "PLATINUM", "DIAMOND", "VIP"],
        "entitlementRule": "Valid membership transfer entitlement",
        "pointsValidationRule": "Required points available and not expired",
        "riskCheckRule": "No active customer restriction",
        "exhaustionRule": "Apply property waitlist / auto-reject rule"
      }
    },
    {
      "_id": "rel_dev_0000000000000001:CS_QUALIFICATION_RULES",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "CS_QUALIFICATION_RULES",
      "payload": {
        "eligibleGuestRule": "Membership tier and entitlement must qualify in loyalty system",
        "justificationRule": "Required for exceptions and below-threshold requests",
        "approvalThresholdRule": "Below threshold requires Operations Manager approval",
        "verificationSource": "Qualification outcome only; no underlying monetary metrics"
      }
    },
    {
      "_id": "rel_dev_0000000000000001:SYSTEM_PARAMETERS",
      "releaseId": "rel_dev_0000000000000001",
      "areaCode": "SYSTEM_PARAMETERS",
      "payload": {
        "language": "en",
        "timeZone": "Asia/Macau",
        "dateFormat": "YYYY-MM-DD",
        "sessionTimeoutMinutes": 30,
        "passwordPolicyReference": "Corporate SSO password policy",
        "pointsRedemptionMode": "REFERENCE_ONLY",
        "pointsExpiryReference": "Points expiry determined by loyalty-system rules"
      }
    }
  ]
}, { relaxed: false });

db = db.getSiblingDB('lms_platform');
const entries = [
  ['configuration_releases', [configuration.release]],
  ['configuration_assets', configuration.assets],
  ['active_configurations', [configuration.activeConfiguration]],
];
for (const [collection, documents] of entries) {
  for (const document of documents) {
    db.getCollection(collection).updateOne({ _id: document._id }, { $setOnInsert: document }, { upsert: true });
  }
}
