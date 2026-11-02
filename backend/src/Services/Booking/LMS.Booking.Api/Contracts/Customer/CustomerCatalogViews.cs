namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 客户本人摘要与预订能力。
/// </summary>
/// <param name="GuestId">客户（宾客）标识，来自令牌绑定。</param>
/// <param name="Name">客户姓名。</param>
/// <param name="TierCode">会员等级代码。</param>
/// <param name="MemberId">会员号；没有时省略。</param>
/// <param name="PropertyId">所属物业；未分配时省略。</param>
/// <param name="PreferredLanguage">偏好语言；没有时省略。</param>
/// <param name="CanBook">当前是否可自助预订。</param>
/// <param name="BookingBlockReason">不可预订的原因：PROPERTY_UNASSIGNED / TIER_NOT_ELIGIBLE / RESTRICTED；可预订时省略。</param>
/// <param name="CurrentTrip">首页当前旅程：进行中优先，其次最近待出发；没有时省略。</param>
public sealed record CustomerSummaryView(
    string GuestId,
    string Name,
    string TierCode,
    string? MemberId,
    string? PropertyId,
    string? PreferredLanguage,
    bool CanBook,
    string? BookingBlockReason,
    CustomerTripSummaryView? CurrentTrip);

/// <summary>服务城市及其覆盖的地点区域。</summary>
public sealed record CityOfferingView(string CityCode, string Name, IReadOnlyList<string> Regions);

/// <summary>
/// 服务产品及对当前客户的可订状态。
/// </summary>
/// <param name="ProductCode">BUSINESS_SEDAN / LIMOUSINE / PARTY / CROSS_BORDER_TRANSFER。</param>
/// <param name="Name">产品名称。</param>
/// <param name="Description">产品说明。</param>
/// <param name="RouteScope">路线范围：LOCAL（起终点同城）/ CROSS_BORDER（分属澳门与香港）。</param>
/// <param name="IsBookable">当前是否可订。</param>
/// <param name="UnavailableReason">不可订的原因代码：产品未配置（例如 PARTY_CAPACITY_NOT_CONFIGURED）或客户级原因（PROPERTY_UNASSIGNED / TIER_NOT_ELIGIBLE / RESTRICTED）；可订时省略。</param>
public sealed record ProductOfferingView(
    string ProductCode,
    string Name,
    string Description,
    string RouteScope,
    bool IsBookable,
    string? UnavailableReason);

/// <summary>Platform 自助预约规则中影响选择时间的部分。</summary>
public sealed record BookingRulesView(int MinimumLeadTimeHours, int MaximumAdvanceDays, int MaximumActiveRequestsPerGuestPerDay);

/// <summary>服务目录：城市、产品、规则，以及是否允许立即出发。</summary>
public sealed record ServiceOfferingsView(
    IReadOnlyList<CityOfferingView> Cities,
    IReadOnlyList<ProductOfferingView> Products,
    BookingRulesView Rules,
    bool IsAsapAvailable);

/// <summary>客户可见的地点信息，不含联系人、电话与围栏等员工管理字段。</summary>
public sealed record CustomerLocationView(
    string LocationId,
    string Name,
    string Address,
    string CityCode,
    string Region,
    string CategoryCode,
    string OperatingHours);

/// <summary>校验通过的路线。</summary>
public sealed record RideRouteView(
    string PickupLocationId,
    string PickupName,
    string DropoffLocationId,
    string DropoffName,
    bool IsCrossBorder,
    string PickupCityCode,
    string DropoffCityCode);

/// <summary>车型产品及对当前客户与乘客数的可选结论；车型产品不等于实际派出的车牌。</summary>
public sealed record RideOptionView(
    string VehicleClass,
    int MaxPassengers,
    int MaxLuggage,
    long PointsRequired,
    bool IsEligible,
    string? IneligibleReason);

/// <summary>车型与路线查询结果；只读，不产生资格或审批记录。</summary>
public sealed record RideOptionsView(RideRouteView Route, IReadOnlyList<RideOptionView> Vehicles);
