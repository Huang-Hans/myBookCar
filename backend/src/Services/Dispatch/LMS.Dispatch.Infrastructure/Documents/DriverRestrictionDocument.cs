using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class DriverRestrictionDocument
{
    [BsonElement("reason")]
    public string? Reason { get; init; }

    // 业务日期以 YYYY-MM-DD 字符串存储。
    [BsonElement("effectiveDate")]
    public string? EffectiveDate { get; init; }

    public static DriverRestrictionDocument FromDomain(DriverRestriction restriction) => new()
    {
        Reason = restriction.Reason,
        EffectiveDate = restriction.EffectiveDate is null ? null : StoredValues.FromDate(restriction.EffectiveDate.Value)
    };

    public DriverRestriction ToDomain() =>
        new(Reason, EffectiveDate is null ? null : StoredValues.ToDate(EffectiveDate, "restriction.effectiveDate"));
}
