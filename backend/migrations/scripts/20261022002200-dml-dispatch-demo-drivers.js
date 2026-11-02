const documents = EJSON.deserialize([
  {
    "_id": "driver_01a123cf816d7509a3fde2fe3da66882",
    "name": "David Wong",
    "licenseNumber": "MAC-28415",
    "licenseExpiryDate": "2027-06-30",
    "phone": "+85366182400",
    "propertyId": "COD",
    "assignedVehicle": "Mercedes S-Class · MP-28-61",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "May Wong",
      "phone": "+853 6618 2499"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEHFhjviUE/238744uhBwHtMJqGQbrMT3XPkDNOFwRPu8nJohEEqHJWovHTurWRTE6A==",
    "version": 1
  },
  {
    "_id": "driver_01a123d9b78a79fcb6b4193753ec6a90",
    "name": "Alex Chen",
    "licenseNumber": "MAC-99881",
    "licenseExpiryDate": "2028-01-01",
    "phone": "+85366189988",
    "propertyId": "COD",
    "assignedVehicle": "Mercedes S-Class · MP-28-61",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "Lily Chen",
      "phone": "+853 6618 9999"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEDPd6ulpq3u0lFTQKcah+t5vvIVNaEIv+LySc1DzUYRDt95UWWGhKPdxp+1sDfJxLQ==",
    "version": 1
  },
  {
    "_id": "driver_1b3d5d02da374f07a2968ea7b8c1a997",
    "name": "Alex Wong",
    "licenseNumber": "MAC-30456",
    "licenseExpiryDate": "2027-12-31",
    "phone": "+85366180001",
    "propertyId": "COD",
    "assignedVehicle": "Executive Sedan · MP-28-72",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "Karen Wong",
      "phone": "+853 6618 0009"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEA6LXFlC5g1zAgYH7m+9zoD/FcwwNljEUTObRPQZleJAw7X9Fa46uSMnh+QpGCfPVQ==",
    "version": 1
  },
  {
    "_id": "driver_5231fcffe342444da07c6b90325b10dd",
    "name": "Daniel Chan",
    "licenseNumber": "MAC-77120",
    "licenseExpiryDate": "2027-08-31",
    "phone": "+85366180002",
    "propertyId": "COD",
    "assignedVehicle": "Executive Sedan · MC-26-18",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "Joan Chan",
      "phone": "+853 6618 0008"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEL2vcJNB8IzjEqd3h2RbCK4U/4Ugpo2H+oWR+Xw1u2IAPnvHwjqjoJOBXd1+KVCbfg==",
    "version": 1
  },
  {
    "_id": "driver_1c6e20c2f21c498c9dc8413f68d495a8",
    "name": "Driver 13800138001",
    "licenseNumber": "TEST-13800138001",
    "licenseExpiryDate": "2027-06-30",
    "phone": "+8613800138001",
    "propertyId": "COD",
    "assignedVehicle": "Test Vehicle 10001",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "May Wong",
      "phone": "+853 6618 2499"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEHFhjviUE/238744uhBwHtMJqGQbrMT3XPkDNOFwRPu8nJohEEqHJWovHTurWRTE6A==",
    "version": 1
  },
  {
    "_id": "driver_b6d262b9dbc0442a90bf94d85e93348c",
    "name": "Driver 13800138002",
    "licenseNumber": "TEST-13800138002",
    "licenseExpiryDate": "2028-01-01",
    "phone": "+8613800138002",
    "propertyId": "COD",
    "assignedVehicle": "Test Vehicle 10002",
    "status": "ACTIVE",
    "languages": [
      "English",
      "Cantonese"
    ],
    "emergencyContact": {
      "name": "Lily Chen",
      "phone": "+853 6618 9999"
    },
    "availability": null,
    "availabilitySource": null,
    "syncedAt": null,
    "restriction": null,
    "password": "AQAAAAIAAYagAAAAEDPd6ulpq3u0lFTQKcah+t5vvIVNaEIv+LySc1DzUYRDt95UWWGhKPdxp+1sDfJxLQ==",
    "version": 1
  }
], { relaxed: false });

db = db.getSiblingDB('lms_dispatch');
for (const document of documents) {
  db.getCollection('drivers').updateOne({ _id: document._id }, { $setOnInsert: document }, { upsert: true });
}
