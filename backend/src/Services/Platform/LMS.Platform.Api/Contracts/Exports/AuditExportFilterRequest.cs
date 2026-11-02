namespace LMS.Platform.Api.Contracts.Exports;

/// <summary>
/// 审计导出的筛选条件，与审计列表查询一致。
/// </summary>
/// <param name="PropertyId">授权物业；不传表示当前有权的全部范围，不能借此获得全物业权限。</param>
/// <param name="From">起始时间（含），ISO 8601 UTC 时间并以 Z 结尾；不传表示不限起点。</param>
/// <param name="To">结束时间（不含），ISO 8601 UTC 时间并以 Z 结尾，不传表示不限（上限始终是申请时刻）；两者都传时必须晚于 From。</param>
/// <param name="Search">审计文本筛选；不传或空白表示不筛选。</param>
public sealed record AuditExportFilterRequest(string? PropertyId, string? From, string? To, string? Search);
