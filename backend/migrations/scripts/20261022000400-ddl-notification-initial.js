db = db.getSiblingDB('lms_notification');
const existing = new Set(db.getCollectionNames());
const collections = ["notification_history"];
for (const name of collections) {
  if (!existing.has(name)) db.createCollection(name);
}
db.getCollection('notification_history').createIndex({"occurredAt": -1}, {"name": "ix_occurred"});
db.getCollection('notification_history').createIndex({"status": 1, "occurredAt": -1}, {"name": "ix_status_occurred"});
