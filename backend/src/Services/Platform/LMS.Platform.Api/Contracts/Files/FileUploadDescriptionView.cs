namespace LMS.Platform.Api.Contracts.Files;

/// <summary>
/// 文件登记结果视图，返回已登记文件标识与一次性上传说明。
/// </summary>
/// <param name="FileId">已登记的文件标识。</param>
/// <param name="Upload">该文件的传输描述，含签名上传地址与失效时间。</param>
public sealed record FileUploadDescriptionView(string FileId, UploadDescriptionPayload Upload);
