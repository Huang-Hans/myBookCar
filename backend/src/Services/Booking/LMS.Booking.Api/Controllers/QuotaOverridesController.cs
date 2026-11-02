using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Overrides;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Overrides;
using LMS.Booking.Application.Overrides.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 限额例外申请与决定接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("限额例外")]
[Route("api/booking/staff/v1/quota-overrides")]
public sealed class QuotaOverridesController(
    IQuotaOverrideQueryHandler queryHandler,
    ICreateQuotaOverrideHandler createHandler,
    IDecideQuotaOverrideHandler decideHandler) : ControllerBase
{
    /// <summary>
    /// OV01 分页查询限额例外。
    /// </summary>
    /// <returns>分页的限额例外列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listQuotaOverrides")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<QuotaOverrideView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.OverridesRead)]
    public async Task<IResult> ListAsync([FromQuery] QuotaOverrideListRequest request, CancellationToken cancellationToken)
    {
        var query = OverrideMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, OverrideMapper.ToView);
    }

    /// <summary>
    /// OV02 提交限额例外申请。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>新建的限额例外；相同幂等键且内容一致的重放同样返回 201，给出记录当前状态。</returns>
    [HttpPost(Name = "createQuotaOverride")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateQuotaOverrideRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QuotaOverrideView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.OverridesRequest)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateQuotaOverrideRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = OverrideMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), OverrideMapper.ToView);
    }

    /// <summary>
    /// OV03 批准或拒绝限额例外。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <param name="overrideId">限额例外标识。</param>
    /// <returns>决定后的限额例外；已决定的记录再次决定返回 409 / 40900。</returns>
    [HttpPost("{overrideId}/decisions", Name = "decideQuotaOverride")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(QuotaOverrideDecisionRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QuotaOverrideView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.OverridesDecide)]
    public async Task<IResult> DecideAsync(string overrideId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<QuotaOverrideDecisionRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, version, key) is { } failure)
        {
            return failure;
        }

        var command = OverrideMapper.ToDecideCommand(overrideId, version.Value, body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await decideHandler.HandleAsync(command.Value, cancellationToken), OverrideMapper.ToView);
    }
}
