namespace LMS.Booking.Domain.Trips;

// 司机履约的服务端规则（Q04 推荐映射）：六步进度对齐八态状态机而不改写它。
// 接受邀请 → DRIVER_ASSIGNED；出发 → DRIVER_EN_ROUTE；到达 → ARRIVED_AT_PICKUP；乘客上车 → IN_PROGRESS；
// 行驶中 / 到达目的地是 IN_PROGRESS 内的子阶段，只记录时间；完成 → COMPLETED。每一步只有唯一的下一动作。
public static class FulfillmentRules
{
    // 当前唯一允许的下一动作；没有指派、已终态或全部完成时为 null。
    public static DriverProgressAction? NextAction(Trip trip)
    {
        if (trip.Assignment is null)
        {
            return null;
        }

        return trip.Status switch
        {
            TripStatus.DriverAssigned => DriverProgressAction.StartEnRoute,
            TripStatus.DriverEnRoute => DriverProgressAction.ArriveAtPickup,
            TripStatus.ArrivedAtPickup => DriverProgressAction.PickUpPassenger,
            TripStatus.InProgress => NextWhileInProgress(trip.Fulfillment),
            _ => null
        };
    }

    public static IReadOnlyList<DriverProgressAction> AllowedActions(Trip trip) =>
        NextAction(trip) is { } action ? [action] : [];

    // 调用前须确认 action 等于 NextAction；这里只负责把动作翻译为状态、事件与时间。
    public static FulfillmentStep StepFor(DriverProgressAction action, TripFulfillment current, DateTimeOffset now) =>
        action switch
        {
            DriverProgressAction.StartEnRoute =>
                new(TripStatus.DriverEnRoute, TripEventType.StartedEnRoute, current with { EnRouteAt = now }),
            DriverProgressAction.ArriveAtPickup =>
                new(TripStatus.ArrivedAtPickup, TripEventType.ArrivedAtPickup, current with { ArrivedAtPickupAt = now }),
            DriverProgressAction.PickUpPassenger =>
                new(TripStatus.InProgress, TripEventType.PassengerBoarded, current with { PickedUpAt = now }),
            DriverProgressAction.StartTransit =>
                new(TripStatus.InProgress, TripEventType.InTransit, current with { InTransitAt = now }),
            DriverProgressAction.ArriveAtDestination =>
                new(TripStatus.InProgress, TripEventType.ArrivedAtDestination, current with { ArrivedAtDestinationAt = now }),
            _ => new(TripStatus.Completed, TripEventType.Completed, current with { CompletedAt = now })
        };

    // 动作对应的轨迹事件类型，用于幂等重放时核对同一幂等键提交的是否为同一动作。
    public static TripEventType EventTypeOf(DriverProgressAction action) =>
        StepFor(action, TripFulfillment.Empty, DateTimeOffset.MinValue).Type;

    private static DriverProgressAction NextWhileInProgress(TripFulfillment fulfillment)
    {
        if (fulfillment.InTransitAt is null)
        {
            return DriverProgressAction.StartTransit;
        }

        return fulfillment.ArrivedAtDestinationAt is null
            ? DriverProgressAction.ArriveAtDestination
            : DriverProgressAction.CompleteTrip;
    }
}
