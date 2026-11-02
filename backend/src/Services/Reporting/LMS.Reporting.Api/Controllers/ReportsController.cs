using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Reporting.Api.Contracts.Reports;
using LMS.Reporting.Api.Mapping.Reports;
using LMS.Reporting.Application.Reports.Handlers;
using LMS.Reporting.Domain.Rows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Reporting.Api.Controllers;

/// <summary>
/// 运营报表只读查询接口。
/// </summary>
/// <remarks>
/// 读取报表读取模型，不创建报表运行历史。报表数据来源（Q04）未接入时返回 503 / 50300，不返回空列表冒充已生成。
/// 统计区间 from、to 必填，只返回统计窗口完全落在 [from, to) 内的行。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("运营报表")]
[Route("api/reporting/staff/v1/reports")]
public sealed class ReportsController(IReportQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// RP01 查询日报汇总。
    /// </summary>
    /// <returns>分页的日报汇总行；分页信息在 meta.pagination。</returns>
    [HttpGet("daily-job-summaries", Name = "listDailyJobSummaryReport")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DailyJobSummaryRowView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public Task<IResult> ListDailyJobSummariesAsync([FromQuery] DailyJobSummaryListRequest request, CancellationToken cancellationToken) =>
        ListAsync(ReportType.DailyJobSummary, request, ReportViewMapper.ToDailyJobSummary, cancellationToken);

    /// <summary>
    /// RP02 查询行程明细。
    /// </summary>
    /// <returns>分页的行程明细行；分页信息在 meta.pagination。</returns>
    [HttpGet("trip-details", Name = "listTripDetailReport")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TripDetailRowView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public Task<IResult> ListTripDetailsAsync([FromQuery] TripDetailListRequest request, CancellationToken cancellationToken) =>
        ListAsync(ReportType.TripDetail, request, ReportViewMapper.ToTripDetail, cancellationToken);

    /// <summary>
    /// RP03 查询司机利用率。
    /// </summary>
    /// <returns>分页的司机利用率行；分页信息在 meta.pagination。</returns>
    [HttpGet("driver-utilizations", Name = "listDriverUtilizationReport")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverUtilizationRowView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public Task<IResult> ListDriverUtilizationsAsync([FromQuery] DriverUtilizationListRequest request, CancellationToken cancellationToken) =>
        ListAsync(ReportType.DriverUtilization, request, ReportViewMapper.ToDriverUtilization, cancellationToken);

    /// <summary>
    /// RP04 查询车辆利用率。
    /// </summary>
    /// <returns>分页的车辆利用率行；分页信息在 meta.pagination。</returns>
    [HttpGet("vehicle-utilizations", Name = "listVehicleUtilizationReport")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<VehicleUtilizationRowView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public Task<IResult> ListVehicleUtilizationsAsync([FromQuery] VehicleUtilizationListRequest request, CancellationToken cancellationToken) =>
        ListAsync(ReportType.VehicleUtilization, request, ReportViewMapper.ToVehicleUtilization, cancellationToken);

    /// <summary>
    /// RP05 查询取消情况。
    /// </summary>
    /// <returns>分页的取消情况行；分页信息在 meta.pagination。</returns>
    [HttpGet("cancellations", Name = "listCancellationReport")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CancellationReportRowView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public Task<IResult> ListCancellationsAsync([FromQuery] CancellationReportListRequest request, CancellationToken cancellationToken) =>
        ListAsync(ReportType.Cancellation, request, ReportViewMapper.ToCancellation, cancellationToken);

    private async Task<IResult> ListAsync<TView>(
        ReportType type,
        ReportListRequest request,
        Func<ReportRow, TView> toView,
        CancellationToken cancellationToken)
    {
        var query = ReportRequestMapper.ToQuery(type, request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, toView);
    }
}
