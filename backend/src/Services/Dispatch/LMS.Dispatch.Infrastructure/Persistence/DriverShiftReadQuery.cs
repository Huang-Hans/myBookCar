using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Infrastructure.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Persistence;

internal static class DriverShiftReadQuery
{
    public const string PropertyField = "currentDriverPropertyId";
    public const string VehicleGradeField = "currentDriverVehicleGrade";
    public const string VehiclePlateField = "currentDriverVehiclePlate";

    public static IAggregateFluent<BsonDocument> Create(IMongoDatabase database,
        FilterDefinition<DriverShiftDocument> matches, PropertyScope scope, string? propertyId,
        IClientSessionHandle? session = null)
    {
        var collection = database.GetCollection<DriverShiftDocument>(DispatchCollections.DriverShifts);
        var query = session is null ? collection.Aggregate() : collection.Aggregate(session);
        return query.Match(matches).AppendStage<BsonDocument>(DriverLookup())
            .AppendStage<BsonDocument>(CurrentProperty())
            .Match(Visibility(scope, propertyId))
            .AppendStage<BsonDocument>(DriverShiftVehicleQuery.AssignedLookup())
            .AppendStage<BsonDocument>(DriverShiftVehicleQuery.LegacyLookup())
            .AppendStage<BsonDocument>(DriverShiftVehicleQuery.CurrentPlate())
            .AppendStage<BsonDocument>(DriverShiftVehicleQuery.CurrentGrade())
            .AppendStage<BsonDocument>(new BsonDocument("$project", new BsonDocument
            {
                ["currentDriver"] = 0, ["currentDriverVehicle"] = 0, ["legacyDriverVehicle"] = 0,
                ["propertyId"] = 0, ["vehiclePlate"] = 0, ["vehicleSlot"] = 0
            }));
    }

    public static DriverShift ToDomain(BsonDocument document) =>
        BsonSerializer.Deserialize<DriverShiftDocument>(document).ToDomain() with
        {
            PropertyId = document.GetValue(PropertyField, string.Empty).AsString,
            VehicleGrade = document.GetValue(VehicleGradeField, BsonNull.Value) is { IsBsonNull: false } grade
                ? StoredValues.ParseEnum<VehicleGrade>(grade.AsString, "grade") : null,
            VehiclePlate = document.GetValue(VehiclePlateField, BsonNull.Value) is { IsBsonNull: false } plate ? plate.AsString : null
        };

    private static BsonDocument DriverLookup() => new("$lookup", new BsonDocument
    {
        ["from"] = DispatchCollections.Drivers,
        ["localField"] = "driverId",
        ["foreignField"] = "_id",
        ["pipeline"] = new BsonArray { new BsonDocument("$project", new BsonDocument { ["propertyId"] = 1, ["assignedVehicleId"] = 1 }) },
        ["as"] = "currentDriver"
    });

    private static BsonDocument CurrentProperty() => new("$set", new BsonDocument(PropertyField,
        new BsonDocument("$ifNull", new BsonArray
        {
            new BsonDocument("$arrayElemAt", new BsonArray { "$currentDriver.propertyId", 0 }), string.Empty
        })));

    private static FilterDefinition<BsonDocument> Visibility(PropertyScope scope, string? propertyId)
    {
        var filter = Builders<BsonDocument>.Filter;
        var visible = scope.IsAllProperties ? filter.Empty : filter.Eq(PropertyField, scope.PropertyId);
        return propertyId is null ? visible : filter.And(visible, filter.Eq(PropertyField, propertyId));
    }
}
