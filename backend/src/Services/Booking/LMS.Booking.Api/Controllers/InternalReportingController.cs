using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.ReportingSources;
using LMS.Booking.Api.Mapping.ReportingSources;
using LMS.Booking.Application.ReportingSources.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 服务间接口：Reporting 定时重算运营报表时读取的行程事实。
/// </summary>
/// <remarks>
/// 不经网关。调用者是 Reporting 的受限服务身份（ServiceBearer，仅含 reporting-source:read 范围），
/// 不接受员工、司机或客户令牌。只读，覆盖全部物业。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ReportingSourceService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/reporting")]
public sealed class InternalReportingController(IReportingTripFactsQueryHandler handler) : ControllerBase
{
    /// <summary>
    /// 按计划接车时间区间分批读取行程事实，以 after 游标继续读取下一批。
    /// </summary>
    [HttpGet("trip-facts")]
    public async Task<IResult> ListTripFactsAsync([FromQuery] ReportingTripFactsRequest request, CancellationToken cancellationToken)
    {
        if (request.To <= request.From)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("to", "Must be later than from")]);
        }

        var page = await handler.ListAsync(ReportingTripFactMapper.ToQuery(request), cancellationToken);

        return ApiResults.Ok(ReportingTripFactMapper.ToView(page));
    }
}
