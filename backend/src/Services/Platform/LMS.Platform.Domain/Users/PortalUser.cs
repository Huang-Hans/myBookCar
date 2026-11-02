namespace LMS.Platform.Domain.Users;

// 企业 SSO 应用用户。SubjectId 为空表示尚未与 IdP 主体关联，此时用户不能登录；员工首次以已验证企业邮箱登录时一次性关联。
// CorporateEmail 保存规范化（去空白、小写）后的值；唯一性由存储层的唯一索引保证。
public sealed record PortalUser(
    string UserId,
    string Name,
    string CorporateEmail,
    PortalRole Role,
    bool HasAllPropertyAccess,
    string? PropertyId,
    bool IsActive,
    string IdentityProvider,
    string? SubjectId,
    long Version)
{
    private const string IdPrefix = "user_";
    private const long FirstVersion = 1;

    public static PortalUser Create(
        string name,
        string corporateEmail,
        PortalRole role,
        bool hasAllPropertyAccess,
        string? propertyId,
        bool isActive,
        string identityProvider) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            name,
            NormalizeEmail(corporateEmail),
            role,
            hasAllPropertyAccess,
            propertyId,
            isActive,
            identityProvider,
            null,
            FirstVersion);

    public static string NormalizeEmail(string corporateEmail) => corporateEmail.Trim().ToLowerInvariant();

    // 关联只发生一次：已有主体的用户不得被改写。
    public PortalUser LinkSubject(string subjectId) =>
        SubjectId is null
            ? this with { SubjectId = subjectId }
            : throw new InvalidOperationException("The portal user is already linked to an identity subject.");

    public PortalUser Apply(PortalUserChanges changes) =>
        this with
        {
            Name = changes.Name.ValueOr(Name),
            CorporateEmail = NormalizeEmail(changes.CorporateEmail.ValueOr(CorporateEmail)),
            Role = changes.Role.ValueOr(Role),
            HasAllPropertyAccess = changes.HasAllPropertyAccess.ValueOr(HasAllPropertyAccess),
            PropertyId = changes.PropertyId.ValueOrCurrent(PropertyId),
            IsActive = changes.IsActive.ValueOr(IsActive)
        };
}
