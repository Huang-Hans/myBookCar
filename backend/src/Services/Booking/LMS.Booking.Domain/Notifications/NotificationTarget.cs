namespace LMS.Booking.Domain.Notifications;

// 通知点击后应跳转的业务对象。Type 为 TRIP（Id 是 tripId）或 QUALIFICATION（Id 是 qualificationId）；
// 不再让前端用 tripReference 搜索后猜测目标。
public sealed record NotificationTarget(string Type, string Id)
{
    public const string TripType = "TRIP";
    public const string QualificationType = "QUALIFICATION";

    public static NotificationTarget ForTrip(string tripId) => new(TripType, tripId);

    public static NotificationTarget ForQualification(string qualificationId) => new(QualificationType, qualificationId);
}
