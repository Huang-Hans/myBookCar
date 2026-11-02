using LMS.BuildingBlocks.Core.Preferences;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 客户偏好：_id 即 guestId，每位客户至多一条，并发的首次写入由主键唯一性裁决。
internal sealed class CustomerPreferencesDocument
{
    [BsonId]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; init; } = string.Empty;

    [BsonElement("isPushNotificationsEnabled")]
    public bool IsPushNotificationsEnabled { get; init; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CustomerPreferencesDocument FromDomain(CustomerPreferences preferences) => new()
    {
        GuestId = preferences.GuestId,
        Language = WireEnum.ToWire(preferences.Language),
        IsPushNotificationsEnabled = preferences.IsPushNotificationsEnabled,
        UpdatedAt = preferences.UpdatedAt is null ? null : UtcDateTimes.ToStored(preferences.UpdatedAt.Value),
        Version = preferences.Version
    };

    public CustomerPreferences ToDomain() => new(
        GuestId,
        StoredValues.ParseEnum<AppLanguage>(Language, "language"),
        IsPushNotificationsEnabled,
        UpdatedAt is null ? null : UtcDateTimes.FromStored(UpdatedAt.Value),
        Version);
}
