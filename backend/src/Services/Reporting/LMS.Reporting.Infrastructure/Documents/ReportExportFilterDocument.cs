using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

internal sealed class ReportExportFilterDocument
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

    [BsonElement("serviceType")]
    [BsonIgnoreIfNull]
    public string? ServiceType { get; init; }

    public static ReportExportFilterDocument FromDomain(ReportExportFilter filter) => new()
    {
        PropertyId = filter.PropertyId,
        From = filter.From is null ? null : UtcDateTimes.ToStored(filter.From.Value),
        To = filter.To is null ? null : UtcDateTimes.ToStored(filter.To.Value),
        ServiceType = filter.ServiceType is null ? null : WireEnum.ToWire(filter.ServiceType.Value)
    };

    public ReportExportFilter ToDomain() => new(
        PropertyId,
        From is null ? null : UtcDateTimes.FromStored(From.Value),
        To is null ? null : UtcDateTimes.FromStored(To.Value),
        ServiceType is null ? null : StoredValues.ParseEnum<ServiceType>(ServiceType, "filter.serviceType"));
}
