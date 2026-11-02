using LMS.Platform.Application.Authorization.Models;
using LMS.Platform.Application.Authorization.Services;
using LMS.Platform.Application.Users.Ports;

namespace LMS.Platform.Application.Authorization.Handlers;

// 每个请求只按已记录的主体解析；主体与应用用户的关联只在登录时建立（见 LinkCallerIdentityHandler）。
internal sealed class ResolveCallerHandler(IPortalUserRepository users, CallerAccessFactory accessFactory) : IResolveCallerHandler
{
    public async Task<CallerAccess?> HandleAsync(string identityProvider, string subjectId, CancellationToken cancellationToken) =>
        await accessFactory.CreateAsync(await users.FindBySubjectAsync(identityProvider, subjectId, cancellationToken), cancellationToken);
}
