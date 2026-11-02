namespace LMS.BuildingBlocks.Identity.Options;

// LMS 本地签发的司机/客户令牌（LmsAppBearer）的验证配置。与企业身份 IdentityOptions 分离：
// 只信任本地发行方、受众与受控公钥，不从令牌指定的任意地址加载密钥。公钥为空表示尚未配置：本地令牌一律拒绝。
// 私钥不放在这里，只存在于签发方 Platform 的 AppSigning 配置中。
public sealed class AppIdentityOptions
{
    public const string SectionName = "AppIdentity";

    public string Issuer { get; init; } = "lms-app";

    public string Audience { get; init; } = "lms-app-api";

    // 签发方写入令牌头 kid 的密钥标识，与公钥对应。
    public string KeyId { get; init; } = "lms-app-1";

    // RSA 公钥（PEM，SubjectPublicKeyInfo 或 RSAPublicKey）。
    public string PublicKeyPem { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKeyPem);
}
