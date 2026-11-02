namespace LMS.Reporting.Application.Refresh.Models;

// 单个业务日的重算结果。
public enum ReportDayOutcome
{
    Refreshed,
    SourceUnavailable,
    Locked
}
