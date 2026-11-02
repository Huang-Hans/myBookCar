namespace LMS.Reporting.Domain.Rows;

// 一行报表数据对应的统计窗口：物业必须是实际数据范围（非空），窗口为 [From, To)，ServiceType 为空表示不区分服务类型。
public sealed record ReportRowScope(string PropertyId, DateTimeOffset From, DateTimeOffset To, ServiceType? ServiceType);
