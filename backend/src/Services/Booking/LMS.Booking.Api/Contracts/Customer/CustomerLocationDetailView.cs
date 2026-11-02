namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 客户可见的地点详情：C03 列表项的字段加地图坐标。不含联系人、电话与围栏等员工管理字段。
/// </summary>
/// <param name="LocationId">目录地点标识，与订单 route.pickupLocationId / dropoffLocationId 相同。</param>
/// <param name="Name">地点名称。</param>
/// <param name="Address">地址。</param>
/// <param name="CityCode">所属城市：MACAU / HONG_KONG。</param>
/// <param name="Region">区域：MACAU_PENINSULA / COTAI / HONG_KONG / AIRPORT。</param>
/// <param name="CategoryCode">分类代码。</param>
/// <param name="OperatingHours">营业时间文本。</param>
/// <param name="Coordinates">坐标；目录没有可用坐标时省略，不由地址推算。</param>
public sealed record CustomerLocationDetailView(
    string LocationId,
    string Name,
    string Address,
    string CityCode,
    string Region,
    string CategoryCode,
    string OperatingHours,
    CustomerCoordinatesView? Coordinates);
