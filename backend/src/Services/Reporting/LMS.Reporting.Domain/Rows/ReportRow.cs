namespace LMS.Reporting.Domain.Rows;

// 报表读取模型中的一行（report_rows）。它不是事实的创造者：数据由来源适配写入，本服务只读；
// SourceVersion 与 UpdatedAt 表达来源读取模型的版本与最后更新时间。
public sealed record ReportRow(
    string RowId,
    ReportType RowType,
    ReportRowScope Scope,
    ReportPayload Payload,
    long SourceVersion,
    DateTimeOffset UpdatedAt);
