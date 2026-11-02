namespace LMS.Reporting.Api.Contracts.Exports;

/// <summary>
/// 报表导出的筛选条件，与报表预览的查询条件一致。
/// </summary>
/// <param name="PropertyId">授权物业；不传表示当前有权的全部范围，不能借此获得全物业权限。</param>
/// <param name="From">统计区间起点（含），ISO 8601 UTC 时间并以 Z 结尾；不传表示不限起点。</param>
/// <param name="To">统计区间终点（不含），ISO 8601 UTC 时间并以 Z 结尾；不传表示不限终点；两者都传时必须晚于 From。</param>
/// <param name="ServiceType">服务类型：CROSS_BORDER / PROPERTY_TRANSFER；不传表示不区分。</param>
public sealed record ReportFilterRequest(string? PropertyId, string? From, string? To, string? ServiceType);
