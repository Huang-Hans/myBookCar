namespace LMS.Booking.Domain.Trips;

// 一个车型的容量与积分价（来自 Console:VehicleClasses 配置），供规则校验使用。
public sealed record VehicleCapacity(VehicleClass VehicleClass, int MaxPassengers, int MaxLuggage, long PointsRequired);
