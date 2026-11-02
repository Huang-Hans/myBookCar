using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

internal sealed class ReportRowScopeDocument
{
    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("from")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime From { get; init; }

    [BsonElement("to")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime To { get; init; }

    [BsonElement("serviceType")]
    [BsonIgnoreIfNull]
    public string? ServiceType { get; init; }

    public static ReportRowScopeDocument Of(ReportRowScope scope) => new()
    {
        PropertyId = scope.PropertyId,
        From = UtcDateTimes.ToStored(scope.From),
        To = UtcDateTimes.ToStored(scope.To),
        ServiceType = scope.ServiceType is null ? null : WireEnum.ToWire(scope.ServiceType.Value)
    };

    public ReportRowScope ToDomain() => new(
        PropertyId,
        UtcDateTimes.FromStored(From),
        UtcDateTimes.FromStored(To),
        ServiceType is null ? null : StoredValues.ParseEnum<ServiceType>(ServiceType, "filter.serviceType"));
}
