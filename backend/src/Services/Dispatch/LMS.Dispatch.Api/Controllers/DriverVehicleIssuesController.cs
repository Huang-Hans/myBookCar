using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Reports;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Reports;
using LMS.Dispatch.Application.Reports.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机车辆问题报告接口。司机标识只来自令牌；他人的记录与不存在不作区分，一律 404。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机车辆问题")]
[Route("api/dispatch/driver/v1/me/vehicle-issues")]
public sealed class DriverVehicleIssuesController(
    ICreateVehicleIssueHandler createHandler,
    IVehicleIssueQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// D2-05 登记车辆问题。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；同键同内容重放返回同一份记录（仍为 201）。只登记真实记录并返回 RECORDED，
    /// 不表示已受理或已维修，也不会自动改司机状态或取消行程。可选 inspectionId 必须是本人的检查记录。
    /// </remarks>
    /// <returns>新建的问题报告。</returns>
    [HttpPost(Name = "createVehicleIssue")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateVehicleIssueRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleIssueView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateVehicleIssueRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ReportMapper.ToIssueCommand(HttpContext.GetPrincipal().EntityId, body.Value, key.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), ReportMapper.ToView);
    }

    /// <summary>
    /// D2-06 分页查询本人车辆问题报告。
    /// </summary>
    /// <returns>分页的问题报告；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listVehicleIssues")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<VehicleIssueView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsRead)]
    public async Task<IResult> ListAsync([FromQuery] VehicleIssueListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var query = ReportMapper.ToListQuery(request, HttpContext.GetPrincipal().EntityId);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, ReportMapper.ToView);
    }

    /// <summary>
    /// D2-07 查询本人车辆问题详情。
    /// </summary>
    /// <param name="issueId">问题标识。</param>
    /// <returns>问题详情与真实处理状态。</returns>
    [HttpGet("{issueId}", Name = "getVehicleIssue")]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleIssueView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsRead)]
    public async Task<IResult> GetAsync(string issueId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(issueId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, ReportMapper.ToView);
    }
}
