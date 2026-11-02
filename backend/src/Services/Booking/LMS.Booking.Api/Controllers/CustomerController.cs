using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Customer;
using LMS.Booking.Api.Mapping.Customer;
using LMS.Booking.Application.SelfService.Handlers;
using LMS.Booking.Application.SelfService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客户本人与预订目录接口（首页摘要、服务目录、地点、车型）。客户标识只来自令牌绑定，路径、查询串和正文都不接受宾客标识。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[Tags("客户预订目录")]
[Route("api/booking/customer/v1/me")]
public sealed class CustomerController(
    ICustomerProfileHandler profileHandler,
    ICustomerOfferingsHandler offeringsHandler,
    ICustomerLocationsHandler locationsHandler,
    ICustomerRideOptionsHandler rideOptionsHandler,
    SelfCancellationRules cancellationRules) : ControllerBase
{
    /// <summary>
    /// C01 查询客户本人摘要、预订能力与首页当前旅程。
    /// </summary>
    /// <remarks>
    /// currentTrip 按固定规则选取：进行中（前往 / 到达 / 行程中）优先，其次最近的待出发订单，范围限定为本人。响应不缓存。
    /// </remarks>
    /// <returns>客户摘要。</returns>
    [HttpGet(Name = "getCustomerSelf")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerSummaryView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await profileHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, summary => CustomerCatalogMapper.ToView(summary, cancellationRules));
    }

    /// <summary>
    /// C02 查询服务目录：城市、服务产品、可订状态与原因。
    /// </summary>
    /// <remarks>
    /// 产品不可订时给出明确原因（产品未配置，或 PROPERTY_UNASSIGNED / TIER_NOT_ELIGIBLE / RESTRICTED），而不是隐藏产品。
    /// 三类服务不是履约行程类型的一一对应值，产品到行程类型的映射在服务端配置。响应不缓存。
    /// </remarks>
    /// <returns>服务目录。</returns>
    [HttpGet("service-offerings", Name = "listCustomerServiceOfferings")]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceOfferingsView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetOfferingsAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await offeringsHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerCatalogMapper.ToView);
    }

    /// <summary>
    /// C03 搜索可选地点。
    /// </summary>
    /// <remarks>
    /// 只返回 Platform 目录中启用的地点，并且只含客户可见字段。自定义地址的受约束解析契约尚未提供：不接受任意文本冒充地点标识。
    /// </remarks>
    /// <returns>分页的地点列表；分页信息在 meta.pagination。</returns>
    [HttpGet("locations", Name = "listCustomerLocations")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerLocationView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> ListLocationsAsync([FromQuery] CustomerLocationListRequest request, CancellationToken cancellationToken)
    {
        var list = request.ToListQuery();
        var result = await locationsHandler.ListAsync(request.Region, list, cancellationToken);

        return PagedResponses.ToResult(result, list, CustomerCatalogMapper.ToView);
    }

    /// <summary>
    /// C03b 按地点标识查询地点详情（含地图坐标）。
    /// </summary>
    /// <remarks>
    /// 供订单详情地图使用：订单 route.pickupLocationId / dropoffLocationId 可直接作为 locationId。可见范围与 C03 一致，只有启用的目录地点；
    /// 不存在或已停用（含订单创建后被停用）的地点返回 404 / 40400，不返回坐标。坐标实时读取自 Platform 地点目录，不在订单中存放，不由地址推算；
    /// 目录没有可用坐标时省略 coordinates。
    /// </remarks>
    /// <param name="locationId">目录地点标识。</param>
    /// <returns>客户可见的地点详情。</returns>
    [HttpGet("locations/{locationId}", Name = "getCustomerLocation")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerLocationDetailView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetLocationAsync(string locationId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return OperationResultMapper.ToResult(await locationsHandler.GetAsync(locationId, cancellationToken), CustomerCatalogMapper.ToView);
    }

    /// <summary>
    /// C04 按服务、路线与乘客数查询可选车型。
    /// </summary>
    /// <remarks>
    /// 只读：不产生资格档案、审批或配额记录。路线与车型的检查和建单使用同一份逻辑；最终资格仍在建单时重新计算。
    /// 车型产品不等于实际派出的车牌。同起终点、不可服务路线与未启用地点由服务端判定。响应不缓存。
    /// </remarks>
    /// <returns>校验通过的路线与车型可选结论。</returns>
    [HttpGet("ride-options", Name = "listCustomerRideOptions")]
    [ProducesResponseType(typeof(ApiEnvelope<RideOptionsView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetRideOptionsAsync([FromQuery] RideOptionsRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await rideOptionsHandler.GetAsync(
            HttpContext.GetPrincipal().EntityId,
            request.ProductCode!,
            request.PickupLocationId!,
            request.DropoffLocationId!,
            request.Passengers,
            cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerCatalogMapper.ToView);
    }
}
