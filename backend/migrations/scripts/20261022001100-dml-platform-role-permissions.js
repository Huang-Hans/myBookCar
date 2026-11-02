const documents = EJSON.deserialize([
  {
    "_id": "OPERATIONS_MANAGER",
    "permissions": [
      "guests:read",
      "bookings:read",
      "bookings:write",
      "notifications:read",
      "locations:read",
      "configuration:read",
      "properties:write",
      "dispatch:read",
      "dispatch:write",
      "qualifications:read",
      "qualifications:escalate",
      "qualifications:approve",
      "reports:read",
      "reports:write",
      "users:read",
      "users:write",
      "drivers:read",
      "drivers:write",
      "locations:write",
      "blacklists:read",
      "blacklists:write",
      "audit:read",
      "quotas:read",
      "quotas:write",
      "overrides:read",
      "overrides:request",
      "overrides:decide",
      "approvals:read",
      "approvals:materials",
      "approvals:decide"
    ]
  }
], { relaxed: false });

db = db.getSiblingDB('lms_platform');
for (const document of documents) {
  db.getCollection('role_permissions').updateOne({ _id: document._id }, { $setOnInsert: document }, { upsert: true });
}
