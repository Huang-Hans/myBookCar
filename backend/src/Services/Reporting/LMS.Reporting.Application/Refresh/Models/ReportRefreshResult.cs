namespace LMS.Reporting.Application.Refresh.Models;

// 一次重算的结果：成功替换的业务日数、因来源不可用而跳过的业务日数、因其他实例正在重算而跳过的业务日数；
// UnavailableSource 是第一个不可用的来源（booking / dispatch / platform），全部可用时为 null。
public sealed record ReportRefreshResult(int RefreshedDays, int UnavailableDays, int LockedDays, string? UnavailableSource = null);
