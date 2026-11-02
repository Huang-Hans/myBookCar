namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 审核材料上传登记的响应体：已登记的文件标识与本次上传说明。
/// </summary>
/// <param name="FileId">已登记的文件标识。</param>
/// <param name="Upload">一次文件传输描述，含短效签名上传地址。</param>
public sealed record UploadDescriptionView(string FileId, UploadDescription Upload);
