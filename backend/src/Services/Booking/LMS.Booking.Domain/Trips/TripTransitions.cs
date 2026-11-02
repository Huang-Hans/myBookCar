namespace LMS.Booking.Domain.Trips;

// 行程单状态机的唯一权威：允许的转移、可取消与可修改的状态集合。
// 控制台只读状态；推进经 /internal 端点（一期运维手工，二期司机 App），取消与修改走控制台写接口。
public static class TripTransitions
{
    private static readonly IReadOnlyDictionary<TripStatus, TripStatus[]> Allowed =
        new Dictionary<TripStatus, TripStatus[]>
        {
            [TripStatus.Pending] = [TripStatus.Confirmed, TripStatus.DriverAssigned, TripStatus.Cancelled],
            [TripStatus.Confirmed] = [TripStatus.DriverAssigned, TripStatus.Cancelled],
            [TripStatus.DriverAssigned] = [TripStatus.DriverEnRoute, TripStatus.Cancelled],
            [TripStatus.DriverEnRoute] = [TripStatus.ArrivedAtPickup, TripStatus.Cancelled],
            [TripStatus.ArrivedAtPickup] = [TripStatus.InProgress, TripStatus.Cancelled],
            [TripStatus.InProgress] = [TripStatus.Completed],
            [TripStatus.Completed] = [],
            [TripStatus.Cancelled] = []
        };

    public static bool CanTransition(TripStatus from, TripStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    // 进行中与终态不可取消；其余状态可由控制台取消。
    public static bool CanCancel(TripStatus status) =>
        status is not (TripStatus.InProgress or TripStatus.Completed or TripStatus.Cancelled);

    // 仅前三个状态允许修改；越态修改按状态冲突处理。
    public static bool CanModify(TripStatus status) =>
        status is TripStatus.Pending or TripStatus.Confirmed or TripStatus.DriverAssigned;
}
