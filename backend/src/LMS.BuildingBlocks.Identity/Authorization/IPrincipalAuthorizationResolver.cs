using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Identity.Authorization;

public interface IPrincipalAuthorizationResolver
{
    // 返回 null 表示令牌没有关联到可用的司机/客户：未绑定、绑定已停用或尚未建立。
    // 授权数据源暂时不可用时抛出 CallerAuthorizationUnavailableException。
    Task<PrincipalAuthorization?> ResolveAsync(HttpContext context, CancellationToken cancellationToken);
}
