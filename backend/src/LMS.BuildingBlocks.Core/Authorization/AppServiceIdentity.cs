namespace LMS.BuildingBlocks.Core.Authorization;

// 服务间调用使用的服务身份。服务身份不是司机、客户或员工，不能访问任何本人或员工端点。
// Platform 调用 Dispatch / Booking 内部凭据核对接口时，用本地签名密钥签发短效服务令牌（PlatformSubject，LmsAppBearer）。
// Booking 后台解析通知收件人时，用自己持有的独立密钥签发仅含 ServiceScopes.NotificationRecipientsQuery 的服务令牌（BookingSubject，ServiceBearer）；
// 行程完成 / 取消后通知 Dispatch 释放司机时，同样签发仅含 ServiceScopes.DriverAvailabilityRelease 的服务令牌。
// Reporting 定时重算报表时签发仅含 ServiceScopes.ReportingSourceRead 的服务令牌（ReportingSubject，ServiceBearer）。
public static class AppServiceIdentity
{
    public const string PrincipalType = "SERVICE";

    public const string PlatformSubject = "lms-platform";

    public const string BookingSubject = "lms-booking";

    public const string ReportingSubject = "lms-reporting";
}
