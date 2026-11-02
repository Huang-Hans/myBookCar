using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Configuration;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间接口：Reporting 定时重算取消报表时读取当前生效的取消原因清单。
/// </summary>
/// <remarks>
/// 不经网关；只接受 Reporting 签发、仅含 reporting-source:read 范围的服务令牌（ReportingSourceService 策略）。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ReportingSourceService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/reporting")]
public sealed class InternalReportingController(IConfigurationListHandler<CancellationReason> reasons) : ControllerBase
{
    /// <summary>
    /// 全部取消原因，按来源顺序。
    /// </summary>
    [HttpGet("cancellation-reasons")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CancellationReasonView>>), StatusCodes.Status200OK)]
    public Task<IResult> ListCancellationReasonsAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAllAsync(reasons, null, ConfigurationViewMapper.ToView, cancellationToken);
}
