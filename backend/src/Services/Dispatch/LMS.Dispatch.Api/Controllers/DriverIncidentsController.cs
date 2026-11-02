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
/// 司机运营联系与事故报告接口（SOS / Incident）。司机标识只来自令牌；他人的记录与不存在不作区分，一律 404。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机事故报告")]
[Route("api/dispatch/driver/v1/me")]
public sealed class DriverIncidentsController(
    IOperationsContactsQueryHandler contactsHandler,
    ICreateIncidentHandler createHandler,
    IIncidentQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// D2-10 查询本人所属物业的运营联系资料。
    /// </summary>
    /// <remarks>
    /// 资料来自部署配置的可信渠道。没有配置时 isAvailable=false 且 contacts 为空，不会回退成司机个人紧急联系人。
    /// 查询资料不表示拨号已接通或运营已受理。
    /// </remarks>
    /// <returns>运营联系资料。</returns>
    [HttpGet("operations-contacts", Name = "getDriverOperationsContacts")]
    [ProducesResponseType(typeof(ApiEnvelope<OperationsContactsView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsRead)]
    public async Task<IResult> GetContactsAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await contactsHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, ReportMapper.ToView);
    }

    /// <summary>
    /// D2-11 登记事故 / 事件报告。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；同键同内容重放返回同一份记录（仍为 201）。发生时间由司机陈述（不得晚于当前时间），登记时间由服务端生成。
    /// 状态为 RECORDED，只表示本系统已保存；不推定已转交运营或已签收。可选 tripId 必须是本人的行程（含已完成 / 已取消）。
    /// </remarks>
    /// <returns>新建的事故报告。</returns>
    [HttpPost("incidents", Name = "createDriverIncident")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateIncidentRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverIncidentView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateIncidentRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ReportMapper.ToIncidentCommand(HttpContext.GetPrincipal().EntityId, body.Value, key.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), ReportMapper.ToView);
    }

    /// <summary>
    /// D2-12 分页查询本人事故报告。
    /// </summary>
    /// <returns>分页的事故报告；分页信息在 meta.pagination。</returns>
    [HttpGet("incidents", Name = "listDriverIncidents")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverIncidentView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsRead)]
    public async Task<IResult> ListAsync([FromQuery] DriverIncidentListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var query = ReportMapper.ToListQuery(request, HttpContext.GetPrincipal().EntityId);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, ReportMapper.ToView);
    }

    /// <summary>
    /// D2-13 查询本人事故报告详情。
    /// </summary>
    /// <param name="incidentId">报告标识。</param>
    /// <returns>报告详情与真实处理状态。</returns>
    [HttpGet("incidents/{incidentId}", Name = "getDriverIncident")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverIncidentView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverReportsRead)]
    public async Task<IResult> GetAsync(string incidentId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(incidentId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, ReportMapper.ToView);
    }
}
