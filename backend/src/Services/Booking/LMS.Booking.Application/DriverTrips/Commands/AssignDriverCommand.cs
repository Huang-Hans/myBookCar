using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.DriverTrips.Commands;

// 来自 Dispatch 的条件指派请求。DriverId 须等于令牌绑定的司机；OfferId 是稳定的操作标识，重试同一邀请得到同一结果。
public sealed record AssignDriverCommand(
    string TripId,
    string OfferId,
    string DriverId,
    string DriverName,
    string VehicleLabel,
    string PropertyId,
    CommandContext Context);
