using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Inspections;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class DriverInspectionDocument
{
    [BsonId]
    public string InspectionId { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("vehicleSnapshot")]
    public string VehicleSnapshot { get; init; } = string.Empty;

    [BsonElement("phase")]
    public string Phase { get; init; } = string.Empty;

    [BsonElement("templateVersion")]
    public int TemplateVersion { get; init; }

    [BsonElement("answers")]
    public List<InspectionAnswerDocument> Answers { get; init; } = [];

    // 冗余的汇总标志，供按结果筛选使用；由 Answers 派生，不单独写入业务含义。
    [BsonElement("hasFailure")]
    public bool HasFailure { get; init; }

    [BsonElement("submittedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime SubmittedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static DriverInspectionDocument FromDomain(DriverInspection inspection) => new()
    {
        InspectionId = inspection.InspectionId,
        DriverId = inspection.DriverId,
        PropertyId = inspection.PropertyId,
        VehicleSnapshot = inspection.VehicleSnapshot,
        Phase = WireEnum.ToWire(inspection.Phase),
        TemplateVersion = inspection.TemplateVersion,
        Answers = inspection.Answers.Select(InspectionAnswerDocument.FromDomain).ToList(),
        HasFailure = inspection.HasFailure,
        SubmittedAt = UtcDateTimes.ToStored(inspection.SubmittedAt),
        Version = inspection.Version
    };

    public DriverInspection ToDomain() => new(
        InspectionId,
        DriverId,
        PropertyId,
        VehicleSnapshot,
        StoredValues.ParseEnum<InspectionPhase>(Phase, "phase"),
        TemplateVersion,
        Answers.Select(answer => answer.ToDomain()).ToList(),
        UtcDateTimes.FromStored(SubmittedAt),
        Version);
}
