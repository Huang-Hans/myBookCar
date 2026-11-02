using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Internal;
using LMS.Platform.Api.Contracts.Locations;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Configuration;
using LMS.Platform.Api.Mapping.Locations;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.CustomerBlacklists.Handlers;
using LMS.Platform.Application.Locations.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间读取接口：客户自助预订所需的地点、物业政策、取消原因、自助规则与客户限制。
/// </summary>
/// <remarks>
/// 员工目录接口要求员工权限，客户本人没有这些权限；此处只对已绑定的客户开放同源的只读数据，不返回任何员工管理字段，
/// 也不开放写入。调用方 Booking 转发客户的 Bearer 令牌；不经网关路由。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/catalog")]
[RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
public sealed class InternalCatalogController(
    ILocationQueryHandler locations,
    IConfigurationListHandler<ConfiguredProperty> properties,
    IConfigurationListHandler<CancellationReason> cancellationReasons,
    IConfigurationDocumentHandler<SelfBookingRules> bookingRules,
    IConfigurationDocumentHandler<EligibilityRules> eligibilityRules,
    ICheckCustomerRestrictionsHandler restrictionsHandler) : ControllerBase
{
    private const int MaxMemberIds = 50;

    /// <summary>
    /// 分页查询启用的地点（客户选择上下车地点）。
    /// </summary>
    /// <returns>分页的启用地点列表；分页信息在 meta.pagination。</returns>
    [HttpGet("locations")]
    public async Task<IResult> ListLocationsAsync([FromQuery] LocationListRequest request, CancellationToken cancellationToken)
    {
        var query = LocationRequestMapper.ToListQuery(request) with { IsActive = true };

        return PagedResponses.ToResult(await locations.ListAsync(query, cancellationToken), query.List, LocationViewMapper.ToView);
    }

    /// <summary>
    /// 查询地点详情。
    /// </summary>
    /// <param name="locationId">地点标识。</param>
    /// <returns>地点详情，包括是否启用。</returns>
    [HttpGet("locations/{locationId}")]
    public async Task<IResult> GetLocationAsync(string locationId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await locations.GetAsync(locationId, cancellationToken), LocationViewMapper.ToView);
    }

    /// <summary>
    /// 查询物业（取消政策引用）。
    /// </summary>
    /// <param name="propertyId">物业标识。</param>
    /// <returns>物业详情；每日限额来自 Booking，这里不提供。</returns>
    [HttpGet("properties/{propertyId}")]
    public Task<IResult> GetPropertyAsync(string propertyId, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(
            HttpContext, propertyId, properties, item => ConfigurationViewMapper.ToView(item, null),
            item => item.IsActive && !item.IsDeleted, cancellationToken);

    /// <summary>
    /// 查询取消原因详情。
    /// </summary>
    /// <param name="reasonCode">取消原因代码。</param>
    /// <returns>取消原因详情。</returns>
    [HttpGet("cancellation-reasons/{reasonCode}")]
    public Task<IResult> GetCancellationReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.FindAsync(
            HttpContext, reasonCode, cancellationReasons, ConfigurationViewMapper.ToView, null, cancellationToken);

    /// <summary>
    /// 查询自助预约规则。
    /// </summary>
    /// <returns>自助预约规则；未配置时为 503 / 50300，不返回演示数据。</returns>
    [HttpGet("self-booking-rules")]
    public Task<IResult> GetBookingRulesAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, bookingRules, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// 查询自助资格说明。
    /// </summary>
    /// <returns>自助资格说明；未配置时为 503 / 50300，不返回演示数据。</returns>
    [HttpGet("self-booking-eligibility-rules")]
    public Task<IResult> GetEligibilityRulesAsync(CancellationToken cancellationToken) =>
        ConfigurationControllerSupport.GetDocumentAsync(HttpContext, eligibilityRules, ConfigurationViewMapper.ToView, cancellationToken);

    /// <summary>
    /// 核对会员是否受客户限制（客户黑名单）。
    /// </summary>
    /// <returns>命中的客户限制列表；没有命中时为空数组。</returns>
    [HttpPost("customer-restriction-checks")]
    public async Task<IResult> CheckRestrictionsAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CustomerRestrictionCheckRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var memberIds = body.Value.MemberIds;
        if (memberIds is null || memberIds.Count > MaxMemberIds || memberIds.Any(string.IsNullOrWhiteSpace))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("memberIds", $"Must contain 0 to {MaxMemberIds} non-blank member ids")]);
        }

        var hits = await restrictionsHandler.HandleAsync(memberIds.Select(id => id!.Trim()).ToList(), cancellationToken);

        return ApiResults.Ok(hits.Select(hit => new CustomerRestrictionView(hit.MemberId, hit.Reason)).ToList());
    }
}
