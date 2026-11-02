namespace LMS.Reporting.Application.Schedules.Models;

public static class ReportScheduleSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["recipientEmail", "frequency", "reportType", "scheduleId"];

    public const string DefaultSort = "recipientEmail,scheduleId";
    public const string StableField = "scheduleId";
}
