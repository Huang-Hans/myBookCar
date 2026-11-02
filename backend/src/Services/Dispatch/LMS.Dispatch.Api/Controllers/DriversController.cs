using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Drivers;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Drivers;
using LMS.Dispatch.Application.Drivers.Handlers;
using LMS.Dispatch.Application.Drivers.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机主档接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("司机")]
[Route("api/dispatch/staff/v1/drivers")]
public sealed class DriversController(
    IDriverQueryHandler queryHandler,
    ICreateDriverHandler createHandler,
    IUpdateDriverHandler updateHandler,
    IRemoveDriverHandler removeHandler) : ControllerBase
{
    /// <summary>
    /// DR01 分页查询司机。
    /// </summary>
    /// <remarks>
    /// 页面标签与其他筛选取交集，黑名单视图包含停用与暂停的司机。
    /// </remarks>
    /// <returns>分页的司机列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listDrivers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DriversRead)]
    public async Task<IResult> ListAsync([FromQuery] DriverListRequest request, CancellationToken cancellationToken)
    {
        var query = DriverRequestMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, DriverViewMapper.ToView);
    }

    /// <summary>
    /// DR02 查询司机详情。
    /// </summary>
    /// <param name="driverId">司机标识。</param>
    /// <returns>司机详情；不属于调用者物业范围时按不可见处理。</returns>
    [HttpGet("{driverId}", Name = "getDriver")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DriversRead)]
    public async Task<IResult> GetAsync(string driverId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(driverId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverViewMapper.ToView);
    }

    /// <summary>
    /// DR03 新增司机。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>新建的司机；availability、availabilitySource 与 syncedAt 只读，在接入可信同步来源前保持未设置。</returns>
    [HttpPost(Name = "createDriver")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateDriverRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DriversWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateDriverRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = DriverRequestMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), DriverViewMapper.ToView);
    }

    /// <summary>
    /// DR04 部分更新司机。
    /// </summary>
    /// <param name="driverId">司机标识。</param>
    /// <returns>更新后的司机。</returns>
    [HttpPatch("{driverId}", Name = "updateDriver")]
    [ApiRequestBody(typeof(PatchDriverRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DriversWrite)]
    public async Task<IResult> UpdateAsync(string driverId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchDriverRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = DriverRequestMapper.ToUpdateCommand(driverId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), DriverViewMapper.ToView);
    }

    /// <summary>Soft-delete a driver while retaining historical references and audit records.</summary>
    [HttpDelete("{driverId}", Name = "removeDriver")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DriversWrite)]
    public async Task<IResult> RemoveAsync(string driverId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure) return failure;
        var result = await removeHandler.HandleAsync(new RemoveDriverCommand(driverId, CallerContexts.From(HttpContext)), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok<object?>(null) : OperationResultMapper.ToFailure(result);
    }
}
