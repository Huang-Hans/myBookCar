using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Domain.Locations;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class CommonLocationDocument
{
    [BsonId]
    public string LocationId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; init; } = string.Empty;

    [BsonElement("coordinates")]
    public CoordinatesDocument Coordinates { get; init; } = new();

    [BsonElement("region")]
    public string Region { get; init; } = string.Empty;

    [BsonElement("categoryCode")]
    public string CategoryCode { get; init; } = string.Empty;

    [BsonElement("contactPerson")]
    public string ContactPerson { get; init; } = string.Empty;

    [BsonElement("contactPhone")]
    public string ContactPhone { get; init; } = string.Empty;

    [BsonElement("operatingHours")]
    public string OperatingHours { get; init; } = string.Empty;

    [BsonElement("geofence")]
    public GeofenceDocument Geofence { get; init; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    [BsonElement("notes")]
    public string? Notes { get; init; }

    [BsonElement("sortOrder")]
    public int SortOrder { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CommonLocationDocument FromDomain(CommonLocation location) => new()
    {
        LocationId = location.LocationId,
        Name = location.Name,
        Address = location.Address,
        Coordinates = CoordinatesDocument.FromDomain(location.Coordinates),
        Region = WireEnum.ToWire(location.Region),
        CategoryCode = location.CategoryCode,
        ContactPerson = location.ContactPerson,
        ContactPhone = location.ContactPhone,
        OperatingHours = location.OperatingHours,
        Geofence = GeofenceDocument.FromDomain(location.Geofence),
        IsActive = location.IsActive,
        Notes = location.Notes,
        SortOrder = location.SortOrder,
        Version = location.Version
    };

    public CommonLocation ToDomain() => new(
        LocationId,
        Name,
        Address,
        Coordinates.ToDomain(),
        StoredValues.ParseEnum<LocationRegion>(Region, "region"),
        CategoryCode,
        ContactPerson,
        ContactPhone,
        OperatingHours,
        Geofence.ToDomain(),
        IsActive,
        Notes,
        SortOrder,
        Version);
}
