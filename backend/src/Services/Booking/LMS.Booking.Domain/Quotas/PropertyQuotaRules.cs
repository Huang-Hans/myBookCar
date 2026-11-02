using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Domain.Quotas;

public static class PropertyQuotaRules
{
    // 合并后的限额：非负，且日 ≤ 周 ≤ 月。
    public static IReadOnlyList<FieldIssue> Validate(PropertyQuota quota)
    {
        var issues = new List<FieldIssue>();
        if (quota.DailyQuota < 0)
        {
            issues.Add(new FieldIssue("dailyQuota", "Must not be negative"));
        }

        if (quota.WeeklyQuota < quota.DailyQuota)
        {
            issues.Add(new FieldIssue("weeklyQuota", "Must not be lower than dailyQuota"));
        }

        if (quota.MonthlyQuota < quota.WeeklyQuota)
        {
            issues.Add(new FieldIssue("monthlyQuota", "Must not be lower than weeklyQuota"));
        }

        return issues;
    }

    // 被修改的限额不得低于权威已用量。调用前必须确认已用量已知。
    public static IReadOnlyList<FieldIssue> ValidateAgainstUsage(PropertyQuota quota, PropertyQuotaChanges changes)
    {
        var issues = new List<FieldIssue>();
        AddIfBelowUsage(issues, changes.DailyQuota.IsSet, "dailyQuota", quota.DailyQuota, quota.DailyUsed);
        AddIfBelowUsage(issues, changes.WeeklyQuota.IsSet, "weeklyQuota", quota.WeeklyQuota, quota.WeeklyUsed);
        AddIfBelowUsage(issues, changes.MonthlyQuota.IsSet, "monthlyQuota", quota.MonthlyQuota, quota.MonthlyUsed);

        return issues;
    }

    private static void AddIfBelowUsage(List<FieldIssue> issues, bool isChanged, string field, int limit, int? used)
    {
        if (isChanged && used is not null && limit < used)
        {
            issues.Add(new FieldIssue(field, "Must not be lower than the current usage"));
        }
    }
}
