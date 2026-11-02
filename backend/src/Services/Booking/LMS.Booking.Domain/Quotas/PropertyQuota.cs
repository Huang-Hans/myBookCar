namespace LMS.Booking.Domain.Quotas;

// 物业每日、每周、每月权益限额。已用量只读，由权威来源写入（Q05）；未知时为 null，绝不当作 0。
public sealed record PropertyQuota(
    string PropertyId,
    int DailyQuota,
    int WeeklyQuota,
    int MonthlyQuota,
    ExhaustionPolicy ExhaustionPolicy,
    int? DailyUsed,
    int? WeeklyUsed,
    int? MonthlyUsed,
    long Version)
{
    public bool IsUsageKnown => DailyUsed is not null && WeeklyUsed is not null && MonthlyUsed is not null;

    public PropertyQuota Apply(PropertyQuotaChanges changes) =>
        this with
        {
            DailyQuota = changes.DailyQuota.ValueOr(DailyQuota),
            WeeklyQuota = changes.WeeklyQuota.ValueOr(WeeklyQuota),
            MonthlyQuota = changes.MonthlyQuota.ValueOr(MonthlyQuota),
            ExhaustionPolicy = changes.ExhaustionPolicy.ValueOr(ExhaustionPolicy)
        };
}
