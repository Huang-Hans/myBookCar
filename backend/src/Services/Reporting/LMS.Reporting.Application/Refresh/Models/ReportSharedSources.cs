using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Application.Refresh.Models;

// 一次重算内各业务日共用的来源：取消原因清单与整个区间的班次，只读取一次。
public sealed record ReportSharedSources(
    IReadOnlyDictionary<string, CancellationReasonInfo> Reasons,
    IReadOnlyList<ReportShiftFact> Shifts);
