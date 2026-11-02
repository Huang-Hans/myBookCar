namespace LMS.BuildingBlocks.Core.Notifications;

// 通知点击后定位的业务对象类型。TRIP 对应 tripId，DRIVER_OFFER 对应 offerId，QUALIFICATION 对应 qualificationId（本次事件不使用）。
public static class RealtimeTargetTypes
{
    public const string Trip = "TRIP";
    public const string DriverOffer = "DRIVER_OFFER";
    public const string Qualification = "QUALIFICATION";
}
