using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Reports;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class VehicleIssueDocument
{
    [BsonId]
    public string IssueId { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("vehicleSnapshot")]
    public string VehicleSnapshot { get; init; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; init; } = string.Empty;

    [BsonElement("inspectionId")]
    public string? InspectionId { get; init; }

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("reportedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReportedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static VehicleIssueDocument FromDomain(VehicleIssue issue) => new()
    {
        IssueId = issue.IssueId,
        DriverId = issue.DriverId,
        PropertyId = issue.PropertyId,
        VehicleSnapshot = issue.VehicleSnapshot,
        Description = issue.Description,
        InspectionId = issue.InspectionId,
        Status = WireEnum.ToWire(issue.Status),
        ReportedAt = UtcDateTimes.ToStored(issue.ReportedAt),
        Version = issue.Version
    };

    public VehicleIssue ToDomain() => new(
        IssueId,
        DriverId,
        PropertyId,
        VehicleSnapshot,
        Description,
        InspectionId,
        StoredValues.ParseEnum<VehicleIssueStatus>(Status, "status"),
        UtcDateTimes.FromStored(ReportedAt),
        Version);
}
