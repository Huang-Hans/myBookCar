using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Reports;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class DriverIncidentDocument
{
    [BsonId]
    public string IncidentId { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("vehicleSnapshot")]
    public string VehicleSnapshot { get; init; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    [BsonElement("description")]
    public string Description { get; init; } = string.Empty;

    [BsonElement("tripId")]
    public string? TripId { get; init; }

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("reportedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReportedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static DriverIncidentDocument FromDomain(DriverIncident incident) => new()
    {
        IncidentId = incident.IncidentId,
        DriverId = incident.DriverId,
        PropertyId = incident.PropertyId,
        VehicleSnapshot = incident.VehicleSnapshot,
        Type = WireEnum.ToWire(incident.Type),
        OccurredAt = UtcDateTimes.ToStored(incident.OccurredAt),
        Description = incident.Description,
        TripId = incident.TripId,
        Status = WireEnum.ToWire(incident.Status),
        ReportedAt = UtcDateTimes.ToStored(incident.ReportedAt),
        Version = incident.Version
    };

    public DriverIncident ToDomain() => new(
        IncidentId,
        DriverId,
        PropertyId,
        VehicleSnapshot,
        StoredValues.ParseEnum<IncidentType>(Type, "type"),
        UtcDateTimes.FromStored(OccurredAt),
        Description,
        TripId,
        StoredValues.ParseEnum<IncidentStatus>(Status, "status"),
        UtcDateTimes.FromStored(ReportedAt),
        Version);
}
