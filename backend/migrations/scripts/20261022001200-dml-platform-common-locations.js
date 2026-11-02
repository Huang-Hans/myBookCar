const documents = EJSON.deserialize([
  {
    "_id": "location_01a1245df2fe7331a3f131ddefe2116e",
    "name": "City of Dreams Lobby",
    "address": "Estrada do Istmo, Cotai",
    "coordinates": {
      "latitude": 22.146112,
      "longitude": 113.571471
    },
    "region": "COTAI",
    "categoryCode": "LC-001",
    "contactPerson": "Concierge desk",
    "contactPhone": "+853 8868 8888",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Main guest pickup point",
    "sortOrder": 1,
    "version": 1
  },
  {
    "_id": "location_01a12464f33a7f8db227b42b3dac436c",
    "name": "City of Dreams Lobby2",
    "address": "Estrada do Istmo, Cotai",
    "coordinates": {
      "latitude": 22.146112,
      "longitude": 113.571471
    },
    "region": "COTAI",
    "categoryCode": "LC-001",
    "contactPerson": "Concierge desk",
    "contactPhone": "+853 8868 8888",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Main guest pickup point",
    "sortOrder": 2,
    "version": 1
  },
  {
    "_id": "location_7b1d68007d3342268103224c9c31aef8",
    "name": "Studio City",
    "address": "Estrada do Istmo, Cotai",
    "coordinates": {
      "latitude": 22.138208,
      "longitude": 113.567197
    },
    "region": "COTAI",
    "categoryCode": "LC-003",
    "contactPerson": "Concierge desk",
    "contactPhone": "+853 8868 3000",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Main lobby guest pickup point",
    "sortOrder": 3,
    "version": 1
  },
  {
    "_id": "location_db709f610db7443aa5035ba32722efb8",
    "name": "Macau International Airport",
    "address": "Avenida Wai Long, Taipa",
    "coordinates": {
      "latitude": 22.154893,
      "longitude": 113.579397
    },
    "region": "AIRPORT",
    "categoryCode": "LC-001",
    "contactPerson": "Concierge desk",
    "contactPhone": "+853 2886 1111",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Arrivals hall pickup point",
    "sortOrder": 4,
    "version": 1
  },
  {
    "_id": "location_3f0974bd2aac44348bf40bacb26237b3",
    "name": "Altira Macau",
    "address": "Avenida de Kwong Tung, Taipa",
    "coordinates": {
      "latitude": 22.158729,
      "longitude": 113.560086
    },
    "region": "COTAI",
    "categoryCode": "LC-003",
    "contactPerson": "Concierge desk",
    "contactPhone": "+853 2886 8000",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Main lobby guest pickup point",
    "sortOrder": 5,
    "version": 1
  },
  {
    "_id": "location_140b0ea7c6ce4d5cab19734e553a429e",
    "name": "Hong Kong International Airport",
    "address": "Cheong Wing Road, Chek Lap Kok",
    "coordinates": {
      "latitude": 22.309553,
      "longitude": 113.940349
    },
    "region": "HONG_KONG",
    "categoryCode": "LC-001",
    "contactPerson": "Concierge desk",
    "contactPhone": "+852 2181 8888",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Arrivals hall pickup point",
    "sortOrder": 6,
    "version": 1
  },
  {
    "_id": "location_a092b5a420e84187aa16d535c7fc59d7",
    "name": "Shun Tak Centre, Sheung Wan",
    "address": "200 Connaught Road Central, Sheung Wan",
    "coordinates": {
      "latitude": 22.286058,
      "longitude": 114.157142
    },
    "region": "HONG_KONG",
    "categoryCode": "LC-002",
    "contactPerson": "Concierge desk",
    "contactPhone": "+852 2859 4000",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Ferry terminal pickup point",
    "sortOrder": 7,
    "version": 1
  },
  {
    "_id": "location_e592d8663a1249088b5322d558a0e898",
    "name": "Macau Ferry Terminal",
    "address": "Avenida da Amizade, Macau",
    "coordinates": {
      "latitude": 22.194488,
      "longitude": 113.563976
    },
    "region": "MACAU_PENINSULA",
    "categoryCode": "LC-002",
    "contactPerson": "Concierge desk",
    "contactPhone": "+852 8790 7039",
    "operatingHours": "24 hours",
    "geofence": {
      "isEnabled": true,
      "radiusMeters": 150
    },
    "isActive": true,
    "notes": "Ferry terminal pickup point",
    "sortOrder": 8,
    "version": 1
  }
], { relaxed: false });

db = db.getSiblingDB('lms_platform');
for (const document of documents) {
  db.getCollection('common_locations').updateOne({ _id: document._id }, { $setOnInsert: document }, { upsert: true });
}
