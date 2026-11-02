namespace LMS.Platform.Domain.Exports;

// 审计导出的筛选条件，与审计列表查询一致。From 含，To 不含，两者都可不传（不限）；PropertyId 为 null 表示申请人当前有权的全部范围。
public sealed record AuditExportFilter(string? PropertyId, DateTimeOffset? From, DateTimeOffset? To, string? Search);
