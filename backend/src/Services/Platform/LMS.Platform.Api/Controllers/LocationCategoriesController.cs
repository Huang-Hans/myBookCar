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
/// 地点分类接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("地点分类")]
[Route("api/platform/staff/v1/location-categories")]
public sealed class LocationCategoriesController(
    IConfigurationListHandler<LocationCategory> handler) : ControllerBase
{
    /// <summary>
    /// CF05 分页查询位置分类。
    /// </summary>
    /// <returns>分页的位置分类列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listLocationCategories")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<LocationCategoryView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] LocationCategoryListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAsync(handler, request.ToListQuery(), null, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF06 查询位置分类详情。
    /// </summary>
    /// <param name="categoryCode">位置分类代码。</param>
    /// <returns>位置分类详情。</returns>
    [HttpGet("{categoryCode}", Name = "getLocationCategory")]
    [ProducesResponseType(typeof(ApiEnvelope<LocationCategoryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string categoryCode, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, categoryCode, handler, ConfigurationViewMapper.ToView, null, cancellationToken);
}
