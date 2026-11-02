namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 一次文件传输描述：客户端把材料上传到授权地址所需的短效信息。
/// </summary>
/// <param name="Url">授权的单文件上传地址，不包含会员标识。</param>
/// <param name="Method">上传使用的 HTTP 方法，暂定 PUT，真实传输协议待确认 Q07。</param>
/// <param name="ContentType">该地址允许的 MIME 类型，与登记的材料类型一致。</param>
/// <param name="ExpiresAt">地址失效时间，UTC 时间点；是描述失效点，不是文件删除期限。</param>
public sealed record UploadDescription(string Url, string Method, string ContentType, DateTime ExpiresAt);
