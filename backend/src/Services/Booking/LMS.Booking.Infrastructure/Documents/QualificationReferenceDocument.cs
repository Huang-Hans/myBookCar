using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Qualifications;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class QualificationReferenceDocument
{
    [BsonId]
    public string QualificationId { get; init; } = string.Empty;

    [BsonElement("guest")]
    public GuestSnapshotDocument Guest { get; init; } = new();

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("loyaltyOutcome")]
    public string LoyaltyOutcome { get; init; } = string.Empty;

    [BsonElement("qualificationReference")]
    public string QualificationReference { get; init; } = string.Empty;

    [BsonElement("entitlementType")]
    public string EntitlementType { get; init; } = string.Empty;

    [BsonElement("pointsBalance")]
    public long? PointsBalance { get; init; }

    [BsonElement("pointsRequired")]
    public long? PointsRequired { get; init; }

    [BsonElement("approvalChain")]
    public List<string> ApprovalChain { get; init; } = [];

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("justification")]
    public string? Justification { get; init; }

    [BsonElement("operatorId")]
    public string? OperatorId { get; init; }

    [BsonElement("operatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? OperatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    [BsonElement("tripReference")]
    public string? TripReference { get; init; }

    // 经理复审的申请快照；早于该字段的存量记录没有该元素。
    [BsonElement("request")]
    public QualificationRequestDocument? Request { get; init; }

    public static QualificationReferenceDocument FromDomain(Domain.Qualifications.QualificationReference reference) => new()
    {
        QualificationId = reference.QualificationId,
        Guest = GuestSnapshotDocument.FromDomain(reference.Guest),
        PropertyId = reference.PropertyId,
        LoyaltyOutcome = reference.LoyaltyOutcome,
        QualificationReference = reference.QualificationReferenceCode,
        EntitlementType = reference.EntitlementType,
        PointsBalance = reference.PointsBalance,
        PointsRequired = reference.PointsRequired,
        ApprovalChain = reference.ApprovalChain.ToList(),
        Status = WireEnum.ToWire(reference.Status),
        Justification = reference.Justification,
        OperatorId = reference.OperatorId,
        OperatedAt = reference.OperatedAt is null ? null : UtcDateTimes.ToStored(reference.OperatedAt.Value),
        Version = reference.Version,
        TripReference = reference.TripReference,
        Request = reference.Request is null ? null : QualificationRequestDocument.FromDomain(reference.Request)
    };

    public Domain.Qualifications.QualificationReference ToDomain() => new(
        QualificationId,
        Guest.ToDomain(),
        PropertyId,
        LoyaltyOutcome,
        QualificationReference,
        EntitlementType,
        PointsBalance,
        PointsRequired,
        ApprovalChain,
        StoredValues.ParseEnum<QualificationStatus>(Status, "status"),
        Justification,
        OperatorId,
        OperatedAt is null ? null : UtcDateTimes.FromStored(OperatedAt.Value),
        Version,
        TripReference,
        Request?.ToDomain());
}
