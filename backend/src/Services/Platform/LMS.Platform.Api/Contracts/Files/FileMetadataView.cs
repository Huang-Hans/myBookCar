namespace LMS.Platform.Api.Contracts.Files;

/// <summary>
/// 文件元数据视图，映射 file_metadata；空值字段省略。
/// </summary>
/// <param name="FileId">文件标识。</param>
/// <param name="ApprovalId">文件所属的跨境审核记录标识；导出文件为空并省略。</param>
/// <param name="DocumentType">审批材料类型（四种之一）；导出文件为空并省略。</param>
/// <param name="ExportId">导出结果所属标识；非导出文件为空并省略。</param>
/// <param name="OriginalFileName">文件原名，不是服务器路径。</param>
/// <param name="ContentType">声明的 MIME 类型，允许列表待确认。</param>
/// <param name="SizeBytes">文件字节数（正数）。</param>
/// <param name="Sha256">声明的 SHA-256 内容摘要；通过安全检查前必须核实，不能把 QUARANTINED 元数据视为内容已确认。</param>
/// <param name="ScanStatus">安全扫描状态，QUARANTINED / CLEAN / INFECTED / FAILED。</param>
/// <param name="CreatedBy">文件登记人。</param>
/// <param name="CreatedAt">登记时间（UTC）。</param>
public sealed record FileMetadataView(
    string FileId,
    string? ApprovalId,
    string? DocumentType,
    string? ExportId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string ScanStatus,
    string CreatedBy,
    DateTime CreatedAt);
