namespace LMS.Booking.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：Platform 登录时核对客户凭据的请求体。仅限 Platform 服务身份调用，明文密码只用于本次校验。
/// </summary>
/// <param name="Phone">手机号（可带国家/地区码，如 +85361234567，也可为本地号码；服务端按统一规范规范化后精确匹配）。</param>
/// <param name="Password">待校验的明文密码。</param>
public sealed record CustomerCredentialVerificationRequest(string? Phone, string? Password);
