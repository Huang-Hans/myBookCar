namespace LMS.Platform.Application.Files.Commands;

// 上传内容。ExpiresAtUnixSeconds 和 Signature 来自上传 URL；ContentType 是请求头里的内容类型。
public sealed record UploadFileContentCommand(
    string FileId,
    long ExpiresAtUnixSeconds,
    string Signature,
    string? ContentType,
    Stream Content);
