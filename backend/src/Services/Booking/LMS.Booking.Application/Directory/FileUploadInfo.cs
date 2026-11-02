namespace LMS.Booking.Application.Directory;

// 一次单文件上传的授权描述，由 Platform 签发。
public sealed record FileUploadInfo(string FileId, string Url, string Method, string ContentType, DateTimeOffset ExpiresAt);
