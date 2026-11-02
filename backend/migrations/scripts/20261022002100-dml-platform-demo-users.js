const documents = EJSON.deserialize([
  {
    "_id": "user_01a11f60d0047afca13c0ffb9188283b",
    "name": "Local Development",
    "corporateEmail": "local.development@winson.local",
    "roleCode": "SUPER_ADMIN",
    "hasAllPropertyAccess": true,
    "propertyId": null,
    "isActive": true,
    "identityProvider": "KEYCLOAK",
    "subjectId": null,
    "version": 1
  },
  {
    "_id": "user_01a128e6d8a070d5924a5493d937b1c5",
    "name": "Andrew Lau",
    "corporateEmail": "andrew.lau@melco-resorts.com",
    "roleCode": "OPERATIONS_MANAGER",
    "hasAllPropertyAccess": true,
    "propertyId": null,
    "isActive": true,
    "identityProvider": "KEYCLOAK",
    "subjectId": null,
    "version": 1
  },
  {
    "_id": "user_01a128eeb50a799faa459d249a49d5e0",
    "name": "Andrew Lau",
    "corporateEmail": "lms@lms.local",
    "roleCode": "OPERATIONS_MANAGER",
    "hasAllPropertyAccess": true,
    "propertyId": null,
    "isActive": true,
    "identityProvider": "KEYCLOAK",
    "subjectId": null,
    "version": 1
  }
], { relaxed: false });

db = db.getSiblingDB('lms_platform');
for (const document of documents) {
  db.getCollection('portal_users').updateOne({ _id: document._id }, { $setOnInsert: document }, { upsert: true });
}
