namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 审核材料上传登记的请求体：只声明文件的 JSON 元数据，文件二进制不放在 JSON 中。
/// </summary>
/// <param name="OriginalFileName">客户端选择的原始文件名，不接受服务器路径。</param>
/// <param name="ContentType">声明的 MIME 类型，允许列表待确认 Q07。</param>
/// <param name="SizeBytes">文件字节数，必须为正数；上限待确认 Q07。</param>
/// <param name="Sha256">客户端声明的 SHA-256 摘要，由服务端核实。</param>
// 文件选择后的 JSON 元数据，不是把文件二进制塞入 JSON。
public sealed record FileUploadRequest(string? OriginalFileName, string? ContentType, long? SizeBytes, string? Sha256);
