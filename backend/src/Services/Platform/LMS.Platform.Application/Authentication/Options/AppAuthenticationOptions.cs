namespace LMS.Platform.Application.Authentication.Options;

// 司机/客户本地认证的默认时长与失败限制。Access Token 15 分钟，Refresh Token 自登录起 7 天绝对有效。
public sealed class AppAuthenticationOptions
{
    public const string SectionName = "AppAuthentication";

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    // 同一登录标识在 FailureWindowMinutes 内连续失败达到该次数后暂时拒绝。
    public int MaxFailedLogins { get; init; } = 5;

    public int FailureWindowMinutes { get; init; } = 15;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
}
