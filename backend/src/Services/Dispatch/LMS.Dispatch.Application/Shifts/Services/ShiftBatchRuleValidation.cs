using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Services;

public static class ShiftBatchRuleValidation
{
    public const int MaxDays = 31;
    public const int MaxItems = 1000;

    public static IReadOnlyList<FieldIssue> Validate(ShiftBatchRules rules)
    {
        var issues = new List<FieldIssue>();
        ValidateSelections(rules, issues);
        var span = rules.To.DayNumber - rules.From.DayNumber + 1;
        if (span is < 1 or > MaxDays)
            issues.Add(new("to", "The date range must contain between 1 and 31 calendar days"));
        if (issues.Count > 0) return issues;
        ValidateSize(rules, issues);
        return issues;
    }

    private static void ValidateSelections(ShiftBatchRules rules, List<FieldIssue> issues)
    {
        if (rules.DriverIds.Count is < 1 or > MaxItems)
            issues.Add(new("driverIds", "Select between 1 and 1000 drivers"));
        ValidateWeekdays(rules.Weekdays, issues);
        ValidateUniqueness(rules, issues);
    }

    private static void ValidateWeekdays(IReadOnlyList<int> weekdays, List<FieldIssue> issues)
    {
        if (weekdays.Count is < 1 or > 7)
            issues.Add(new("weekdays", "Select between 1 and 7 weekdays"));
        if (weekdays.Any(day => day is < 1 or > 7))
            issues.Add(new("weekdays", "Weekdays must be between 1 (Monday) and 7 (Sunday)"));
    }

    private static void ValidateUniqueness(ShiftBatchRules rules, List<FieldIssue> issues)
    {
        if (rules.DriverIds.Distinct(StringComparer.Ordinal).Count() != rules.DriverIds.Count)
            issues.Add(new("driverIds", "Driver IDs must be unique"));
        if (rules.Weekdays.Distinct().Count() != rules.Weekdays.Count)
            issues.Add(new("weekdays", "Weekdays must be unique"));
    }

    private static void ValidateSize(ShiftBatchRules rules, List<FieldIssue> issues)
    {
        var dateCount = rules.Dates().Count;
        if (dateCount == 0) issues.Add(new("weekdays", "The selected weekdays do not match any date in the range"));
        if ((long)dateCount * rules.DriverIds.Count > MaxItems)
            issues.Add(new("driverIds", "A batch must not contain more than 1000 driver-date combinations"));
    }
}
