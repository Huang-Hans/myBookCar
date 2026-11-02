namespace LMS.Reporting.Application.Refresh.Options;

// 报表行定时重算的设置。窗口为澳门业务日 [今天 − DaysBack, 今天 + DaysAhead]；
// BackfillFrom / BackfillTo 同时给出时，在服务启动后先重算一次该区间（用于首次上线与修正历史数据），再进入定时重算。
public sealed class ReportRefreshOptions
{
    public const string SectionName = "Reporting:Refresh";

    public bool Enabled { get; init; }

    public int IntervalMinutes { get; init; } = 10;

    public int DaysBack { get; init; } = 6;

    public int DaysAhead { get; init; } = 1;

    public int LockLeaseMinutes { get; init; } = 5;

    // 计为“未到场”的取消原因代码（Platform 取消原因清单），默认 CR-001 Customer no-show。
    public string[] NoShowReasonCodes { get; init; } = ["CR-001"];

    public DateOnly? BackfillFrom { get; init; }

    public DateOnly? BackfillTo { get; init; }
}
