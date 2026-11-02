namespace LMS.Platform.Application.Files.Commands;

// 下载内容。ExpiresAtUnixSeconds 和 Signature 来自下载 URL。
public sealed record DownloadFileContentCommand(string FileId, long ExpiresAtUnixSeconds, string Signature);
