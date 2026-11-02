namespace LMS.BuildingBlocks.Core.Authorization;

// 服务令牌 scope 声明里的受限操作范围；服务身份只能调用对应的内部接口，不继承任何员工角色或权限。
public static class ServiceScopes
{
    public const string NotificationRecipientsQuery = "notification-recipients:query";

    // Booking 在行程完成 / 取消后通知 Dispatch 释放执行该行程的司机。
    public const string DriverAvailabilityRelease = "driver-availability:release";

    public const string DriverContactRead = "driver-contact:read";

    // Booking 在優惠券訂單建立時扣減已使用次數，取消時退還。
    public const string CouponRedemptionWrite = "coupon-redemption:write";

    // Reporting 定时重算报表时读取 Booking / Dispatch / Platform 的只读报表来源。
    public const string ReportingSourceRead = "reporting-source:read";
}
