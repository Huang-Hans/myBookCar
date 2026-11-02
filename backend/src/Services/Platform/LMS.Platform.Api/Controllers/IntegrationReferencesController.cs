using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
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
/// 集成参考接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("集成参考")]
[Route("api/platform/staff/v1/integration-references")]
public sealed class IntegrationReferencesController(IConfigurationListHandler<IntegrationReference> handler) : ControllerBase
{
    /// <summary>
    /// CF20 查询集成参考。
    /// </summary>
    /// <remarks>
    /// 集成参考不分页。
    /// </remarks>
    /// <returns>集成参考列表；列表不分页，因此没有 meta.pagination。</returns>
    [HttpGet(Name = "listIntegrationReferences")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<IntegrationReferenceView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> ListAsync([FromQuery] IntegrationReferenceListRequest request, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.ListAllAsync(handler, item => item.Group == request.Group, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// CF21 查询集成参考详情。
    /// </summary>
    /// <param name="integrationCode">集成代码。</param>
    /// <returns>集成参考详情。</returns>
    [HttpGet("{integrationCode}", Name = "getIntegrationReference")]
    [ProducesResponseType(typeof(ApiEnvelope<IntegrationReferenceView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public Task<IResult> GetAsync(string integrationCode, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(HttpContext, integrationCode, handler, ConfigurationViewMapper.ToView, null, cancellationToken);}
