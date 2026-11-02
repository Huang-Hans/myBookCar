namespace LMS.Reporting.Api.Contracts.Exports;

/// <summary>
/// 申请报表文件导出。服务端记录真实申请人及其授权范围，不接受 propertyScopes。
/// </summary>
/// <param name="ReportType">报表类型，必填：DAILY_JOB_SUMMARY / TRIP_DETAIL / DRIVER_UTILIZATION / VEHICLE_UTILIZATION / CANCELLATION。</param>
/// <param name="Format">文件格式，必填：XLSX / PDF。PDF 的服务端生成尚未实现，传 PDF 返回 400 / 40001。</param>
/// <param name="Filter">导出筛选条件，必填。</param>
public sealed record CreateReportExportRequest(string? ReportType, string? Format, ReportFilterRequest? Filter);
