namespace LMS.Platform.Api.Contracts.Exports;

/// <summary>
/// 申请审计 Excel 导出。格式固定为 XLSX，不提供格式选择。
/// </summary>
/// <param name="Filter">导出筛选条件，必填。</param>
public sealed record CreateAuditExportRequest(AuditExportFilterRequest? Filter);
