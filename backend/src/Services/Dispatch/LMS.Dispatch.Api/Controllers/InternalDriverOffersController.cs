using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Offers;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Offers;
using LMS.Dispatch.Application.Offers.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 服务间 / 调度来源接口：为待分配行程创建派单邀请。
/// </summary>
/// <remarks>
/// 不经网关；转发调度员（具有 drivers:write）的令牌，并按其物业范围限定。邀请来源（自动派单）待业务确认（Q03），
/// 当前由调度员显式指定司机，不使用演示触发器。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/driver-offers")]
public sealed class InternalDriverOffersController(
    ICreateDriverOfferHandler createHandler,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// 创建一轮派单邀请；需要 Idempotency-Key。
    /// </summary>
    /// <returns>新建的邀请。</returns>
    [HttpPost]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateDriverOfferRequest))]
    [RequirePermission(PermissionCodes.DriversWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateDriverOfferRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = DriverOfferMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        var serverTime = timeProvider.GetUtcNow();

        return OperationResultMapper.ToResult(
            await createHandler.HandleAsync(command.Value, cancellationToken), offer => DriverOfferMapper.ToView(offer, serverTime));
    }
}
