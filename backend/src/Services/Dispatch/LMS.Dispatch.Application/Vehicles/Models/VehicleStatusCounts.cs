namespace LMS.Dispatch.Application.Vehicles.Models;

// 车辆管理顶部四张卡片。没有车辆的状态也返回 0。
public sealed record VehicleStatusCounts(long Available, long OnTrip, long Maintenance, long OutOfService);
