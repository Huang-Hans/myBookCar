using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Domain.Quotas;

public sealed record PropertyQuotaChanges(
    Optional<int> DailyQuota,
    Optional<int> WeeklyQuota,
    Optional<int> MonthlyQuota,
    Optional<ExhaustionPolicy> ExhaustionPolicy)
{
    public bool ChangesLimits => DailyQuota.IsSet || WeeklyQuota.IsSet || MonthlyQuota.IsSet;

    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("dailyQuota", DailyQuota.IsSet),
            ("weeklyQuota", WeeklyQuota.IsSet),
            ("monthlyQuota", MonthlyQuota.IsSet),
            ("exhaustionPolicy", ExhaustionPolicy.IsSet));
}
