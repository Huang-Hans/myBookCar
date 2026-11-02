using LMS.Platform.Domain.Locations;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class GeofenceDocument
{
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; init; }

    [BsonElement("radiusMeters")]
    public double RadiusMeters { get; init; }

    public static GeofenceDocument FromDomain(Geofence geofence) =>
        new() { IsEnabled = geofence.IsEnabled, RadiusMeters = geofence.RadiusMeters };

    public Geofence ToDomain() => new(IsEnabled, RadiusMeters);
}
