namespace LMS.Platform.Infrastructure.Authentication;

// 司机/客户令牌的签名私钥配置，只存在于签发方 Platform。发行方、受众与 kid 沿用 AppIdentity 配置，
// 因此验证方与签发方使用同一份受控值。私钥为空表示尚未配置：登录与刷新返回 503，不签发任何令牌。
public sealed class AppSigningOptions
{
    public const string SectionName = "AppSigning";

    // RSA 私钥（PEM，PKCS#8 或 PKCS#1）。必须通过环境变量、用户机密或密钥管理提供，不得提交到仓库。
    public string PrivateKeyPem { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PrivateKeyPem);
}
