using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Users;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Users;
using LMS.Platform.Application.Users.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 应用用户接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("应用用户")]
[Route("api/platform/staff/v1/users")]
public sealed class UsersController(
    IPortalUserQueryHandler queryHandler,
    ICreatePortalUserHandler createHandler,
    IUpdatePortalUserHandler updateHandler) : ControllerBase
{
    /// <summary>
    /// US01 分页查询应用用户。
    /// </summary>
    /// <remarks>
    /// propertyId 显式选择调用者范围之外的物业时返回 403。
    /// </remarks>
    /// <returns>分页的应用用户列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listUsers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<PortalUserView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.UsersRead)]
    public async Task<IResult> ListAsync([FromQuery] PortalUserListRequest request, CancellationToken cancellationToken)
    {
        var query = PortalUserRequestMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, PortalUserViewMapper.ToView);
    }

    /// <summary>
    /// US02 查询应用用户详情。
    /// </summary>
    /// <param name="userId">应用用户标识。</param>
    /// <returns>应用用户详情；不可见时返回 404。</returns>
    [HttpGet("{userId}", Name = "getUser")]
    [ProducesResponseType(typeof(ApiEnvelope<PortalUserView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.UsersRead)]
    public async Task<IResult> GetAsync(string userId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(userId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, PortalUserViewMapper.ToView);
    }

    /// <summary>
    /// US03 新增应用用户。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// 只登记本地授权，不创建身份提供方账号：subjectId 保持未设置，关联前该用户无法登录。
    /// </remarks>
    /// <returns>新建的应用用户；相同幂等键且内容一致的重放同样返回 201，给出记录当前状态。</returns>
    [HttpPost(Name = "createUser")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreatePortalUserRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<PortalUserView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.UsersWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreatePortalUserRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = PortalUserRequestMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), PortalUserViewMapper.ToView);
    }

    /// <summary>
    /// US04 部分更新应用用户。
    /// </summary>
    /// <param name="userId">应用用户标识。</param>
    /// <returns>更新后的应用用户。</returns>
    [HttpPatch("{userId}", Name = "updateUser")]
    [ApiRequestBody(typeof(PatchPortalUserRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<PortalUserView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.UsersWrite)]
    public async Task<IResult> UpdateAsync(string userId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchPortalUserRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = PortalUserRequestMapper.ToUpdateCommand(userId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), PortalUserViewMapper.ToView);
    }
}
