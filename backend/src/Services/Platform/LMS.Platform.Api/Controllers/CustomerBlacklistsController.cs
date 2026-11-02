using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.CustomerBlacklists;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.CustomerBlacklists;
using LMS.Platform.Application.CustomerBlacklists.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 客户黑名单接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客户黑名单")]
[Route("api/platform/staff/v1/customer-blacklists")]
public sealed class CustomerBlacklistsController(
    ICustomerBlacklistQueryHandler queryHandler,
    ICreateCustomerBlacklistHandler createHandler,
    IUpdateCustomerBlacklistHandler updateHandler) : ControllerBase
{
    /// <summary>
    /// BL01 分页查询客户黑名单。
    /// </summary>
    /// <returns>分页的客户黑名单列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listCustomerBlacklists")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerBlacklistView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BlacklistsRead)]
    public async Task<IResult> ListAsync([FromQuery] CustomerBlacklistListRequest request, CancellationToken cancellationToken)
    {
        var query = CustomerBlacklistRequestMapper.ToListQuery(request);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, CustomerBlacklistViewMapper.ToView);
    }

    /// <summary>
    /// BL02 查询客户黑名单详情。
    /// </summary>
    /// <param name="blacklistId">黑名单记录标识。</param>
    /// <returns>客户黑名单详情。</returns>
    [HttpGet("{blacklistId}", Name = "getCustomerBlacklist")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerBlacklistView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BlacklistsRead)]
    public async Task<IResult> GetAsync(string blacklistId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await queryHandler.GetAsync(blacklistId, cancellationToken), CustomerBlacklistViewMapper.ToView);
    }

    /// <summary>
    /// BL03 新增客户黑名单。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>新建的黑名单记录；相同幂等键且内容一致的重放同样返回 201，给出记录当前状态。</returns>
    [HttpPost(Name = "createCustomerBlacklist")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateCustomerBlacklistRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerBlacklistView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.BlacklistsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateCustomerBlacklistRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = CustomerBlacklistRequestMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), CustomerBlacklistViewMapper.ToView);
    }

    /// <summary>
    /// BL04 部分更新客户黑名单。
    /// </summary>
    /// <param name="blacklistId">黑名单记录标识。</param>
    /// <returns>更新后的黑名单记录。</returns>
    [HttpPatch("{blacklistId}", Name = "updateCustomerBlacklist")]
    [ApiRequestBody(typeof(PatchCustomerBlacklistRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerBlacklistView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BlacklistsWrite)]
    public async Task<IResult> UpdateAsync(string blacklistId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchCustomerBlacklistRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = CustomerBlacklistRequestMapper.ToUpdateCommand(blacklistId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), CustomerBlacklistViewMapper.ToView);
    }
}
