using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Exports;

// 与报表预览相同的筛选：PropertyId 为空表示申请人当前有权的全部范围。From 含，To 不含，两者都可不传（不限）。
public sealed record ReportExportFilter(string? PropertyId, DateTimeOffset? From, DateTimeOffset? To, ServiceType? ServiceType);
