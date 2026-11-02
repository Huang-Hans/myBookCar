using LMS.Booking.Domain.Qualifications;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class QualificationRequestDocument
{
    [BsonElement("requestedBy")]
    public string RequestedBy { get; init; } = string.Empty;

    [BsonElement("requestedByName")]
    public string? RequestedByName { get; init; }

    [BsonElement("requestedByRole")]
    public string? RequestedByRole { get; init; }

    [BsonElement("reason")]
    public string? Reason { get; init; }

    [BsonElement("requestedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RequestedAt { get; init; }

    public static QualificationRequestDocument FromDomain(QualificationRequest request) => new()
    {
        RequestedBy = request.RequestedBy,
        RequestedByName = request.RequestedByName,
        RequestedByRole = request.RequestedByRole,
        Reason = request.Reason,
        RequestedAt = UtcDateTimes.ToStored(request.RequestedAt)
    };

    public QualificationRequest ToDomain() => new(
        RequestedBy, RequestedByName, RequestedByRole, Reason, UtcDateTimes.FromStored(RequestedAt));
}
