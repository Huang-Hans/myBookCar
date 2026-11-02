namespace LMS.BuildingBlocks.Identity.Options;

// 企业身份接入配置（Q01）。Authority 为空表示尚未接入真实身份：所有业务路由一律 401，不放行任何调用。
public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    // 令牌发行方（OIDC Authority）。
    public string Authority { get; init; } = string.Empty;

    // 令牌的目标受众。
    public string Audience { get; init; } = string.Empty;

    // 稳定主体标识所在的声明名。
    public string SubjectClaimType { get; init; } = "sub";

    // 本发行方在 portal_users.identityProvider 中的来源代码。
    public string ProviderCode { get; init; } = "CORPORATE_SSO";

    // 允许调用本资源的客户端；访问令牌的 azp 声明必须属于该集合。配置了 Authority 时不得为空。
    public string[] AllowedClients { get; init; } = [];

    // 企业邮箱所在的声明名。登录时用于首次关联，以及核对已关联用户的邮箱是否与 LMS 记录一致。
    public string EmailClaimType { get; init; } = "email";

    // 邮箱已验证声明名。为空表示信任该发行方的邮箱，只在企业目录统一管控邮箱且经书面确认后使用。
    public string EmailVerifiedClaimType { get; init; } = "email_verified";

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority);
}
