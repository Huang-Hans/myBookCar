using LMS.Platform.Application.Authorization.Models;

namespace LMS.Platform.Application.Authorization.Handlers;

public interface IResolveCallerHandler
{
    // 返回 null 表示该主体没有可用授权：未关联应用用户、用户已停用或授权数据不一致。
    Task<CallerAccess?> HandleAsync(string identityProvider, string subjectId, CancellationToken cancellationToken);
}
