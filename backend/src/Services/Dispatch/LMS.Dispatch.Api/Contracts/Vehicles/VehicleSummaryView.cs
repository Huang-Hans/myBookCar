namespace LMS.Dispatch.Api.Contracts.Vehicles;

/// <summary>
/// 车辆管理顶部四张卡片。数量按调用者物业范围统计，可选再按一个物业收窄。不受列表搜索、等级和状态筛选影响。
/// </summary>
/// <param name="Available">可用，AVAILABLE。</param>
/// <param name="OnTrip">行程中，ON_TRIP。</param>
/// <param name="Maintenance">维修中，MAINTENANCE。</param>
/// <param name="OutOfService">停用，OUT_OF_SERVICE。</param>
public sealed record VehicleSummaryView(long Available, long OnTrip, long Maintenance, long OutOfService);
