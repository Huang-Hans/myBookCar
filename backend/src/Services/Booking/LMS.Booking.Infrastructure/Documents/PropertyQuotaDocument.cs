using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Quotas;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class PropertyQuotaDocument
{
    // _id 即上传配置的物业代码，每物业一条。
    [BsonId]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("dailyQuota")]
    public int DailyQuota { get; init; }

    [BsonElement("weeklyQuota")]
    public int WeeklyQuota { get; init; }

    [BsonElement("monthlyQuota")]
    public int MonthlyQuota { get; init; }

    [BsonElement("exhaustionPolicy")]
    public string ExhaustionPolicy { get; init; } = string.Empty;

    // 已用量只读，由权威来源写入；未知时为 null。
    [BsonElement("dailyUsed")]
    public int? DailyUsed { get; init; }

    [BsonElement("weeklyUsed")]
    public int? WeeklyUsed { get; init; }

    [BsonElement("monthlyUsed")]
    public int? MonthlyUsed { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public PropertyQuota ToDomain() => new(
        PropertyId,
        DailyQuota,
        WeeklyQuota,
        MonthlyQuota,
        StoredValues.ParseEnum<ExhaustionPolicy>(ExhaustionPolicy, "exhaustionPolicy"),
        DailyUsed,
        WeeklyUsed,
        MonthlyUsed,
        Version);

    public static string PolicyToStored(ExhaustionPolicy policy) => WireEnum.ToWire(policy);
}
