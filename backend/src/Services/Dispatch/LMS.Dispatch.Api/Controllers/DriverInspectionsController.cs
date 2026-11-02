using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Inspections;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Inspections;
using LMS.Dispatch.Application.Inspections.Handlers;
using LMS.Dispatch.Domain.Inspections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机检查接口（Pre-Trip / Post-Trip）。司机标识只来自令牌；他人的记录与不存在不作区分，一律 404。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机检查")]
[Route("api/dispatch/driver/v1/me")]
public sealed class DriverInspectionsController(
    ISubmitInspectionHandler submitHandler,
    IDriverInspectionQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// D2-01 查询当前发布的检查模板。
    /// </summary>
    /// <remarks>
    /// 返回 PRE_TRIP / POST_TRIP 两组模板及版本。检查项以稳定代码标识，显示名称可以变更。模板是服务端发布的静态配置，响应可缓存。
    /// </remarks>
    /// <returns>当前模板列表。</returns>
    [HttpGet("inspection-templates", Name = "listInspectionTemplates")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<InspectionTemplateView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverInspectionsRead)]
    public IResult ListTemplates()
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "private, max-age=300";

        return ApiResults.Ok(InspectionTemplates.Current.Select(InspectionMapper.ToView).ToList());
    }

    /// <summary>
    /// D2-02 提交一次完整检查。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；同键同内容重放返回同一份记录（仍为 201）。必填项全部出现且无重复 / 未知项，否则 400。
    /// FAIL 也是合法结果，会如实保存，并不会自动限制接单或改变司机状态。提交后的记录不可修改。
    /// 停用 / 暂停的司机不能提交（422）。
    /// </remarks>
    /// <returns>新建的检查记录。</returns>
    [HttpPost("inspections", Name = "createDriverInspection")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(SubmitInspectionRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverInspectionView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverInspectionsWrite)]
    public async Task<IResult> SubmitAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<SubmitInspectionRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var driverId = HttpContext.GetPrincipal().EntityId;
        var command = InspectionMapper.ToSubmitCommand(driverId, body.Value, key.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await submitHandler.HandleAsync(command.Value, cancellationToken), InspectionMapper.ToView);
    }

    /// <summary>
    /// D2-03 分页查询本人检查记录。
    /// </summary>
    /// <remarks>
    /// 默认按提交时间倒序，稳定分页；可按阶段、是否存在不合格项、提交时间区间筛选。响应不缓存。
    /// </remarks>
    /// <returns>分页的检查记录；分页信息在 meta.pagination。</returns>
    [HttpGet("inspections", Name = "listDriverInspections")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverInspectionView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverInspectionsRead)]
    public async Task<IResult> ListAsync([FromQuery] DriverInspectionListRequest request, CancellationToken cancellationToken)
    {
        if (request.From is not null && request.To is not null && request.To <= request.From)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("to", "Must be later than from")]);
        }

        Response.Headers.CacheControl = "no-store";
        var query = InspectionMapper.ToListQuery(request, HttpContext.GetPrincipal().EntityId);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, InspectionMapper.ToView);
    }

    /// <summary>
    /// D2-04 查询本人检查详情。
    /// </summary>
    /// <param name="inspectionId">检查记录标识。</param>
    /// <returns>检查详情。</returns>
    [HttpGet("inspections/{inspectionId}", Name = "getDriverInspection")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverInspectionView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverInspectionsRead)]
    public async Task<IResult> GetAsync(string inspectionId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(inspectionId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, InspectionMapper.ToView);
    }
}
