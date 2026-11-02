using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Domain.Configuration;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class ActiveConfigurationDocument
{
    // 每个环境一条记录，_id 本身保证唯一。
    [BsonId]
    public string Environment { get; init; } = string.Empty;

    [BsonElement("releaseId")]
    public string ReleaseId { get; init; } = string.Empty;

    [BsonElement("sequence")]
    public long Sequence { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; init; }

    public static ActiveConfigurationDocument FromDomain(ActiveConfiguration active) => new()
    {
        Environment = active.Environment,
        ReleaseId = active.ReleaseId,
        Sequence = active.Sequence,
        Version = active.Version,
        UpdatedAt = UtcDateTimes.ToStored(active.UpdatedAt)
    };

    public ActiveConfiguration ToDomain() => new(Environment, ReleaseId, Sequence, Version, UtcDateTimes.FromStored(UpdatedAt));
}
