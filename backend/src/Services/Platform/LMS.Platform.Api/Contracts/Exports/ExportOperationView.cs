namespace LMS.Platform.Api.Contracts.Exports;

/// <summary>
/// 导出作业视图，映射 report_exports；空值字段省略。
/// </summary>
/// <param name="OperationId">导出标识，API operationId 与 exportId 是同一个标识。</param>
/// <param name="ExportKind">导出类别，审计导出固定为 AUDIT_LOG。</param>
/// <param name="ReportType">报表类型；审计导出没有，省略。</param>
/// <param name="Format">文件格式，审计导出固定为 XLSX。</param>
/// <param name="RequestedAt">受理时间（UTC）。</param>
/// <param name="State">技术作业状态：QUEUED / RUNNING / SUCCEEDED / FAILED。202 只表示受理，SUCCEEDED 才表示结果文件已生成。</param>
/// <param name="ResultFileId">结果文件标识；完成后出现。</param>
/// <param name="FailureMessage">失败时的安全英文说明。</param>
/// <param name="ExpiresAt">结果失效时间（UTC）；完成后出现，过期后不能再下载。</param>
public sealed record ExportOperationView(
    string OperationId,
    string ExportKind,
    string? ReportType,
    string Format,
    DateTime RequestedAt,
    string State,
    string? ResultFileId,
    string? FailureMessage,
    DateTime? ExpiresAt);
