using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Reporting.Api.Contracts.Schedules;
using LMS.Reporting.Api.Controllers.Support;
using LMS.Reporting.Api.Mapping.Schedules;
using LMS.Reporting.Application.Schedules.Commands;
using LMS.Reporting.Application.Schedules.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Reporting.Api.Controllers;

/// <summary>
/// 邮件报告计划登记接口。
/// </summary>
/// <remarks>
/// 范围是登记与移除：登记成功只表示计划已登记，不代表执行时间已确定或邮件已发送；本服务没有发送、重发、暂停或任务历史入口。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("邮件报告计划")]
[Route("api/reporting/staff/v1/report-schedules")]
public sealed class ReportSchedulesController(
    IReportScheduleQueryHandler queryHandler,
    ICreateReportScheduleHandler createHandler,
    IRemoveReportScheduleHandler removeHandler) : ControllerBase
{
    /// <summary>
    /// SC01 分页查询邮件报告计划。
    /// </summary>
    /// <returns>分页的计划列表；只包含对调用者当前授权范围可见的计划；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listReportSchedules")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ReportScheduleView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public async Task<IResult> ListAsync([FromQuery] ReportScheduleListRequest request, CancellationToken cancellationToken)
    {
        var query = ReportScheduleMapper.ToListQuery(request, HttpContext.GetCaller().Scope);
        var result = await queryHandler.ListAsync(query, cancellationToken);

        return PagedResponses.ToResult(result, query.List, ReportScheduleMapper.ToView);
    }

    /// <summary>
    /// SC02 登记邮件报告计划。
    /// </summary>
    /// <remarks>
    /// 同一范围内邮箱（忽略大小写）、频率与报表类型相同的重复登记返回 409 / 40900；唯一性是否跨申请人范围待确认（Q08）。
    /// </remarks>
    /// <returns>登记的计划；相同幂等键且内容一致的重放同样返回 201，给出记录当前状态。</returns>
    [HttpPost(Name = "createReportSchedule")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateReportScheduleRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ReportScheduleView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.ReportsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateReportScheduleRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ReportScheduleMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), ReportScheduleMapper.ToView);
    }

    /// <summary>
    /// SC03 移除邮件报告计划。
    /// </summary>
    /// <remarks>
    /// 移除后保留审计，不留下停用记录。计划已不存在（或对调用者不可见）时返回相同的成功。
    /// </remarks>
    /// <param name="scheduleId">计划标识。</param>
    /// <returns>成功时不带数据。</returns>
    [HttpDelete("{scheduleId}", Name = "removeReportSchedule")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsWrite)]
    public async Task<IResult> RemoveAsync(string scheduleId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var command = new RemoveReportScheduleCommand(scheduleId, CallerContexts.From(HttpContext));
        var result = await removeHandler.HandleAsync(command, cancellationToken);

        return result.IsSuccess ? ApiResults.Ok<object?>(null) : OperationResultMapper.ToFailure(result);
    }
}
