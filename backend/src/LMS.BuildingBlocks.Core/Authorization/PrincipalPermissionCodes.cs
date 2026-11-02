namespace LMS.BuildingBlocks.Core.Authorization;

// 司机端与客户端的操作权限。与员工权限分开：不进入 PermissionCodes.All，因此不能通过角色权限配置授予员工，
// 也不会因员工权限放宽而被获得。权限随主体类型固定给出（见 AppPrincipalAuthorizationResolver）。
public static class PrincipalPermissionCodes
{
    public const string DriverSelfRead = "driver-self:read";
    public const string DriverSelfWrite = "driver-self:write";
    public const string DriverTripsRead = "driver-trips:read";
    public const string DriverTripsWrite = "driver-trips:write";
    public const string DriverPreferencesRead = "driver-preferences:read";
    public const string DriverPreferencesWrite = "driver-preferences:write";
    public const string DriverInspectionsRead = "driver-inspections:read";
    public const string DriverInspectionsWrite = "driver-inspections:write";
    public const string DriverReportsRead = "driver-reports:read";
    public const string DriverReportsWrite = "driver-reports:write";

    public const string CustomerBookingsRead = "customer-bookings:read";
    public const string CustomerBookingsWrite = "customer-bookings:write";
    public const string CustomerProfileRead = "customer-profile:read";
    public const string CustomerProfileWrite = "customer-profile:write";
    public const string CustomerFavoritesRead = "customer-favorites:read";
    public const string CustomerFavoritesWrite = "customer-favorites:write";
    public const string CustomerReviewsRead = "customer-reviews:read";
    public const string CustomerReviewsWrite = "customer-reviews:write";
    public const string CustomerPreferencesRead = "customer-preferences:read";
    public const string CustomerPreferencesWrite = "customer-preferences:write";

    public static IReadOnlyList<string> For(PrincipalType type) => type switch
    {
        PrincipalType.Driver =>
        [
            DriverSelfRead, DriverSelfWrite, DriverTripsRead, DriverTripsWrite,
            DriverPreferencesRead, DriverPreferencesWrite, DriverInspectionsRead, DriverInspectionsWrite,
            DriverReportsRead, DriverReportsWrite
        ],
        PrincipalType.Customer =>
        [
            CustomerBookingsRead, CustomerBookingsWrite, CustomerProfileRead, CustomerProfileWrite,
            CustomerFavoritesRead, CustomerFavoritesWrite, CustomerReviewsRead, CustomerReviewsWrite,
            CustomerPreferencesRead, CustomerPreferencesWrite
        ],
        _ => []
    };
}
