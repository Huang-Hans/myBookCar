db = db.getSiblingDB('lms_platform');
db.getCollection('role_permissions').updateOne(
  { _id: 'OPERATIONS_MANAGER', permissions: 'guests:read' },
  { $addToSet: { permissions: 'guests:write' } }
);
