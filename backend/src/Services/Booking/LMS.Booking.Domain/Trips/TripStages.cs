namespace LMS.Booking.Domain.Trips;

public static class TripStages
{
    // IN_PROGRESS 按已记录的履约时间区分子阶段；其余主状态一一对应。
    public static TripStage Of(Trip trip) => trip.Status switch
    {
        TripStatus.Pending => TripStage.Pending,
        TripStatus.Confirmed => TripStage.Confirmed,
        TripStatus.DriverAssigned => TripStage.DriverAssigned,
        TripStatus.DriverEnRoute => TripStage.DriverEnRoute,
        TripStatus.ArrivedAtPickup => TripStage.ArrivedAtPickup,
        TripStatus.InProgress => InProgressStage(trip.Fulfillment),
        TripStatus.Completed => TripStage.Completed,
        _ => TripStage.Cancelled
    };

    // 履约事件发生后行程所处的细分阶段：IN_PROGRESS 内的三个事件分别对应各自的子阶段，供时间线逐条显示。
    public static TripStage OfEvent(TripEventType type) => type switch
    {
        TripEventType.DriverAssigned => TripStage.DriverAssigned,
        TripEventType.StartedEnRoute => TripStage.DriverEnRoute,
        TripEventType.ArrivedAtPickup => TripStage.ArrivedAtPickup,
        TripEventType.PassengerBoarded => TripStage.PassengerOnBoard,
        TripEventType.InTransit => TripStage.InTransit,
        TripEventType.ArrivedAtDestination => TripStage.ArrivedAtDestination,
        TripEventType.Completed => TripStage.Completed,
        _ => TripStage.Cancelled
    };

    private static TripStage InProgressStage(TripFulfillment fulfillment)
    {
        if (fulfillment.ArrivedAtDestinationAt is not null)
        {
            return TripStage.ArrivedAtDestination;
        }

        return fulfillment.InTransitAt is not null ? TripStage.InTransit : TripStage.PassengerOnBoard;
    }
}
