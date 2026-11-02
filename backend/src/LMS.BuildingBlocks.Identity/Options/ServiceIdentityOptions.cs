namespace LMS.BuildingBlocks.Identity.Options;

// 受限服务身份（ServiceBearer）的验证配置，仅被提供内部接口的服务使用（Platform、Dispatch）。与 LmsAppBearer 完全分离：
// 不同的发行方、受众与公钥，因此司机/客户令牌和 Platform 自己的服务令牌都不会被它接受。
// An empty public key rejects service tokens. Each service stores the matching private/public PEM in its ServiceSigning/ServiceIdentity settings.
public sealed class ServiceIdentityOptions
{
    public const string SectionName = "ServiceIdentity";

    public string Issuer { get; init; } = "lms-service";

    public string Audience { get; init; } = "lms-platform-internal";

    public string KeyId { get; init; } = "lms-service-1";

    public string PublicKeyPem { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKeyPem);
}
