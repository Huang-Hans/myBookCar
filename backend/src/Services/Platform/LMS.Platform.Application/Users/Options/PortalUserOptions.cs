namespace LMS.Platform.Application.Users.Options;

// 应用用户与企业身份的对应设置，与身份接入使用同一配置节（Identity）。
public sealed class PortalUserOptions
{
    public const string SectionName = "Identity";

    // 新建应用用户时写入的 IdP 来源代码（Identity:ProviderCode）。
    public string ProviderCode { get; init; } = "CORPORATE_SSO";

    // 登录时是否允许按已验证企业邮箱把未关联的主体关联到应用用户（Identity:LinkByVerifiedEmail）。
    // 关闭后只有已记录主体的用户能登录；已关联用户的邮箱核对不受此开关影响。
    public bool LinkByVerifiedEmail { get; init; } = true;
}
