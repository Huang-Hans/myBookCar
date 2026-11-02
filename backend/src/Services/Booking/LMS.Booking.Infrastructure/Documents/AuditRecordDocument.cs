using LMS.Booking.Domain.Auditing;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class AuditRecordDocument
{
    [BsonId]
    public string AuditId { get; init; } = string.Empty;

    [BsonElement("actor")]
    public string Actor { get; init; } = string.Empty;

    [BsonElement("operation")]
    public string Operation { get; init; } = string.Empty;

    [BsonElement("resourceType")]
    public string ResourceType { get; init; } = string.Empty;

    [BsonElement("resourceId")]
    public string ResourceId { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OccurredAt { get; init; }

    [BsonElement("outcome")]
    [BsonRepresentation(BsonType.String)]
    public AuditOutcome Outcome { get; init; }

    [BsonElement("traceId")]
    public string TraceId { get; init; } = string.Empty;

    [BsonElement("sourceService")]
    public string SourceService { get; init; } = string.Empty;

    [BsonElement("changeSummary")]
    [BsonIgnoreIfNull]
    public string? ChangeSummary { get; init; }

    [BsonElement("reasonReference")]
    [BsonIgnoreIfNull]
    public string? ReasonReference { get; init; }

    [BsonElement("propertyId")]
    [BsonIgnoreIfNull]
    public string? PropertyId { get; init; }

    [BsonElement("device")]
    [BsonIgnoreIfNull]
    public string? Device { get; init; }

    [BsonElement("sourceIp")]
    [BsonIgnoreIfNull]
    public string? SourceIp { get; init; }

    [BsonElement("sessionReference")]
    [BsonIgnoreIfNull]
    public string? SessionReference { get; init; }

    [BsonElement("qualification")]
    [BsonIgnoreIfNull]
    public QualificationAuditContextDocument? Qualification { get; init; }

    public static AuditRecordDocument FromDomain(AuditRecord record) => new()
    {
        AuditId = record.AuditId,
        Actor = record.Actor,
        Operation = record.Operation,
        ResourceType = record.ResourceType,
        ResourceId = record.ResourceId,
        OccurredAt = UtcDateTimes.ToStored(record.OccurredAt),
        Outcome = record.Outcome,
        TraceId = record.TraceId,
        SourceService = record.SourceService,
        ChangeSummary = record.ChangeSummary,
        ReasonReference = record.ReasonReference,
        PropertyId = record.PropertyId,
        Device = record.Device,
        SourceIp = record.SourceIp,
        SessionReference = record.SessionReference,
        Qualification = record.Qualification is null ? null : QualificationAuditContextDocument.FromDomain(record.Qualification)
    };

    public AuditRecord ToDomain() => new(
        AuditId,
        Actor,
        Operation,
        ResourceType,
        ResourceId,
        UtcDateTimes.FromStored(OccurredAt),
        Outcome,
        TraceId,
        SourceService,
        ChangeSummary,
        ReasonReference,
        PropertyId,
        Device,
        SourceIp,
        SessionReference,
        Qualification?.ToDomain());
}
