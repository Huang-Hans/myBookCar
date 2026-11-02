db = db.getSiblingDB('lms_platform');
const existing = new Set(db.getCollectionNames());
const collections = ["configuration_releases", "active_configurations", "audit_records", "idempotency_records", "portal_users", "role_permissions", "common_locations", "customer_blacklists", "configuration_assets", "file_metadata", "report_exports", "app_refresh_tokens"];
for (const name of collections) {
  if (!existing.has(name)) db.createCollection(name);
}
db.getCollection('configuration_releases').createIndex({"environment": 1, "sequence": 1}, {"name": "ux_environment_sequence", "unique": true});
db.getCollection('audit_records').createIndex({"resourceType": 1, "resourceId": 1, "occurredAt": -1}, {"name": "ix_resource_occurred"});
db.getCollection('audit_records').createIndex({"traceId": 1}, {"name": "ix_trace"});
db.getCollection('audit_records').createIndex({"propertyId": 1, "occurredAt": -1}, {"name": "ix_property_occurred"});
db.getCollection('idempotency_records').createIndex({"expiresAt": 1}, {"name": "ttl_expires_at", "expireAfterSeconds": 0});
db.getCollection('portal_users').createIndex({"corporateEmail": 1}, {"name": "ux_corporate_email", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('portal_users').createIndex({"identityProvider": 1, "subjectId": 1}, {"name": "ux_identity_subject", "unique": true, "partialFilterExpression": {"subjectId": {"$type": "string"}}});
db.getCollection('common_locations').createIndex({"name": 1}, {"name": "ux_name", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('common_locations').createIndex({"region": 1, "sortOrder": 1}, {"name": "ix_region_sort_order"});
db.getCollection('customer_blacklists').createIndex({"memberId": 1}, {"name": "ux_member_id", "unique": true, "collation": {"locale": "en", "strength": 2}});
db.getCollection('file_metadata').createIndex({"approvalId": 1, "documentType": 1}, {"name": "ix_approval_document"});
db.getCollection('file_metadata').createIndex({"exportId": 1}, {"name": "ix_export", "partialFilterExpression": {"exportId": {"$type": "string"}}});
db.getCollection('report_exports').createIndex({"state": 1, "requestedAt": 1}, {"name": "ix_state_requested"});
db.getCollection('app_refresh_tokens').createIndex({"tokenHash": 1}, {"name": "ux_token_hash", "unique": true});
db.getCollection('app_refresh_tokens').createIndex({"familyId": 1}, {"name": "ix_family"});
db.getCollection('app_refresh_tokens').createIndex({"expiresAt": 1}, {"name": "ttl_expires_at", "expireAfterSeconds": 86400});
