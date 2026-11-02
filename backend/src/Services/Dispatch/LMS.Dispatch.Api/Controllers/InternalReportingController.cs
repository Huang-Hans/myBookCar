using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.ReportingSources;
using LMS.Dispatch.Api.Mapping.ReportingSources;
using LMS.Dispatch.Application.ReportingSources.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 服务间接口：Reporting 定时重算司机利用率时读取的班次。
/// </summary>
/// <remarks>
/// 不经网关；只接受 Reporting 签发、仅含 reporting-source:read 范围的服务令牌（ReportingSourceService 策略）。只读，覆盖全部物业。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ReportingSourceService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/reporting")]
public sealed class InternalReportingController(IReportingShiftsQueryHandler handler) : ControllerBase
{
    private const int MaxDays = 62;

    /// <summary>
    /// 业务日期闭区间内的全部班次。
    /// </summary>
    [HttpGet("shifts")]
    public async Task<IResult> ListShiftsAsync([FromQuery] ReportingShiftsRequest request, CancellationToken cancellationToken)
    {
        var from = ReportingShiftMapper.ParseDate(request.From);
        var to = ReportingShiftMapper.ParseDate(request.To);
        if (Validate(from, to) is { } failure)
        {
            return failure;
        }

        var shifts = await handler.ListAsync(from!.Value, to!.Value, cancellationToken);

        return ApiResults.Ok(shifts.Select(ReportingShiftMapper.ToView).ToList());
    }

    private static IResult? Validate(DateOnly? from, DateOnly? to)
    {
        if (from is null || to is null)
        {
            return ApiResults.ValidationFailed([new ApiFieldError(from is null ? "from" : "to", "Must be a date in YYYY-MM-DD format")]);
        }

        return to < from || to.Value.DayNumber - from.Value.DayNumber >= MaxDays
            ? ApiResults.ValidationFailed([new ApiFieldError("to", $"Must not be earlier than from and span at most {MaxDays} days")])
            : null;
    }
}
