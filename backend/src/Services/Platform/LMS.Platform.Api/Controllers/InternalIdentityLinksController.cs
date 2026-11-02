using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Http.Tracing;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Internal;
using LMS.Platform.Application.Authorization.Handlers;
using LMS.Platform.Application.Authorization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间接口：网关员工登录时核对令牌主体在 LMS 中的应用用户，并在首次登录时建立关联。
/// </summary>
/// <remarks>
/// 不经网关路由；调用方为网关登录接口，转发刚由 Keycloak 签发的员工访问令牌（StaffBearer）。
/// 主体尚未关联时不能使用 RequireCaller，因此在此直接读取已验证令牌。司机与客户不经 Keycloak 登录，也不在这里关联。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/identity-links")]
public sealed class InternalIdentityLinksController(ILinkCallerIdentityHandler handler) : ControllerBase
{
    /// <summary>
    /// 核对并关联当前令牌主体：已关联，或本次按已验证企业邮箱关联成功且用户可用时放行，否则为 403。
    /// </summary>
    /// <returns>调用者的用户、角色、物业范围与权限清单。</returns>
    [HttpPost]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var identity = CallerIdentityReader.Read(HttpContext);
        if (identity is null)
        {
            return ApiResults.Fail(ApiErrors.InsufficientPermission);
        }

        var staff = await handler.HandleAsync(ToLookup(identity), cancellationToken);

        return staff is null
            ? ApiResults.Fail(ApiErrors.InsufficientPermission)
            : ApiResults.Ok(new CallerAuthorizationView(
                staff.UserId, staff.RoleCode, staff.HasAllPropertyAccess, staff.PropertyId, staff.Permissions.ToList()));
    }

    private CallerLookup ToLookup(CallerIdentity identity) =>
        new(identity.ProviderCode, identity.SubjectId, identity.VerifiedEmail, HttpContext.GetTraceId());
}
