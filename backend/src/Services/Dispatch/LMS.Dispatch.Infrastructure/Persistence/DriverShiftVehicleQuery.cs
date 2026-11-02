using MongoDB.Bson;

namespace LMS.Dispatch.Infrastructure.Persistence;

internal static class DriverShiftVehicleQuery
{
    public static BsonDocument AssignedLookup() => Lookup("currentDriver.assignedVehicleId", "_id",
        "currentDriverVehicle", new BsonDocument("$eq", new BsonArray { "$propertyId", "$$propertyId" }));

    public static BsonDocument LegacyLookup() => Lookup("driverId", "driverId", "legacyDriverVehicle",
        new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { "$propertyId", "$$propertyId" }),
            new BsonDocument("$eq", new BsonArray { "$$vehicleId", BsonNull.Value })
        }));

    public static BsonDocument CurrentPlate() => new("$set", new BsonDocument(DriverShiftReadQuery.VehiclePlateField,
        new BsonDocument("$ifNull", new BsonArray
        {
            new BsonDocument("$arrayElemAt", new BsonArray
            {
                new BsonDocument("$concatArrays", new BsonArray { "$currentDriverVehicle.plate", "$legacyDriverVehicle.plate" }), 0
            }), BsonNull.Value
        })));

    public static BsonDocument CurrentGrade() => new("$set", new BsonDocument(DriverShiftReadQuery.VehicleGradeField,
        new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { CurrentValue("status"), "OUT_OF_SERVICE" }),
            BsonNull.Value, CurrentValue("grade")
        })));

    private static BsonDocument CurrentValue(string field) => new("$ifNull", new BsonArray
    {
        new BsonDocument("$arrayElemAt", new BsonArray
        {
            new BsonDocument("$concatArrays", new BsonArray { "$currentDriverVehicle." + field, "$legacyDriverVehicle." + field }), 0
        }), BsonNull.Value
    });

    private static BsonDocument Lookup(string localField, string foreignField, string result, BsonDocument matches) =>
        new("$lookup", new BsonDocument
        {
            ["from"] = DispatchCollections.Vehicles, ["localField"] = localField, ["foreignField"] = foreignField,
            ["let"] = new BsonDocument
            {
                ["propertyId"] = "$" + DriverShiftReadQuery.PropertyField,
                ["vehicleId"] = new BsonDocument("$ifNull", new BsonArray
                {
                    new BsonDocument("$arrayElemAt", new BsonArray { "$currentDriver.assignedVehicleId", 0 }), BsonNull.Value
                })
            },
            ["pipeline"] = new BsonArray
            {
                new BsonDocument("$match", new BsonDocument("$expr", matches)),
                new BsonDocument("$project", new BsonDocument { ["plate"] = 1, ["grade"] = 1, ["status"] = 1 })
            },
            ["as"] = result
        });
}
