using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 客户本地补充资料：_id 即 guestId，每位客户至多一条；与 guests 快照分集合保存，快照导入不会触碰它。
internal sealed class CustomerAccountProfileDocument
{
    [BsonId]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string? Name { get; init; }

    [BsonElement("email")]
    public string? Email { get; init; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CustomerAccountProfileDocument FromDomain(CustomerAccountProfile profile) => new()
    {
        GuestId = profile.GuestId,
        Name = profile.Name,
        Email = profile.Email,
        UpdatedAt = profile.UpdatedAt is null ? null : UtcDateTimes.ToStored(profile.UpdatedAt.Value),
        Version = profile.Version
    };

    public CustomerAccountProfile ToDomain() =>
        new(GuestId, Name, Email, UpdatedAt is null ? null : UtcDateTimes.FromStored(UpdatedAt.Value), Version);
}
