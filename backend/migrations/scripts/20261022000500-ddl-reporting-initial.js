db = db.getSiblingDB('lms_reporting');
const existing = new Set(db.getCollectionNames());
const collections = ["report_rows", "report_refresh_locks", "report_schedules", "report_exports", "file_metadata", "audit_records", "idempotency_records"];
for (const name of collections) {
  if (!existing.has(name)) db.createCollection(name);
}
db.getCollection('report_rows').createIndex({"rowType": 1, "filter.propertyId": 1, "filter.from": 1, "filter.to": 1}, {"name": "ix_type_property_window"});
db.getCollection('report_rows').createIndex({"filter.from": 1}, {"name": "ix_window_from"});
db.getCollection('report_refresh_locks').createIndex({"expiresAt": 1}, {"name": "ttl_expires_at", "expireAfterSeconds": 0});
db.getCollection('report_schedules').createIndex({"scopeKey": 1, "recipientEmail": 1, "frequency": 1, "reportType": 1}, {"name": "ux_scope_email_frequency_type", "unique": true});
db.getCollection('report_exports').createIndex({"state": 1, "requestedAt": 1}, {"name": "ix_state_requested"});
db.getCollection('file_metadata').createIndex({"exportId": 1}, {"name": "ix_export"});
db.getCollection('audit_records').createIndex({"resourceType": 1, "resourceId": 1, "occurredAt": -1}, {"name": "ix_resource_occurred"});
db.getCollection('audit_records').createIndex({"traceId": 1}, {"name": "ix_trace"});
db.getCollection('audit_records').createIndex({"propertyId": 1, "occurredAt": -1}, {"name": "ix_property_occurred"});
db.getCollection('idempotency_records').createIndex({"expiresAt": 1}, {"name": "ttl_expires_at", "expireAfterSeconds": 0});
