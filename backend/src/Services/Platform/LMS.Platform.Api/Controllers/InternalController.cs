using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Internal;
using LMS.Platform.Application.Configuration.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间读取接口：让其他服务解析调用者授权并核对物业代码，而不直接读取 Platform 的库。
/// </summary>
/// <remarks>
/// 调用方为 Dispatch 等服务，转发终端用户的 Bearer 令牌；
/// 这些接口不经网关路由，也只回答该令牌所属调用者自己的授权。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalController(IPropertyCatalog propertyCatalog) : ControllerBase
{
    /// <summary>
    /// 返回当前令牌所属调用者的授权。
    /// </summary>
    /// <returns>调用者的用户、角色、物业范围与权限清单。</returns>
    [HttpGet("caller-authorization")]
    [RequireCaller]
    public IResult GetCallerAuthorization()
    {
        var caller = HttpContext.GetCaller();

        return ApiResults.Ok(new CallerAuthorizationView(
            caller.UserId, caller.RoleCode, caller.HasAllPropertyAccess, caller.PropertyId, caller.Permissions.ToList(), caller.Name));
    }

    /// <summary>
    /// 核对物业代码：物业存在返回 204；不存在为 404；配置不可用为 503。
    /// </summary>
    /// <param name="propertyId">待核对的物业代码。</param>
    /// <returns>物业存在时无响应体，只返回 204。</returns>
    [HttpGet("properties/{propertyId}")]
    [RequireCaller]
    public async Task<IResult> CheckPropertyAsync(string propertyId, CancellationToken cancellationToken) =>
        await propertyCatalog.ContainsAsync(propertyId, cancellationToken) switch
        {
            CatalogLookup.Found => Results.NoContent(),
            CatalogLookup.NotFound => ApiResults.Fail(ApiErrors.ResourceNotFound),
            _ => ApiResults.Fail(ApiErrors.ServiceUnavailable)
        };
}
