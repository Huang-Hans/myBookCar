namespace LMS.Reporting.Infrastructure.ServiceIdentity;

// Reporting 后台调用 Booking / Dispatch / Platform 报表来源接口时的服务身份签名配置。
// 与各服务的 ServiceIdentity 验证配置使用同一 RSA 密钥对、issuer、audience 与 kid；私钥为空表示未配置，不签发令牌，重算按来源不可用处理。
public sealed class ServiceSigningOptions
{
    public const string SectionName = "ServiceSigning";

    public string Issuer { get; init; } = "lms-service";

    public string Audience { get; init; } = "lms-platform-internal";

    public string KeyId { get; init; } = "lms-service-1";

    public string PrivateKeyPem { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PrivateKeyPem);
}
