namespace LMS.Platform.Application.Exports.Models;

// 经当前授权签发的短效下载描述；Url 自带签名和失效时间。
public sealed record ExportDownload(string Url, string FileName, string ContentType, DateTimeOffset ExpiresAt);
