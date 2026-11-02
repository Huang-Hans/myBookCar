db = db.getSiblingDB('lms_dispatch');
const existing = new Set(db.getCollectionNames());
const collections = ["drivers", "driver_offers", "driver_inspections", "vehicle_issues", "driver_incidents", "driver_preferences", "driver_shifts", "shift_batches", "vehicles", "coupons", "audit_records", "idempotency_records", "notification_events"];
for (const name of collections) {
  if (!existing.has(name)) db.createCollection(name);
}
db.getCollection('drivers').createIndex({"licenseNumber": 1}, {"name": "ux_license_number", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('drivers').createIndex({"phone": 1}, {"name": "ix_phone"});
db.getCollection('drivers').createIndex({"propertyId": 1, "status": 1}, {"name": "ix_property_status"});
db.getCollection('driver_offers').createIndex({"driverId": 1, "status": 1, "expiresAt": 1}, {"name": "ix_driver_status_expires"});
db.getCollection('driver_offers').createIndex({"tripId": 1, "driverId": 1}, {"name": "ix_trip_driver"});
db.getCollection('driver_offers').createIndex({"openTripKey": 1}, {"name": "ux_open_trip", "unique": true, "partialFilterExpression": {"openTripKey": {"$type": "string"}}});
db.getCollection('driver_offers').createIndex({"openDriverKey": 1}, {"name": "ux_open_driver", "unique": true, "partialFilterExpression": {"openDriverKey": {"$type": "string"}}});
db.getCollection('driver_offers').createIndex({"tripId": 1, "createdAt": -1}, {"name": "ix_trip_created"});
db.getCollection('driver_offers').createIndex({"propertyId": 1, "createdAt": -1}, {"name": "ix_property_created"});
db.getCollection('driver_offers').createIndex({"status": 1, "expiresAt": 1}, {"name": "ix_status_expires"});
db.getCollection('driver_inspections').createIndex({"driverId": 1, "submittedAt": -1}, {"name": "ix_driver_submitted"});
db.getCollection('vehicle_issues').createIndex({"driverId": 1, "reportedAt": -1}, {"name": "ix_driver_reported"});
db.getCollection('driver_incidents').createIndex({"driverId": 1, "reportedAt": -1}, {"name": "ix_driver_reported"});
// A driver may work different periods on one date; withdrawn shifts do not occupy the period.
db.getCollection('driver_shifts').createIndex({"driverId": 1, "businessDate": 1, "period": 1}, {"name": "ux_active_driver_date_period", "unique": true, "partialFilterExpression": {"withdrawnAt": null}});
db.getCollection('driver_shifts').createIndex({"businessDate": 1, "period": 1}, {"name": "ix_date_period"});
db.getCollection('driver_shifts').createIndex({"driverId": 1, "businessDate": 1}, {"name": "ix_driver_business_date"});
db.getCollection('vehicles').createIndex({"plate": 1}, {"name": "ux_plate", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('vehicles').createIndex({"driverId": 1}, {"name": "ux_assigned_driver", "unique": true, "partialFilterExpression": {"driverId": {"$type": "string"}}});
db.getCollection('vehicles').createIndex({"propertyId": 1, "status": 1}, {"name": "ix_property_status"});
db.getCollection('coupons').createIndex({"code": 1}, {"name": "ux_code", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('coupons').createIndex({"memberId": 1}, {"name": "ix_member"});
db.getCollection('audit_records').createIndex({"resourceType": 1, "resourceId": 1, "occurredAt": -1}, {"name": "ix_resource_occurred"});
db.getCollection('audit_records').createIndex({"traceId": 1}, {"name": "ix_trace"});
db.getCollection('audit_records').createIndex({"propertyId": 1, "occurredAt": -1}, {"name": "ix_property_occurred"});
db.getCollection('idempotency_records').createIndex({"expiresAt": 1}, {"name": "ttl_expires_at", "expireAfterSeconds": 0});
