using LMS.BuildingBlocks.Core.Authorization;
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
/// 路线缓冲（行程时间指数）接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("路线缓冲")]
[Route("api/platform/staff/v1/travel-time-indices")]
public sealed class TravelTimeIndicesController(
    IConfigurationListHandler<TravelBuffer> handler) : ControllerBase
{
    /// <summary>
    /// CF07 分页查询路线缓冲。
    /// </summary>
    /// <returns>分页的路线缓冲列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listTravelTimeIndices")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<TravelBufferView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] TravelBufferListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(handler, request.ToListQuery(), null, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF08 查询路线缓冲详情。
    /// </summary>
    /// <param name="routeCategoryCode">路线分类代码。</param>
    /// <returns>路线缓冲详情。</returns>
    [HttpGet("{routeCategoryCode}", Name = "getTravelTimeIndex")]
    [ProducesResponseType(typeof(ApiEnvelope<TravelBufferView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string routeCategoryCode, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, routeCategoryCode, handler, ConfigurationViewMapper.ToView, null, cancellationToken);
}
