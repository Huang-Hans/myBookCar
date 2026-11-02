using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Commands;

// 服务间状态推进（一期运维手工 / 二期司机 App）：按状态机校验后原子推进。
public sealed record AdvanceTripStatusCommand(string TripId, TripStatus Target, CommandContext Context);
