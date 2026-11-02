using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Users;
using LMS.Platform.Api.Mapping.Users;
using LMS.Platform.Application.Users.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 当前用户接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("当前用户")]
[Route("api/platform/staff/v1/current-user")]
public sealed class CurrentUserController(ICurrentUserHandler handler) : ControllerBase
{
    /// <summary>
    /// 读取当前登录用户及其生效权限。
    /// </summary>
    /// <remarks>
    /// 只回答当前令牌所属调用者自己的授权，不接受任何用户标识参数；不要求特定权限，但令牌必须能对应到启用的应用用户。
    /// </remarks>
    /// <returns>当前用户档案、角色、物业范围与权限清单。</returns>
    [HttpGet(Name = "getCurrentUser")]
    [ProducesResponseType(typeof(ApiEnvelope<CurrentUserView>), StatusCodes.Status200OK)]
    [RequireCaller]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var caller = HttpContext.GetCaller();
        var result = await handler.GetAsync(caller.UserId, cancellationToken);

        return OperationResultMapper.ToResult(result, user => CurrentUserViewMapper.ToView(user, caller));
    }
}
