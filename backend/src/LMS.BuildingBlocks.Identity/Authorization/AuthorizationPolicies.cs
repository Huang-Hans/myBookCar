namespace LMS.BuildingBlocks.Identity.Authorization;

// 业务入口使用的身份策略名。每个策略只接受一种认证方案，并检查该方案下的身份类型；
// 操作权限与本人/物业范围仍由 RequirePermission / RequirePrincipal 在其后累加检查。
public static class AuthorizationPolicies
{
    // 员工/客服：StaffBearer。
    public const string Staff = "Staff";

    // 司机本人：LmsAppBearer 且身份类型为 DRIVER。
    public const string Driver = "Driver";

    // 客户本人：LmsAppBearer 且身份类型为 CUSTOMER。
    public const string Customer = "Customer";

    // 服务间：LmsAppBearer 且为 Platform 签发的服务身份，仅用于未登录阶段的内部凭据核对。
    public const string PlatformService = "PlatformService";

    // 服务间受限身份：ServiceBearer 且为 Booking 签发、仅含通知收件人查询范围的服务令牌，只能调用对应的内部接口。
    public const string NotificationRecipientService = "NotificationRecipientService";

    // 服务间受限身份：ServiceBearer 且为 Booking 签发、仅含司机释放范围的服务令牌，只能调用 Dispatch 的司机释放接口。
    public const string DriverReleaseService = "DriverReleaseService";

    public const string DriverContactService = "DriverContactService";

    // 服務間受限身份：ServiceBearer 且為 Booking 簽發、僅含優惠券核銷範圍的服務令牌，只能調用 Dispatch 的優惠券核銷接口。
    public const string CouponRedemptionService = "CouponRedemptionService";

    // 服务间受限身份：ServiceBearer 且为 Reporting 签发、仅含报表来源读取范围的服务令牌，只能调用各服务的报表来源接口。
    public const string ReportingSourceService = "ReportingSourceService";
}
