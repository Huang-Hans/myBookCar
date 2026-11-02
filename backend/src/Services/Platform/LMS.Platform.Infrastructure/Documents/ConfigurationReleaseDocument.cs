using LMS.Platform.Domain.Configuration;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class ConfigurationReleaseDocument
{
    [BsonId]
    public string ReleaseId { get; init; } = string.Empty;

    [BsonElement("environment")]
    public string Environment { get; init; } = string.Empty;

    [BsonElement("sequence")]
    public long Sequence { get; init; }

    [BsonElement("digest")]
    public string Digest { get; init; } = string.Empty;

    [BsonElement("approvedBy")]
    public string ApprovedBy { get; init; } = string.Empty;

    [BsonElement("effectiveAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EffectiveAt { get; init; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    public static ConfigurationReleaseDocument FromDomain(ConfigurationRelease release) => new()
    {
        ReleaseId = release.ReleaseId,
        Environment = release.Environment,
        Sequence = release.Sequence,
        Digest = release.Digest,
        ApprovedBy = release.ApprovedBy,
        EffectiveAt = UtcDateTimes.ToStored(release.EffectiveAt),
        CreatedAt = UtcDateTimes.ToStored(release.CreatedAt)
    };

    public ConfigurationRelease ToDomain() => new(
        ReleaseId,
        Environment,
        Sequence,
        Digest,
        ApprovedBy,
        UtcDateTimes.FromStored(EffectiveAt),
        UtcDateTimes.FromStored(CreatedAt));
}
