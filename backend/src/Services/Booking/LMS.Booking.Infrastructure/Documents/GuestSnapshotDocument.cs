using LMS.Booking.Domain.Qualifications;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class GuestSnapshotDocument
{
    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("tierCode")]
    public string TierCode { get; init; } = string.Empty;

    public static GuestSnapshotDocument FromDomain(GuestSnapshot guest) => new() { GuestId = guest.GuestId, Name = guest.Name, TierCode = guest.TierCode };

    public GuestSnapshot ToDomain() => new(GuestId, Name, TierCode);
}
