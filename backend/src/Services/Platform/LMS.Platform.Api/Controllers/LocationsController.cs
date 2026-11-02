using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Locations;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Locations;
using LMS.Platform.Application.Locations.Handlers;
using LMS.Platform.Domain.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 地点主档与展示顺序接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("地点")]
[Route("api/platform/staff/v1/locations")]
public sealed class LocationsController(
    ILocationQueryHandler queryHandler,
    ICreateLocationHandler createHandler,
    IUpdateLocationHandler updateHandler,
    IReplaceLocationOrderHandler replaceOrderHandler) : ControllerBase
{
    /// <summary>
    /// LC01 分页查询地点。
    /// </summary>
    /// <returns>分页的地点列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listLocations")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<LocationView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.LocationsRead)]
    public async Task<IResult> ListAsync([FromQuery] LocationListRequest request, CancellationToken cancellationToken)
    {
        var query = LocationRequestMapper.ToListQuery(request);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, LocationViewMapper.ToView);
    }

    /// <summary>
    /// LC02 查询地点详情。
    /// </summary>
    /// <param name="locationId">地点标识。</param>
    /// <returns>地点详情。</returns>
    [HttpGet("{locationId}", Name = "getLocation")]
    [ProducesResponseType(typeof(ApiEnvelope<LocationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.LocationsRead)]
    public async Task<IResult> GetAsync(string locationId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await queryHandler.GetAsync(locationId, cancellationToken), LocationViewMapper.ToView);
    }

    /// <summary>
    /// LC03 新增地点。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>新建的地点；相同幂等键且内容一致的重放同样返回 201，给出记录当前状态。</returns>
    [HttpPost(Name = "createLocation")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateLocationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LocationView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.LocationsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateLocationRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = LocationRequestMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), LocationViewMapper.ToView);
    }

    /// <summary>
    /// LC04 部分更新地点。
    /// </summary>
    /// <param name="locationId">地点标识。</param>
    /// <returns>更新后的地点。</returns>
    [HttpPatch("{locationId}", Name = "updateLocation")]
    [ApiRequestBody(typeof(PatchLocationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LocationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.LocationsWrite)]
    public async Task<IResult> UpdateAsync(string locationId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchLocationRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = LocationRequestMapper.ToUpdateCommand(locationId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), LocationViewMapper.ToView);
    }

    /// <summary>
    /// LC05 整体替换某区域的地点展示顺序。
    /// </summary>
    /// <param name="region">区域枚举的对外字面值，例如 MACAU_PENINSULA。</param>
    /// <returns>替换后的展示顺序。</returns>
    [HttpPut("~/api/platform/staff/v1/location-orderings/{region}", Name = "replaceLocationOrdering")]
    [ApiRequestBody(typeof(LocationOrderingRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LocationOrderingView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.LocationsWrite)]
    public async Task<IResult> ReplaceOrderAsync(string region, CancellationToken cancellationToken)
    {
        if (!WireEnum.TryParse<LocationRegion>(region, out var parsedRegion))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("region", "Must be a valid region code")]);
        }

        var body = await JsonRequestBody.ReadAsync<LocationOrderingRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var command = LocationRequestMapper.ToOrderCommand(parsedRegion, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(
            await replaceOrderHandler.HandleAsync(command.Value, cancellationToken), result => LocationViewMapper.ToView(result));
    }
}
