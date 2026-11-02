using LMS.Platform.Application.Authorization.Models;
using LMS.Platform.Application.Authorization.Services;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Application.Users.Services;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Authorization.Handlers;

// 登录时核对 IdP 与 LMS 两侧身份：已关联的用户，令牌中已验证的企业邮箱还必须与 LMS 记录一致；
// 尚未关联的主体按已验证企业邮箱一次性关联。任一核对不成立都按无授权处理。
internal sealed class LinkCallerIdentityHandler(
    IPortalUserRepository users,
    PortalUserIdentityLinker linker,
    CallerAccessFactory accessFactory) : ILinkCallerIdentityHandler
{
    public async Task<CallerAccess?> HandleAsync(CallerLookup lookup, CancellationToken cancellationToken)
    {
        var linkedUser = await users.FindBySubjectAsync(lookup.IdentityProvider, lookup.SubjectId, cancellationToken);
        var user = linkedUser is null
            ? await linker.TryLinkAsync(lookup, cancellationToken)
            : WithMatchingEmail(linkedUser, lookup.VerifiedEmail);

        return await accessFactory.CreateAsync(user, cancellationToken);
    }

    // 令牌未带已验证邮箱，或邮箱与 LMS 记录不一致（任一侧邮箱已变更）时返回 null，需管理员核实后处理。
    private static PortalUser? WithMatchingEmail(PortalUser user, string? verifiedEmail) =>
        verifiedEmail is not null
        && string.Equals(PortalUser.NormalizeEmail(verifiedEmail), user.CorporateEmail, StringComparison.Ordinal)
            ? user
            : null;
}
