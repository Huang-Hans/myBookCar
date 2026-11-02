using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 一个车型产品及其对当前客户 / 乘客数的可选结论。车型产品不等于实际派出的车牌，实际车辆在接单后才确定。
// IneligibleReason 取 TIER_NOT_ENTITLED / PASSENGERS_EXCEED_CAPACITY；可选时为 null。
public sealed record RideOption(
    VehicleClass VehicleClass,
    int MaxPassengers,
    int MaxLuggage,
    long PointsRequired,
    bool IsEligible,
    string? IneligibleReason);
