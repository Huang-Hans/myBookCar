namespace LMS.Booking.Api.Notifications;

// 行程位置的订阅组名。组名只由服务端在校验查看权限后拼接，客户端不能传入任意组名。
// 客户 Hub 与客服 Hub 各自维护自己的同名组，推送时两个 Hub 都发送。
internal static class TripLocationGroups
{
    public const string ClientMethod = "tripLocationUpdated";

    public static string For(string tripId) => $"trip-location:{tripId}";
}
