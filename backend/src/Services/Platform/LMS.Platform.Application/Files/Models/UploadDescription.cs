namespace LMS.Platform.Application.Files.Models;

// 一次单文件上传的授权描述。URL 自带签名和失效时间，不包含会员标识。
public sealed record UploadDescription(string Url, string Method, string ContentType, DateTimeOffset ExpiresAt);
