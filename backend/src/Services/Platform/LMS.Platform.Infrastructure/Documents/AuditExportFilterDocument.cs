using LMS.Platform.Domain.Exports;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class AuditExportFilterDocument
{
    [BsonElement("propertyId")]
    [BsonIgnoreIfNull]
    public string? PropertyId { get; init; }

    [BsonElement("from")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? From { get; init; }

    [BsonElement("to")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? To { get; init; }

    [BsonElement("search")]
    [BsonIgnoreIfNull]
    public string? Search { get; init; }

    public static AuditExportFilterDocument FromDomain(AuditExportFilter filter) => new()
    {
        PropertyId = filter.PropertyId,
        From = filter.From is null ? null : UtcDateTimes.ToStored(filter.From.Value),
        To = filter.To is null ? null : UtcDateTimes.ToStored(filter.To.Value),
        Search = filter.Search
    };

    public AuditExportFilter ToDomain() => new(PropertyId, From is null ? null : UtcDateTimes.FromStored(From.Value), To is null ? null : UtcDateTimes.FromStored(To.Value), Search);
}
