namespace LMS.BuildingBlocks.Core.Notifications;

// 实时通知的六类事件。对外字面值（WireEnum）为 BOOKING_CREATED 等；与旧站内通知分类（NotificationType）互不相通。
public enum RealtimeEventType
{
    BookingCreated,
    DriverOfferCreated,
    DriverOfferClosed,
    DriverOfferAccepted,
    DriverAssigned,
    TripStatusChanged
}
