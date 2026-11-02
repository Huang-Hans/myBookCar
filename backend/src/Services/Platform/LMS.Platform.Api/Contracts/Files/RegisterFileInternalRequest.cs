namespace LMS.Platform.Api.Contracts.Files;

// 服务间请求：Booking 在确认审核可见、材料类型合法后，为该审核的某种材料登记文件。
/// <summary>
/// 服务间登记文件请求：Booking 在确认审核可见、材料类型合法后，为该审核的某种材料登记文件。
/// </summary>
/// <param name="ApprovalId">文件所属的跨境审核记录标识，必填。</param>
/// <param name="DocumentType">材料类型，必填，取审批四种材料类型之一。</param>
/// <param name="PropertyId">文件所属物业代码，必填，用于权限与物业范围校验。</param>
/// <param name="OriginalFileName">文件原名，必填且非空白，不接受服务器路径。</param>
/// <param name="ContentType">声明的 MIME 类型，必填。</param>
/// <param name="SizeBytes">文件字节数，必填且为正数。</param>
/// <param name="Sha256">客户端声明的 SHA-256 摘要（64 位小写十六进制），由服务端核实。</param>
public sealed record RegisterFileInternalRequest(
    string? ApprovalId,
    string? DocumentType,
    string? PropertyId,
    string? OriginalFileName,
    string? ContentType,
    long? SizeBytes,
    string? Sha256);
