using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Qualifications;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class QualificationAuditContextDocument
{
    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("justification")]
    public string Justification { get; init; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    public static QualificationAuditContextDocument FromDomain(QualificationAuditContext context) => new()
    {
        GuestId = context.GuestId,
        Justification = context.Justification,
        Status = WireEnum.ToWire(context.Status)
    };

    public QualificationAuditContext ToDomain() =>
        new(GuestId, Justification, StoredValues.ParseEnum<QualificationStatus>(Status, "qualification.status"));
}
