namespace LMS.Dispatch.Api.Authorization;

// 为 true 且处于 Development 时，员工接口可以不带令牌。当前开发配置保持 false，员工接口要求 Bearer 令牌。
public sealed class DevelopmentAccessOptions
{
    public const string SectionName = "DevelopmentAccess";

    public bool AllowAnonymousStaff { get; init; }
}
