using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Vehicles;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Vehicles;
using LMS.Dispatch.Application.Vehicles.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 车辆主档接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("车辆")]
[Route("api/dispatch/staff/v1/vehicles")]
public sealed class VehiclesController(
    IVehicleQueryHandler queryHandler,
    ICreateVehicleHandler createHandler) : ControllerBase
{
    /// <summary>
    /// VH01 分页查询车辆。
    /// </summary>
    /// <remarks>
    /// 搜索车牌或车型，可与物业、等级、状态同时筛选。
    /// </remarks>
    /// <returns>分页的车辆列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listVehicles")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<VehicleView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListAsync([FromQuery] VehicleListRequest request, CancellationToken cancellationToken)
    {
        var query = VehicleMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, VehicleMapper.ToView);
    }

    /// <summary>
    /// VH02 统计可用、行程中、维修中、停用的车辆数量。
    /// </summary>
    /// <remarks>
    /// 对应车辆管理顶部四张卡片。只按调用者物业范围和可选的物业参数统计。
    /// </remarks>
    /// <returns>四种状态的数量，没有车辆的状态为 0。</returns>
    [HttpGet("summary", Name = "summarizeVehicles")]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleSummaryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> SummarizeAsync([FromQuery] VehicleSummaryRequest request, CancellationToken cancellationToken)
    {
        var result = await queryHandler.SummarizeAsync(VehicleMapper.ToSummaryQuery(request, HttpContext.GetCaller().Scope), cancellationToken);

        return OperationResultMapper.ToResult(result, VehicleMapper.ToSummary);
    }

    /// <summary>
    /// VH03 查询车辆详情。
    /// </summary>
    /// <param name="vehicleId">车辆标识。</param>
    /// <returns>车辆详情；不属于调用者物业范围时按不可见处理。</returns>
    [HttpGet("{vehicleId}", Name = "getVehicle")]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetAsync(string vehicleId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(vehicleId, VehicleMapper.ToSummaryQuery(new VehicleSummaryRequest(), HttpContext.GetCaller().Scope), cancellationToken);

        return OperationResultMapper.ToResult(result, VehicleMapper.ToView);
    }

    /// <summary>
    /// VH04 新增车辆。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。车牌不区分大小写唯一。省略 status 时按可用；省略 driverId 表示未派车。
    /// </remarks>
    /// <returns>新建的车辆。</returns>
    [HttpPost(Name = "createVehicle")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateVehicleRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateVehicleRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = VehicleMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), VehicleMapper.ToView);
    }
}
