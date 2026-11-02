using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Commands;

// 司机提交一个履约动作。DriverId 来自令牌；不接受期望版本。
public sealed record RecordTripProgressCommand(
    string TripId,
    string DriverId,
    DriverProgressAction Action,
    string IdempotencyKey,
    CommandContext Context);
