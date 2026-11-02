using LMS.BuildingBlocks.Core.Preferences;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Preferences;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

// 司机偏好：_id 即 driverId，因此每位司机至多一条，并发的首次写入由主键唯一性裁决。
internal sealed class DriverPreferencesDocument
{
    [BsonId]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; init; } = string.Empty;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static DriverPreferencesDocument FromDomain(DriverPreferences preferences) => new()
    {
        DriverId = preferences.DriverId,
        Language = WireEnum.ToWire(preferences.Language),
        UpdatedAt = preferences.UpdatedAt is null ? null : UtcDateTimes.ToStored(preferences.UpdatedAt.Value),
        Version = preferences.Version
    };

    public DriverPreferences ToDomain() => new(
        DriverId,
        StoredValues.ParseEnum<AppLanguage>(Language, "language"),
        UpdatedAt is null ? null : UtcDateTimes.FromStored(UpdatedAt.Value),
        Version);
}
