namespace LMS.Platform.Api.Contracts.Files;

/// <summary>
/// 单文件上传说明，给出授权上传地址、方法与失效时间。
/// </summary>
/// <param name="Url">经授权的单文件上传地址，不包含会员标识。</param>
/// <param name="Method">上传使用的 HTTP 方法（暂定 PUT）。</param>
/// <param name="ContentType">上传内容类型，须与登记类型一致。</param>
/// <param name="ExpiresAt">上传地址的失效时间点，不是文件删除期限。</param>
public sealed record UploadDescriptionPayload(string Url, string Method, string ContentType, DateTime ExpiresAt);
