using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Identity.Authorization;

public interface ICallerAuthorizationResolver
{
    // 返回 null 表示调用者没有可用授权：未关联应用用户、用户已停用，或授权数据不一致。
    // 授权数据源暂时不可用时抛出 CallerAuthorizationUnavailableException。
    Task<CallerAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken);
}
