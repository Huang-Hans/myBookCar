using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Vehicles;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class VehicleDocument
{
    [BsonId]
    public string VehicleId { get; init; } = string.Empty;

    [BsonElement("plate")]
    public string Plate { get; init; } = string.Empty;

    [BsonElement("model")]
    public string Model { get; init; } = string.Empty;

    [BsonElement("grade")]
    public string Grade { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("seats")]
    public int Seats { get; init; }

    [BsonElement("mileageKm")]
    public int MileageKm { get; init; }

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("driverId")]
    [BsonIgnoreIfNull]
    public string? DriverId { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static VehicleDocument FromDomain(Vehicle vehicle) => new()
    {
        VehicleId = vehicle.VehicleId,
        Plate = vehicle.Plate,
        Model = vehicle.Model,
        Grade = WireEnum.ToWire(vehicle.Grade),
        PropertyId = vehicle.PropertyId,
        Seats = vehicle.Seats,
        MileageKm = vehicle.MileageKm,
        Status = WireEnum.ToWire(vehicle.Status),
        DriverId = vehicle.DriverId,
        Version = vehicle.Version
    };

    public Vehicle ToDomain() => new(
        VehicleId,
        Plate,
        Model,
        StoredValues.ParseEnum<VehicleGrade>(Grade, "grade"),
        PropertyId,
        Seats,
        MileageKm,
        StoredValues.ParseEnum<VehicleStatus>(Status, "status"),
        DriverId,
        Version);
}
