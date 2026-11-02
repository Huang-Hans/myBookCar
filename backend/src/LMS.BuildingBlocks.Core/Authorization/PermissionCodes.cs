namespace LMS.BuildingBlocks.Core.Authorization;

// Permission codes are fixed here; staff role grants come from the database with deployment configuration as fallback.
public static class PermissionCodes
{
    public const string ConfigurationRead = "configuration:read";
    public const string PropertiesWrite = "properties:write";
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string DriversRead = "drivers:read";
    public const string DriversWrite = "drivers:write";
    public const string LocationsRead = "locations:read";
    public const string LocationsWrite = "locations:write";
    public const string BlacklistsRead = "blacklists:read";
    public const string BlacklistsWrite = "blacklists:write";
    public const string AuditRead = "audit:read";
    public const string QuotasRead = "quotas:read";
    public const string QuotasWrite = "quotas:write";
    public const string OverridesRead = "overrides:read";
    public const string OverridesRequest = "overrides:request";
    public const string OverridesDecide = "overrides:decide";
    public const string QualificationsRead = "qualifications:read";
    public const string QualificationsEscalate = "qualifications:escalate";
    public const string QualificationsApprove = "qualifications:approve";
    public const string ApprovalsRead = "approvals:read";
    public const string ApprovalsMaterials = "approvals:materials";
    public const string ApprovalsDecide = "approvals:decide";
    public const string GuestsRead = "guests:read";
    public const string GuestsWrite = "guests:write";
    public const string BookingsRead = "bookings:read";
    public const string BookingsWrite = "bookings:write";
    public const string NotificationsRead = "notifications:read";
    public const string ReportsRead = "reports:read";
    public const string ReportsWrite = "reports:write";
    public const string DispatchRead = "dispatch:read";
    public const string DispatchWrite = "dispatch:write";

    public static IReadOnlyList<string> All { get; } =
    [
        ConfigurationRead, PropertiesWrite, UsersRead, UsersWrite, DriversRead, DriversWrite,
        LocationsRead, LocationsWrite, BlacklistsRead, BlacklistsWrite, AuditRead,
        QuotasRead, QuotasWrite, OverridesRead, OverridesRequest, OverridesDecide,
        QualificationsRead, QualificationsEscalate, QualificationsApprove,
        ApprovalsRead, ApprovalsMaterials, ApprovalsDecide,
        GuestsRead, GuestsWrite, BookingsRead, BookingsWrite, NotificationsRead, ReportsRead, ReportsWrite,
        DispatchRead, DispatchWrite
    ];
}
